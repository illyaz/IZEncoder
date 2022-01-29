namespace IZEncoder.Common.Helper
{
    using System.IO;

    public static class PathHelper
    {
        public static string EnsureDirectoryExists(this string file)
        {
            if (!Directory.Exists(file))
                Directory.CreateDirectory(file);

            return file;
        }
    }
}