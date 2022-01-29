namespace IZEncoder.Common.FFmpegEncoder
{
    using System;

    public class FFmpegParam
    {
        private string _default;

        public FFmpegParam() { }

        public FFmpegParam(string name, FFmpegParamType type, object def = null)
        {
            Name = name;
            Type = type;
            Default = def?.ToString();
        }

        public string Name { get; set; }
        public FFmpegParamType Type { get; set; }

        public string Default
        {
            get => _default;
            set => _default = value != null ? GetValue(value).ToString() : null;
        }

        public bool IsRequired { get; set; }
        public FFmpegParamUIBase UI { get; set; }

        public object GetValue(string input)
        {
            if (input == null)
                return null;

            switch (Type)
            {
                case FFmpegParamType.Int:
                {
                    return int.TryParse(input, out var result)
                        ? result
                        : throw new FormatException("Invalid int value");
                }
                case FFmpegParamType.Float:
                {
                    return float.TryParse(input, out var result)
                        ? result
                        : throw new FormatException("Invalid float value");
                }
                case FFmpegParamType.Boolean:
                {
                    return bool.TryParse(input, out var result)
                        ? result
                        : throw new FormatException("Invalid boolean value");
                }
                case FFmpegParamType.String:
                    return input;
                default:
                    return input;
            }
        }
    }
}