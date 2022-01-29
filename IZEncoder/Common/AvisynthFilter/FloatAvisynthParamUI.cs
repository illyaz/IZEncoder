namespace IZEncoder.Common.AvisynthFilter
{
    public class FloatAvisynthParamUI : AvisynthParamUIBase
    {
        public float Interval { get; set; } = 0.5f;
        public float MinValue { get; set; } = float.MinValue;
        public float MaxValue { get; set; } = float.MaxValue;
        public string StringFormat { get; set; } = "{0:N3}";
        public string NullText { get; set; } = "NULL";

        public override string Validate(object input)
        {
            if (input == null || string.IsNullOrEmpty(input.ToString()))
                return null;

            var vresult = float.TryParse(input.ToString(), out var v)
                ? base.Validate(v)
                : "Invalid float value";

            if (vresult != null)
                return vresult;

            if (!(v >= MinValue && v <= MaxValue))
                return $"Value out of range {MinValue}-{MaxValue}";

            return null;
        }
    }
}