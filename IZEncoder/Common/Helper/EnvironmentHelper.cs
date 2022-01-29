namespace IZEncoder.Common.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal static class EnvironmentHelper
    {
        private static HashSet<char> _invalidCharacters = new HashSet<char>(Path.GetInvalidPathChars());

        public static void AddPath(string path)
        {
            var paths = GetPaths();

            if (!paths.Contains(path))
                paths.Add(path);

            Environment.SetEnvironmentVariable("PATH", string.Join(";", paths));
        }

        public static void AddPaths(IEnumerable<string> paths)
        {
            AddPaths(paths.ToArray());
        }

        public static void AddPaths(params string[] _paths)
        {
            var paths = GetPaths();

            foreach (var path in _paths)
                if (!paths.Contains(path))
                    paths.Add(path);

            Environment.SetEnvironmentVariable("PATH", string.Join(";", paths));
        }

        public static Dictionary<string, string> GetCommandLineArgs()
        {
            var args = new Dictionary<string, string>();
            foreach (var arg in Environment.GetCommandLineArgs())
                if (arg.StartsWith("--"))
                {
                    var v = arg.Substring(2).Split(new[] { '=' }, 2);
                    switch (v.Length) {
                        case 1:
                            args.Add(v[0], null);
                            break;
                        case 2:
                            args.Add(v[0], v[1]);
                            break;
                    }
                }

            return args;
        }

        public static List<string> GetPaths()
        {
            return Environment.GetEnvironmentVariable("PATH")?.Split(';').Where(IsPathValid).ToList();
        }

        public static bool IsPathValid(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && !filePath.Any(pc => _invalidCharacters.Contains(pc));
        }
    }
}