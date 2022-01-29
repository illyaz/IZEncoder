namespace IZEncoder.Common.FFmpegEncoder
{
    public class FFmpegFormat
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsMuxer { get; set; }
        public bool IsDemuxer { get; set; }
    }
}