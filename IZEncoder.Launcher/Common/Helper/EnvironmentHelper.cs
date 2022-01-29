namespace IZEncoder.Launcher.Common.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class EnvironmentHelper
    {
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
            var currentArg = (string) null;
            foreach (var arg in Environment.GetCommandLineArgs())
                if (arg.StartsWith("--"))
                {
                    currentArg = arg.Substring(2);
                    if (arg.Contains(currentArg))
                        args[currentArg] = null;
                    else
                        args.Add(currentArg, null);
                }
                else if (currentArg != null)
                {
                    args[currentArg] = arg;
                    currentArg = null;
                }

            return args;
        }

        public static List<string> GetPaths()
        {
            return Environment.GetEnvironmentVariable("PATH")?.Split(';').ToList();
        }
    }
}