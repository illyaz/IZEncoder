namespace IZEncoder.Common.FFmpegEncoder
{
    public class BooleanFFmpegParamUI : FFmpegParamUIBase
    {
        public override string Validate(object input)
        {
            if (input == null || string.IsNullOrEmpty(input.ToString()))
                return null;

            return input is bool || bool.TryParse(input.ToString(), out var result)
                ? base.Validate(input)
                : "Invalid boolean value";
        }
    }
}