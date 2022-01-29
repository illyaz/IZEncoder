namespace IZEncoder.Common.FFmpegEncoder
{
    public enum FFmpegParamType
    {
        Unknown = 0,
        Int = 1 << 0,
        Float = 1 << 1,
        Boolean = 1 << 2,
        String = 1 << 3
    }
}