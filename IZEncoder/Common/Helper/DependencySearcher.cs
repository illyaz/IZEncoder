namespace IZEncoder.Common.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal static class DependencySearcher
    {
        public static string TrySearch(string name, IEnumerable<string> paths)
        {
            return paths.Select(x => Path.GetFullPath(Path.Combine(x.Trim().Trim('\\'), name.Trim().Trim('\\'))))
                .FirstOrDefault(File.Exists);
        }

        public static string Search(string name, IEnumerable<string> paths)
        {
            var pathArray = paths as List<string> ?? paths.ToList();
            pathArray.Insert(0, Environment.CurrentDirectory);
            pathArray = pathArray.Distinct().ToList();
            return TrySearch(name, pathArray) ??
                   throw new FileNotFoundException(
                       $"Could not find '{name}' with paths: \r\n{string.Join("\r\n", pathArray.Select(x => x.TrimEnd('\\', '/')))}",
                       name);
        }
    }
}