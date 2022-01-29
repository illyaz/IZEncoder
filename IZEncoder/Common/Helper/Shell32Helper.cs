namespace IZEncoder.Common.Helper
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    public static class Shell32Helper
    {
        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl,
            [In] [MarshalAs(UnmanagedType.LPArray)]
            IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name,
            IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);

        public static void OpenFolderAndSelectItem(string folderPath, string file)
        {
            SHParseDisplayName(folderPath, IntPtr.Zero, out var nativeFolder, 0, out _);

            if (nativeFolder == IntPtr.Zero) return;

            SHParseDisplayName(Path.Combine(folderPath, file), IntPtr.Zero, out var nativeFile, 0, out _);

            IntPtr[] fileArray;
            fileArray = nativeFile == IntPtr.Zero ? new IntPtr[0] : new[] {nativeFile};

            SHOpenFolderAndSelectItems(nativeFolder, (uint) fileArray.Length, fileArray, 0);

            Marshal.FreeCoTaskMem(nativeFolder);
            if (nativeFile != IntPtr.Zero) Marshal.FreeCoTaskMem(nativeFile);
        }

        public static void OpenFolder(string folderPath)
        {
            SHParseDisplayName(folderPath, IntPtr.Zero, out var nativeFolder, 0, out _);

            if (nativeFolder == IntPtr.Zero)
                return;

            SHOpenFolderAndSelectItems(nativeFolder, 0, null, 0);
            Marshal.FreeCoTaskMem(nativeFolder);
        }
    }
}