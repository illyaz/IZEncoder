namespace IZEncoder.Common.FFMSIndexer
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    public class FFMSIndexer : IDisposable
    {
        public delegate void TDoIndexAsyncCallback(long Current, long Total, ref bool cancel);

        public delegate int TIndexCallback(long Current, long Total, IntPtr ICPrivate);

        private readonly string _file;
        private readonly IntPtr _filePtr;
        private readonly FFMS_Indexer _indexer;


        private FFMS_ErrorInfo _errorInfo;
        private FFMS_Index _index;

        private TIndexCallback _progressCallback; // Hold object in .net, prevent gc collect

        public FFMSIndexer(string file)
        {
            if (LoadLibrary("ffms2.dll") == IntPtr.Zero)
                throw new FFMSIndexerException($"Unable to load FFMS2 Library: {GetLastError()}") {ErrorType = FFMSErrors.UNSUPPORTED};

            _file = file;
            _filePtr = NativeUtf8FromString(_file);
            FFMS_Init(0, 0);
            _errorInfo.BufferSize = 1024;
            _errorInfo.Buffer = Marshal.AllocHGlobal(_errorInfo.BufferSize);

            _indexer = FFMS_CreateIndexer(_filePtr, ref _errorInfo);
            EnsureSuccess();

            FFMS_TrackTypeIndexSettings(_indexer, FFMSTrackType.AUDIO, 1, 0);
            for (var i = 0; i < sizeof(long) * 8; i++)
                if (((-1 >> i) & 1) != 0)
                    FFMS_TrackIndexSettings(_indexer, i, 1, 0);
        }

        public void Dispose()
        {
            if (_index != IntPtr.Zero)
            {
                FFMS_DestroyIndex(_index);
            }
            else
            {
                if (_indexer != IntPtr.Zero
                    && _errorInfo.ErrorType == FFMSErrors.SUCCESS)
                    FFMS_CancelIndexing(_indexer);
            }

            if (_errorInfo.Buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(_errorInfo.Buffer);

            if (_filePtr != IntPtr.Zero)
                Marshal.FreeHGlobal(_filePtr);

            _progressCallback = null;
        }

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern void
            FFMS_Init(int unk1, int unk2); /* Pass 0 to both arguments, kept to partially preserve abi */

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern FFMS_Indexer FFMS_CreateIndexer(IntPtr SourceFile, ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern void
            FFMS_TrackTypeIndexSettings(FFMS_Indexer Indexer, FFMSTrackType TrackType, int Index,
                int unk1); /* Pass 0 to last argument, kapt to preserve abi. Introduced in FFMS_VERSION ((2 << 24) | (21 << 16) | (0 << 8) | 0) */

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern void
            FFMS_TrackIndexSettings(FFMS_Indexer Indexer, int Track, int Index,
                int unk1); /* Pass 0 to last argument, kapt to preserve abi. Introduced in FFMS_VERSION ((2 << 24) | (21 << 16) | (0 << 8) | 0) */

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern FFMS_Index FFMS_DoIndexing2(FFMS_Indexer Indexer, FFMSIndexErrorHandling ErrorHandling,
            ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern void FFMS_DestroyIndex(FFMS_Index Index);

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern void FFMS_CancelIndexing(FFMS_Indexer Indexer);

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern int FFMS_WriteIndex(IntPtr IndexFile, FFMS_Index Index, ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern FFMS_Index FFMS_ReadIndex(IntPtr IndexFile, ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern int FFMS_IndexBelongsToFile(FFMS_Index Index, IntPtr SourceFile,
            ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        private static extern void
            FFMS_SetProgressCallback(FFMS_Indexer Indexer, TIndexCallback IC,
                IntPtr ICPrivate); /* Introduced in FFMS_VERSION ((2 << 24) | (21 << 16) | (0 << 8) | 0) */

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        public void SetProgressCallback(TIndexCallback callback)
        {
            _progressCallback = callback;
            FFMS_SetProgressCallback(_indexer, _progressCallback, IntPtr.Zero);
        }

        public void DoIndex()
        {
            _index = FFMS_DoIndexing2(_indexer, FFMSIndexErrorHandling.ABORT, ref _errorInfo);
            EnsureSuccess();
        }

        public Task DoIndexAsync(TDoIndexAsyncCallback callback)
        {
            return Task.Factory.StartNew(() =>
            {
                var cancel = false;
                SetProgressCallback((c, t, p) =>
                {
                    callback(c, t, ref cancel);
                    return cancel ? 1 : 0;
                });

                DoIndex();
            });
        }

        public void WriteIndex(string path)
        {
            var pathPtr = NativeUtf8FromString(path);
            try
            {
                FFMS_WriteIndex(pathPtr, _index, ref _errorInfo);
                EnsureSuccess();
            }
            finally
            {
                Marshal.FreeHGlobal(pathPtr);
            }
        }

        public Task WriteIndexAsync(string path)
        {
            return Task.Factory.StartNew(() => WriteIndex(path));
        }

        public bool IndexBelongsToFile(string indexPath)
        {
            FFMS_Index index = IntPtr.Zero;
            try
            {
                var indexPathPtr = NativeUtf8FromString(indexPath);
                try
                {
                    index = FFMS_ReadIndex(indexPathPtr, ref _errorInfo);
                    if (_errorInfo.ErrorType == FFMSErrors.PARSER &&
                        (_errorInfo.SubType == FFMSErrors.NO_FILE || _errorInfo.SubType == FFMSErrors.FILE_READ))
                        return false;

                    EnsureSuccess();
                }
                finally
                {
                    Marshal.FreeHGlobal(indexPathPtr);
                }

                FFMS_IndexBelongsToFile(index, _filePtr, ref _errorInfo);
                if (_errorInfo.ErrorType == FFMSErrors.INDEX && _errorInfo.SubType == FFMSErrors.FILE_MISMATCH)
                {
                    return false;
                }
                else if (_errorInfo.ErrorType != FFMSErrors.SUCCESS)
                {
                    EnsureSuccess();
                    return false;
                }
                else
                {
                    return true;
                }
            }
            finally
            {
                if (index != IntPtr.Zero)
                    FFMS_DestroyIndex(index);
            }
        }

        public Task<bool> IndexBelongsToFileAsync(string indexPath)
        {
            return Task.Factory.StartNew(() => IndexBelongsToFile(indexPath));
        }
        private static IntPtr NativeUtf8FromString(string managedString)
        {
            var len = Encoding.UTF8.GetByteCount(managedString);
            var buffer = new byte[len + 1];
            Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);
            var nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
            return nativeUtf8;
        }

        private static string StringFromNativeUtf8(IntPtr nativeUtf8)
        {
            var len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0) ++len;
            var buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        private void EnsureSuccess()
        {
            if (_errorInfo.ErrorType != FFMSErrors.SUCCESS)
                throw new FFMSIndexerException(StringFromNativeUtf8(_errorInfo.Buffer))
                {
                    ErrorType = _errorInfo.ErrorType,
                    SubType = _errorInfo.SubType
                };
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct FFMS_Indexer
        {
            private IntPtr p;

            public static implicit operator IntPtr(FFMS_Indexer a)
            {
                return a.p;
            }

            public static implicit operator FFMS_Indexer(IntPtr a)
            {
                return new FFMS_Indexer {p = a};
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FFMS_Index
        {
            private IntPtr p;

            public static implicit operator IntPtr(FFMS_Index a)
            {
                return a.p;
            }

            public static implicit operator FFMS_Index(IntPtr a)
            {
                return new FFMS_Index {p = a};
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FFMS_ErrorInfo
        {
            public readonly FFMSErrors ErrorType;
            public readonly FFMSErrors SubType;
            public int BufferSize;
            public IntPtr Buffer;
        }
    }
}