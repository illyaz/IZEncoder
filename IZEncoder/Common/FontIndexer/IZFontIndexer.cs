namespace IZEncoder.Common.FontIndexer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using Force.Crc32;

    internal class IZFontIndexer : IDisposable
    {
        private static readonly string _header = "IZFontIndexCollectionV112";
        private static readonly byte[] _headerBytes = Encoding.ASCII.GetBytes(_header);
        private string _path;
        private Stream _stream;

        private double oadate { get; set; } = -1;
        private int fontCount { get; set; } = -1;
        private double oadate2 { get; set; } = -1;
        private int fontCount2 { get; set; } = -1;

        private List<IZFontIndex> data { get; set; } = new List<IZFontIndex>();

        public void Dispose()
        {
            _stream?.Dispose();
        }
                
        public void Init(string path)
        {
            _path = path;
            _stream = File.Open(_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

            if (_stream.Length <= 0)
                return;

            try
            {
                using (var br = new BinaryReader(_stream, Encoding.UTF8, true))
                {
                    if (!br.ReadBytes(_headerBytes.Length).SequenceEqual(_headerBytes))
                        throw new Exception("Invalid header");

                    br.BaseStream.Position = 0;
                    using (var crc = new Crc32Algorithm())
                    {
                        var hash = crc.ComputeHash(br.ReadBytes((int)br.BaseStream.Length - 4));
                        if (!hash.SequenceEqual(br.ReadBytes(4)))
                            throw new Exception("Unexpected end of index data");
                    }

                    br.BaseStream.Position = _headerBytes.Length;

                    oadate = br.ReadDouble();
                    fontCount = br.ReadInt32();
                    oadate2 = br.ReadDouble();
                    fontCount2 = br.ReadInt32();

                    var dataCount = br.ReadInt32();
                    for (var i = 0; i < dataCount; i++)
                    {
                        var izfi = new IZFontIndex();
                        izfi.Path = br.ReadString();
                        izfi.FamilyName = br.ReadString();
                        if (string.IsNullOrEmpty(izfi.FamilyName))
                            izfi.FamilyName = null;

                        izfi.OADate = br.ReadDouble();

                        var faceCount = br.ReadInt32();
                        for (var j = 0; j < faceCount; j++)
                        {
                            var izff = new IZFontFacename();
                            izff.Name = br.ReadString();
                            izff.NameId = (NameId)br.ReadInt16();
                            izff.EncodingId = br.ReadInt16();
                            izff.LanguageId = br.ReadInt16();
                            izff.PlatformId = (PlatformId)br.ReadInt16();
                            izfi.Facenames.Add(izff);
                        }

                        data.Add(izfi);
                    }
                }
            }
            catch (Exception)
            {
                _stream?.Dispose();
                throw;
            }
        }

        public Task<bool> HasNewFont()
        {
            return Task.Factory.StartNew(() =>
            {
                var fontDirectoryInfo = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));
                var fontDirectoryInfo2 = new DirectoryInfo(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Windows\Fonts"));

                if (fontDirectoryInfo.GetFiles().Length != fontCount ||
                    fontDirectoryInfo2.GetFiles().Length != fontCount2)
                    return true;


                if (fontDirectoryInfo.LastWriteTimeUtc.ToOADate() != oadate ||
                    fontDirectoryInfo2.LastWriteTimeUtc.ToOADate() != oadate2)
                    return true;

                foreach (var izFontIndex in data)
                    try
                    {
                        var fi = new FileInfo(izFontIndex.Path);
                        if (izFontIndex.OADate != fi.LastWriteTimeUtc.ToOADate())
                            return true;
                    }
                    catch (Exception)
                    {
                        return true;
                    }

                return false;
            });
        }

        public Task Index(Func<double, string, bool> processChanged)
        {
            return Task.Factory.StartNew(() =>
            {
                FT_Library ftLibrary = IntPtr.Zero;
                var fontDirectoryInfo = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));
                var fontDirectoryInfo2 = new DirectoryInfo(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Windows\Fonts"));
                var fontFiles = fontDirectoryInfo.GetFiles().ToList();

                if (OSVersionHelper.WindowsVersionHelper.IsWindows10May2019OrGreater && fontDirectoryInfo2.Exists)
                    fontFiles.AddRange(fontDirectoryInfo2.GetFiles());

                try
                {
                    ThrowIfFailure(FT_Init_FreeType(out ftLibrary));
                    var c = 0;

                    data.Where(x => !fontFiles.Any(f => f.FullName.Equals(x.Path, StringComparison.OrdinalIgnoreCase)))
                        .ToList()
                        .ForEach(x => { data.Remove(x); });

                    data = data.GroupBy(t => t.Path).Select(g => g.First()).ToList();

                    foreach (var file in fontFiles)
                    {
                        if (!processChanged.Invoke(100 * (++c / (double)fontFiles.Count), file.FullName))
                            break; // Cancelled by user

                        var isnewfamily = true;
                        var findex = data.FirstOrDefault(x =>
                            x.Path.Equals(file.FullName, StringComparison.OrdinalIgnoreCase));
                        if (findex == null)
                            findex = new IZFontIndex();
                        else
                            isnewfamily = false;

                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (findex.OADate == file.LastWriteTimeUtc.ToOADate())
                            continue;

                        findex.Path = file.FullName;
                        findex.OADate = file.LastWriteTimeUtc.ToOADate();

                        using (var fs = File.OpenRead(file.FullName))
                        {
                            FT_Face face = IntPtr.Zero;
                            using (var ms = new MemoryStream())
                            {
                                fs.CopyTo(ms);
                                var bytes = ms.ToArray();

                                try
                                {
                                    var result = FT_New_Memory_Face(ftLibrary, bytes, (int)fs.Length, 0, out face);
                                    if (result == FT_Error.Ok)
                                    {
                                        //findex.FamilyName =
                                        //    Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(face, IntPtr.Size * 5));

                                        var sfntCount = FT_Get_Sfnt_Name_Count(face);
                                        for (var i = 0; i < sfntCount; i++)
                                        {
                                            var code = FT_Get_Sfnt_Name(face, i, out var sfnt);
                                            if (code != FT_Error.Ok ||
                                                !(sfnt.name_id == NameId.FontFamily
                                                  || sfnt.name_id == NameId.FullName
                                                  || sfnt.name_id == NameId.PSName))
                                                continue;

                                            var encoding = GetEncoding(sfnt.platform_id, sfnt.encoding_id)
                                                           ?? Encoding.BigEndianUnicode;
                                            var stringBytes = new byte[sfnt.string_len];
                                            Marshal.Copy(sfnt.string_ptr, stringBytes, 0, sfnt.string_len);

                                            findex.Facenames.Add(new IZFontFacename
                                            {
                                                NameId = sfnt.name_id,
                                                EncodingId = sfnt.encoding_id,
                                                LanguageId = sfnt.language_id,
                                                PlatformId = sfnt.platform_id,
                                                Name = encoding.GetString(stringBytes, 0, sfnt.string_len)
                                            });

                                            if (findex.FamilyName == null && sfnt.name_id == NameId.FontFamily)
                                                findex.FamilyName = encoding.GetString(stringBytes, 0, sfnt.string_len);
                                            findex.Facenames.Where(x => string.IsNullOrEmpty(x.Name)).ToList()
                                                .ForEach(x => findex.Facenames.Remove(x));
                                            stringBytes = null;
                                        }

                                        if (isnewfamily && findex.Facenames.Count > 0)
                                            data.Add(findex);
                                    }
                                    else
                                    {
                                        data.Remove(findex);
                                    }
                                }
                                finally
                                {
                                    if (face != IntPtr.Zero)
                                        FT_Done_Face(face);
                                    bytes = null;
                                }
                            }

                            face = IntPtr.Zero;
                        }
                    }
                }
                finally
                {
                    if (ftLibrary != IntPtr.Zero)
                        ThrowIfFailure(FT_Done_FreeType(ftLibrary));

                    oadate = fontDirectoryInfo.LastWriteTimeUtc.ToOADate();
                    fontCount = fontDirectoryInfo.Exists ? fontDirectoryInfo.GetFiles().Length : -1;
                    oadate2 = fontDirectoryInfo2.Exists ? fontDirectoryInfo2.LastWriteTimeUtc.ToOADate() : -1;
                    fontCount2 = fontDirectoryInfo2.Exists ? fontDirectoryInfo2.GetFiles().Length : -1;
                    data = data.OrderBy(x => x.FamilyName).ToList();
                }
            });
        }

        public Task Save()
        {
            return Task.Factory.StartNew(() =>
            {
                _stream.SetLength(0);

                using (var bw = new BinaryWriter(_stream, Encoding.UTF8, true))
                {
                    bw.Write(_headerBytes);
                    bw.Write(oadate);
                    bw.Write(fontCount);
                    bw.Write(oadate2);
                    bw.Write(fontCount2);
                    bw.Write(data.Count);

                    foreach (var izFontIndex in data)
                    {
                        bw.Write(izFontIndex.Path);
                        bw.Write(izFontIndex.FamilyName ?? "");
                        bw.Write(izFontIndex.OADate);
                        bw.Write(izFontIndex.Facenames.Count);
                        foreach (var izFontFacename in izFontIndex.Facenames)
                        {
                            bw.Write(izFontFacename.Name);
                            bw.Write((short)izFontFacename.NameId);
                            bw.Write(izFontFacename.EncodingId);
                            bw.Write(izFontFacename.LanguageId);
                            bw.Write((short)izFontFacename.PlatformId);
                        }
                    }
                }

                using (var crc = new Crc32Algorithm())
                {
                    _stream.Position = 0;
                    var hash = crc.ComputeHash(_stream);
                    _stream.Position = _stream.Length;
                    _stream.Write(hash, 0, hash.Length);
                }

                _stream.Flush();
            });
        }

        public IEnumerable<IZFontIndex> GetFonts()
            => data;

        public IEnumerable<IZFontIndex> Find(string name)
        {
            return data.Where(x =>
                (x.FamilyName ?? string.Empty).Equals(name, StringComparison.OrdinalIgnoreCase) ||
                x.Facenames.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }

        private static void ThrowIfFailure(FT_Error error)
        {
            if (error != FT_Error.Ok)
                throw new Exception(error.ToString());
        }

        public class IZFontIndex
        {
            public string Path { get; set; }
            public double OADate { get; set; }
            public string FamilyName { get; set; }
            public List<IZFontFacename> Facenames { get; set; } = new List<IZFontFacename>();
        }

        public class IZFontFacename
        {
            public string Name { get; set; }
            public NameId NameId { get; set; }
            public PlatformId PlatformId { get; set; }
            public short EncodingId { get; set; }
            public short LanguageId { get; set; }
        }


        #region NativeFreeType2

        [StructLayout(LayoutKind.Sequential)]
        private struct FT_Library
        {
            private IntPtr reference;

            public static implicit operator IntPtr(FT_Library a)
            {
                return a.reference;
            }

            public static implicit operator FT_Library(IntPtr a)
            {
                return new FT_Library { reference = a };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FT_Face
        {
            private IntPtr reference;

            public static implicit operator IntPtr(FT_Face a)
            {
                return a.reference;
            }

            public static implicit operator FT_Face(IntPtr a)
            {
                return new FT_Face { reference = a };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FT_SfntName
        {
            [MarshalAs(UnmanagedType.U2)] public readonly PlatformId platform_id;
            [MarshalAs(UnmanagedType.U2)] public readonly short encoding_id;
            [MarshalAs(UnmanagedType.U2)] public readonly short language_id;
            [MarshalAs(UnmanagedType.U2)] public readonly NameId name_id;
            public readonly IntPtr string_ptr;
            [MarshalAs(UnmanagedType.U4)] public readonly int string_len;
        }

        public enum NameId : ushort
        {
            FontFamily = 1,
            FullName = 4,
            PSName = 6
        }

        public enum PlatformId : ushort
        {
            AppleUnicode = 0,
            Macintosh = 1,
            Iso = 2,
            Microsoft = 3,
            Custom = 4,
            Adobe = 7
        }

        public enum MacEncodingId : ushort
        {
            Roman = 0,
            Japanese = 1,
            TraditionalChinese = 2,
            Korean = 3,
            Arabic = 4,
            Hebrew = 5,
            Greek = 6,
            Russian = 7,
            RSymbol = 8,
            Devanagari = 9,
            Gurmukhi = 10,          // 0x000A
            Gujarati = 11,          // 0x000B
            Oriya = 12,             // 0x000C
            Bengali = 13,           // 0x000D
            Tamil = 14,             // 0x000E
            Telugu = 15,            // 0x000F
            Kannada = 16,           // 0x0010
            Malayalam = 17,         // 0x0011
            Sinhalese = 18,         // 0x0012
            Burmese = 19,           // 0x0013
            Khmer = 20,             // 0x0014
            Thai = 21,              // 0x0015
            Laotian = 22,           // 0x0016
            Georgian = 23,          // 0x0017
            Armenian = 24,          // 0x0018
            Maldivian = 25,         // 0x0019
            SimplifiedChinese = 25, // 0x0019
            Tibetan = 26,           // 0x001A
            Mongolian = 27,         // 0x001B
            Geez = 28,              // 0x001C
            Slavic = 29,            // 0x001D
            Vietnamese = 30,        // 0x001E
            Sindhi = 31,            // 0x001F
            Uninterpreted = 32      // 0x0020
        }

        public enum MicrosoftEncodingId : ushort
        {
            Symbol = 0,
            Unicode = 1,
            Sjis = 2,
            GB2312 = 3,
            Big5 = 4,
            Wansung = 5,
            Johab = 6,
            Ucs4 = 10 // 0x000A
        }

        private enum FT_Error
        {
            Ok = 0,
            CannotOpenResource = 1,
            UnknownFileFormat = 2,
            InvalidFileFormat = 3,
            InvalidVersion = 4,
            LowerModuleVersion = 5,
            InvalidArgument = 6,
            UnimplementedFeature = 7,
            InvalidTable = 8,
            InvalidOffset = 9,
            ArrayTooLarge = 10,               // 0x0000000A
            InvalidGlyphIndex = 16,           // 0x00000010
            InvalidCharacterCode = 17,        // 0x00000011
            InvalidGlyphFormat = 18,          // 0x00000012
            CannotRenderGlyph = 19,           // 0x00000013
            InvalidOutline = 20,              // 0x00000014
            InvalidComposite = 21,            // 0x00000015
            TooManyHints = 22,                // 0x00000016
            InvalidPixelSize = 23,            // 0x00000017
            InvalidHandle = 32,               // 0x00000020
            InvalidLibraryHandle = 33,        // 0x00000021
            InvalidDriverHandle = 34,         // 0x00000022
            InvalidFaceHandle = 35,           // 0x00000023
            InvalidSizeHandle = 36,           // 0x00000024
            InvalidSlotHandle = 37,           // 0x00000025
            InvalidCharMapHandle = 38,        // 0x00000026
            InvalidCacheHandle = 39,          // 0x00000027
            InvalidStreamHandle = 40,         // 0x00000028
            TooManyDrivers = 48,              // 0x00000030
            TooManyExtensions = 49,           // 0x00000031
            OutOfMemory = 64,                 // 0x00000040
            UnlistedObject = 65,              // 0x00000041
            CannotOpenStream = 81,            // 0x00000051
            InvalidStreamSeek = 82,           // 0x00000052
            InvalidStreamSkip = 83,           // 0x00000053
            InvalidStreamRead = 84,           // 0x00000054
            InvalidStreamOperation = 85,      // 0x00000055
            InvalidFrameOperation = 86,       // 0x00000056
            NestedFrameAccess = 87,           // 0x00000057
            InvalidFrameRead = 88,            // 0x00000058
            RasterUninitialized = 96,         // 0x00000060
            RasterCorrupted = 97,             // 0x00000061
            RasterOverflow = 98,              // 0x00000062
            RasterNegativeHeight = 99,        // 0x00000063
            TooManyCaches = 112,              // 0x00000070
            InvalidOpCode = 128,              // 0x00000080
            TooFewArguments = 129,            // 0x00000081
            StackOverflow = 130,              // 0x00000082
            CodeOverflow = 131,               // 0x00000083
            BadArgument = 132,                // 0x00000084
            DivideByZero = 133,               // 0x00000085
            InvalidReference = 134,           // 0x00000086
            DebugOpCode = 135,                // 0x00000087
            EndfInExecStream = 136,           // 0x00000088
            NestedDefs = 137,                 // 0x00000089
            InvalidCodeRange = 138,           // 0x0000008A
            ExecutionTooLong = 139,           // 0x0000008B
            TooManyFunctionDefs = 140,        // 0x0000008C
            TooManyInstructionDefs = 141,     // 0x0000008D
            TableMissing = 142,               // 0x0000008E
            HorizHeaderMissing = 143,         // 0x0000008F
            LocationsMissing = 144,           // 0x00000090
            NameTableMissing = 145,           // 0x00000091
            CMapTableMissing = 146,           // 0x00000092
            HmtxTableMissing = 147,           // 0x00000093
            PostTableMissing = 148,           // 0x00000094
            InvalidHorizMetrics = 149,        // 0x00000095
            InvalidCharMapFormat = 150,       // 0x00000096
            InvalidPPem = 151,                // 0x00000097
            InvalidVertMetrics = 152,         // 0x00000098
            CouldNotFindContext = 153,        // 0x00000099
            InvalidPostTableFormat = 154,     // 0x0000009A
            InvalidPostTable = 155,           // 0x0000009B
            SyntaxError = 160,                // 0x000000A0
            StackUnderflow = 161,             // 0x000000A1
            Ignore = 162,                     // 0x000000A2
            NoUnicodeGlyphName = 163,         // 0x000000A3
            MissingStartfontField = 176,      // 0x000000B0
            MissingFontField = 177,           // 0x000000B1
            MissingSizeField = 178,           // 0x000000B2
            MissingFontboudingboxField = 179, // 0x000000B3
            MissingCharsField = 180,          // 0x000000B4
            MissingStartcharField = 181,      // 0x000000B5
            MissingEncodingField = 182,       // 0x000000B6
            MissingBbxField = 183,            // 0x000000B7
            BbxTooBig = 184,                  // 0x000000B8
            CorruptedFontHeader = 185,        // 0x000000B9
            CorruptedFontGlyphs = 186         // 0x000000BA
        }

        [DllImport("freetype", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_Error FT_Init_FreeType(out FT_Library library);

        [DllImport("freetype", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_Error FT_Done_FreeType(FT_Library library);

        [DllImport("freetype", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_Error FT_New_Face(FT_Library library, string filepathname, int face_index,
            out FT_Face aface);

        [DllImport("freetype", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_Error FT_New_Memory_Face(FT_Library library, byte[] file_base, int file_size,
            int face_index, out FT_Face aface);

        [DllImport("freetype", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_Error FT_Done_Face(FT_Face aface);

        [DllImport("freetype", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT_Error FT_Get_Sfnt_Name(FT_Face library, int idx, out FT_SfntName aname);

        [DllImport("freetype", CallingConvention = CallingConvention.Cdecl)]
        private static extern int FT_Get_Sfnt_Name_Count(FT_Face face);

        private static Encoding GetEncoding(PlatformId pid, short eid)
        {
            switch (pid)
            {
                case PlatformId.Adobe:
                case PlatformId.AppleUnicode:
                case PlatformId.Iso:
                    break;
                case PlatformId.Macintosh:
                    switch ((MacEncodingId)eid)
                    {
                        case MacEncodingId.Japanese:
                            return Encoding.GetEncoding(10001);
                        case MacEncodingId.TraditionalChinese:
                            return Encoding.GetEncoding(10002);
                        case MacEncodingId.Korean:
                            return Encoding.GetEncoding(10003);
                        case MacEncodingId.Arabic:
                            return Encoding.GetEncoding(10004);
                        case MacEncodingId.Hebrew:
                            return Encoding.GetEncoding(10005);
                        case MacEncodingId.Greek:
                            return Encoding.GetEncoding(10006);
                        case MacEncodingId.SimplifiedChinese:
                            return Encoding.GetEncoding(10008);
                        case MacEncodingId.Roman:
                            return Encoding.GetEncoding(10010);
                        case MacEncodingId.Thai:
                            return Encoding.GetEncoding(10021);
                    }

                    break;
                case PlatformId.Microsoft:
                    switch ((MicrosoftEncodingId)eid)
                    {
                        case MicrosoftEncodingId.Symbol:
                            break;
                        case MicrosoftEncodingId.Unicode:
                            return Encoding.BigEndianUnicode;
                        case MicrosoftEncodingId.Sjis:
                            return Encoding.GetEncoding(932);
                        case MicrosoftEncodingId.GB2312:
                            return Encoding.GetEncoding(936);
                        case MicrosoftEncodingId.Big5:
                            return Encoding.GetEncoding(950);
                        case MicrosoftEncodingId.Wansung:
                            return Encoding.GetEncoding(20949);
                        case MicrosoftEncodingId.Johab:
                            return Encoding.GetEncoding(1361);
                        case MicrosoftEncodingId.Ucs4:
                            return Encoding.BigEndianUnicode;
                    }

                    break;
            }

            return null;
        }

        #endregion
    }
}