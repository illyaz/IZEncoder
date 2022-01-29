namespace IZEncoder.Server.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using Newtonsoft.Json;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var parsedArgs = EnvironmentHelper.GetCommandLineArgs();

            if (parsedArgs.ContainsKey("makedist"))
            {
                if (parsedArgs.ContainsKey("baseDir") && parsedArgs.ContainsKey("outDir"))
                    BuildUpdateFile(parsedArgs["makedist"], parsedArgs["baseDir"], parsedArgs["outDir"], parsedArgs.ContainsKey("zip") ? parsedArgs["zip"] : null);
                else
                    Console.WriteLine($"Usage: --makedist {{distListFile}} {{baseDir}} {{outDir}}");
            }
            
        }

        private static void BuildUpdateFile(string distListFile, string baseDir, string outDir, string zip)
        {
            distListFile = Path.GetFullPath(distListFile);
            baseDir = Path.GetFullPath(baseDir);
            outDir = Path.GetFullPath(outDir);

            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            //var distList = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(distListFile));
            var distInfos = JsonConvert.DeserializeObject<Dictionary<string, DistInfo>>(File.ReadAllText(distListFile));

            Console.WriteLine("Creating dist files ...");
            foreach (var kvp in distInfos)
            {
                var s = kvp.Key;
                var dist = kvp.Value;
                var fi = new FileInfo(Path.Combine(baseDir, s));

                if (!File.Exists(fi.FullName))
                {
                    Console.WriteLine($"                                 -> [NOT FOUND] {s}");
                    continue;
                }

                dist.Hash = CreateMD5Hash(fi.FullName);
                dist.Size = fi.Length;
                dist.Time = fi.LastWriteTimeUtc;

                var outFile = new FileInfo(Path.Combine(outDir, dist.Hash));
                var outFileMode = "REUSE";
                if (!outFile.Exists)
                {
                    outFileMode = "CREATE";
                    using (var gzf = outFile.Create())
                    {
                        using (var gz = new GZipStream(gzf,
                            CompressionMode.Compress, true))
                        {
                            using (var infile = fi.OpenRead())
                                infile.CopyTo(gz);
                        }

                        dist.CompressSize = gzf.Length;
                    }
                }
                else
                    dist.CompressSize = outFile.Length;
                
                Console.WriteLine($"{distInfos[s].Hash} -> [{outFileMode}] {s}");
            }

            Console.WriteLine("Removing unused dist files ...");
            foreach (var file in Directory.GetFiles(outDir))
            {
                var name = Path.GetFileName(file);
                if (name.EndsWith(".json"))
                    continue;

                if (distInfos.All(x => x.Value.Hash != name))
                {
                    Console.WriteLine($"{name} Deleted.");
                    File.Delete(file);
                }
            }

            Console.WriteLine("Writing info.json");
            File.WriteAllText(Path.Combine(outDir, "info.json"),
                JsonConvert.SerializeObject(
                    distInfos.Where(x => x.Key != "IZEncoder.Launcher.exe").ToDictionary(x => x.Key, x => x.Value),
                    Formatting.Indented));

            Console.WriteLine("Writing launcher.json");
            File.WriteAllText(Path.Combine(outDir, "launcher.json"),
                JsonConvert.SerializeObject(distInfos.First(x => x.Key == "IZEncoder.Launcher.exe").Value,
                    Formatting.Indented));

            if (!string.IsNullOrEmpty(zip))
            {
                Console.WriteLine("Creating archive ...");
                if (File.Exists(zip))
                    File.Delete(zip);
                ZipFile.CreateFromDirectory(outDir, zip);
            }

            Console.WriteLine("Completed");
        }

        private static string CreateMD5Hash(string file)
        {
            using (var stream = File.OpenRead(file))
                using (var md5 = MD5.Create())
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
        }

        //private static void MakeDist(string path, string output)
        //{
        //    path = Path.GetFullPath(path);
        //    output = Path.GetFullPath(output);

        //    if (!Directory.Exists(path))
        //    {
        //        Console.WriteLine("Directory not exists: " + path);
        //        return;
        //    }

        //    var di = new DirectoryInfo(path);
        //    var files = di.GetFiles("*", SearchOption.AllDirectories);
        //    var dists = new Dictionary<string, DistInfo>();
        //    DistInfo launcherInfo = null;

        //    Console.WriteLine("Building dist lists ...");
        //    using (var md5 = MD5.Create())
        //    {
        //        var launcherExe = files.FirstOrDefault(x => x.Name == "IZEncoder.Launcher.exe");
        //        if (launcherExe == null)
        //        {
        //            Console.WriteLine("IZEncoder.Launcher.exe not exists");
        //            return;
        //        }

        //        if (!Directory.Exists(output))
        //            Directory.CreateDirectory(output);

        //        using (var infile = launcherExe.OpenRead())
        //        {
        //            Console.WriteLine(launcherExe);
        //            launcherInfo = new DistInfo
        //            {
        //                Size = launcherExe.Length,
        //                Hash = BitConverter.ToString(md5.ComputeHash(infile)).Replace("-", "").ToLower(),
        //                Time = launcherExe.LastWriteTimeUtc
        //            };

        //            infile.Position = 0;

        //            using (var gzf = File.Create(Path.Combine(output, launcherInfo.Hash)))
        //            {
        //                using (var gz = new GZipStream(gzf,
        //                    CompressionMode.Compress, true))
        //                {
        //                    infile.CopyTo(gz);
        //                }

        //                launcherInfo.CompressSize = gzf.Length;
        //            }

        //            File.WriteAllText(Path.Combine(output, "launcher.json"),
        //                JsonConvert.SerializeObject(launcherInfo, Formatting.Indented));
        //        }

        //        foreach (var fileInfo in files)
        //        {
        //            if (fileInfo == launcherExe)
        //                continue;

        //            using (var stream = fileInfo.OpenRead())
        //            {
        //                Console.WriteLine(fileInfo.FullName);
        //                dists.Add(fileInfo.FullName.Replace(path.Trim('\\') + "\\", ""), new DistInfo
        //                {
        //                    Size = fileInfo.Length,
        //                    Hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower(),
        //                    Time = fileInfo.LastWriteTimeUtc
        //                });
        //            }
        //        }
        //    }

        //    foreach (var dist in dists)
        //        using (var infile = File.OpenRead(Path.Combine(path, dist.Key)))
        //        {
        //            Console.WriteLine($"Compressing: {dist.Key} > {dist.Value.Hash}");

        //            var pathx = Path.Combine(output, dist.Value.Hash);
        //            if (File.Exists(pathx))
        //            {
        //                dist.Value.CompressSize = new FileInfo(pathx).Length;
        //                continue;
        //            }

        //            using (var gzf = File.Create(pathx))
        //            {
        //                using (var gz = new GZipStream(gzf,
        //                    CompressionMode.Compress, true))
        //                {
        //                    infile.CopyTo(gz);
        //                }

        //                dist.Value.CompressSize = gzf.Length;
        //            }
        //        }

        //    foreach (var file in Directory.GetFiles(output))
        //    {
        //        if (file.EndsWith(".json"))
        //            continue;

        //        var name = Path.GetFileName(file);
        //        if (launcherInfo.Hash != name && dists.All(x => x.Value.Hash != name))
        //        {
        //            Console.WriteLine($"{name} Deleted.");
        //            File.Delete(file);
        //        }
        //    }
        //    Console.WriteLine("Building info.json");

        //    File.WriteAllText(Path.Combine(output, "info.json"),
        //        JsonConvert.SerializeObject(dists, Formatting.Indented));
        //}
    }

    public class DistInfo
    {
        public bool CanChange { get; set; }
        public long Size { get; set; }
        public long CompressSize { get; set; }
        public string Hash { get; set; }
        public DateTime Time { get; set; }
    }
}