namespace IZEncoder.Common.FFmpegEncoder
{
    public class StringFFmpegParamUI : FFmpegParamUIBase
    {
        public int MaxLines { get; set; } = int.MaxValue;
        public string NullText { get; set; } = "NULL";

        public override string Validate(object input)
        {
            if (input == null)
                return null;

            return input is string
                ? base.Validate(input)
                : "Invalid string value";
        }
    }
}