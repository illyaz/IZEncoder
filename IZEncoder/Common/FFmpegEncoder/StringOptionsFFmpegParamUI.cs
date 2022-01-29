namespace IZEncoder.Common.FFmpegEncoder
{
    using System.Collections.Generic;
    using System.Linq;

    public class StringOptionsFFmpegParamUI : FFmpegParamUIBase
    {
        public string NullText { get; set; } = "NULL";
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

        public override string Validate(object input)
        {
            if (input == null)
                return null;

            return input is string v
                ? Options.Keys.Any(x => x.Equals(v)) ? base.Validate(input)
                : "Invalid options"
                : "Invalid string value";
        }
    }
}