namespace IZEncoder.Common.Project
{
    using System;

    public class VideoTrackInfomation : PropertyChangedBaseJson
    {
        public int Index { get; set; }
        public string Title { get; set; }
        public string Lang { get; set; }
        public int Lcid { get; set; }
        public bool Default { get; set; }
        public bool Forced { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public string Format { get; set; }
        public string Codec { get; set; }
        public int BitDepth { get; set; }
        public double BitRate { get; set; }
        public bool Interlaced { get; set; }

        public TimeSpan Duration { get; set; }
        public double FrameRate { get; set; }
        public bool IsVfr { get; set; } // Variable Frame Rate
    }
}