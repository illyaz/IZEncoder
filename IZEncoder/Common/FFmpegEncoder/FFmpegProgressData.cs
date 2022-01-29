namespace IZEncoder.Common.FFmpegEncoder {
    using System;
    using Newtonsoft.Json;

    public struct FFmpegProgressData
    {
        public int Frame { get; set; }
        public double Fps { get; set; }
        public double BitRate { get; set; }
        public long TotalSize { get; set; }
        public TimeSpan Time { get; set; }
        public int DuplicateFrames { get; set; }
        public int DropFrames { get; set; }
        public double Speed { get; set; }
        public bool HasNext { get; set; }
        public DateTime ReportTime { get; set; }
    }
}