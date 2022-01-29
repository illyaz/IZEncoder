namespace IZEncoder.Common.FFmpegEncoder
{
    public interface IFFmpegParamUI
    {
        string DisplayName { get; set; }
        string Description { get; set; }
        string ToolTip { get; set; }
    }
}