namespace IZEncoder.Common.Project
{
    using System;

    public class AudioTrackInfomation : PropertyChangedBaseJson
    {
        public int Index { get; set; }
        public string Title { get; set; }
        public string Lang { get; set; }
        public int Lcid { get; set; }
        public bool Default { get; set; }
        public bool Forced { get; set; }

        public string CodecName { get; set; }
        public string CodecFriendly { get; set; }
        public string Format { get; set; }
        public int Channels { get; set; }
        public string ChannelsFriendly { get; set; }
        public double BitRate { get; set; }
        public double SampleRate { get; set; }
        public TimeSpan Duration { get; set; }
    }
}