namespace IZEncoder.Common.FFmpegEncoder
{
    public class FFmpegEncoder
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool FrameLevelMultiThreading { get; set; }
        public bool SliceLevelMultiThreading { get; set; }
        public bool IsExperimental { get; set; }
        public FFmpegEncoderTypes Type { get; set; }
    }
}