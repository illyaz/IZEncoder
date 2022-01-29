namespace IZEncoder.Common.FFmpegEncoder
{
    public interface IFFmpegParamValidator
    {
        string Validator { get; set; }
        string Validate(object input);
    }
}