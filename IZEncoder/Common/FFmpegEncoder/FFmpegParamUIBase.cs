namespace IZEncoder.Common.FFmpegEncoder
{
    using DynamicExpresso;

    public abstract class FFmpegParamUIBase : IFFmpegParamUI, IFFmpegParamValidator
    {
        private static readonly Interpreter Interpreter = new Interpreter();
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string ToolTip { get; set; }
        public string Validator { get; set; }

        public virtual string Validate(object input)
        {
            return string.IsNullOrEmpty(Validator)
                ? null
                : Interpreter.Eval(Validator, new Parameter("Input", input))?.ToString();
        }
    }
}