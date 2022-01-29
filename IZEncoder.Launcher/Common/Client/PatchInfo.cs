namespace IZEncoder.Launcher.Common.Client
{
    using System;
    using Newtonsoft.Json;

    public class PatchInfo
    {
        public bool CanChange { get; set; }
        public long Size { get; set; }
        public long CompressSize { get; set; }
        public string Hash { get; set; }
        public DateTime Time { get; set; }

        [JsonIgnore]
        public bool IsMatched { get; set; }

        [JsonIgnore]
        public bool Exists { get; set; }
    }
}