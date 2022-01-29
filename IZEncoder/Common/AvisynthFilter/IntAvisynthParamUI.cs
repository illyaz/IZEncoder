namespace IZEncoder.Common.AvisynthFilter
{
    public class IntAvisynthParamUI : AvisynthParamUIBase
    {
        public int Interval { get; set; } = 1;
        public int MinValue { get; set; } = int.MinValue;
        public int MaxValue { get; set; } = int.MaxValue;
        public string StringFormat { get; set; }
        public string NullText { get; set; } = "NULL";

        public override string Validate(object input)
        {
            if (input == null || string.IsNullOrEmpty(input.ToString()))
                return null;

            var v = 0;
            var vresult = input is int || int.TryParse(input.ToString(), out v)
                ? base.Validate(v)
                : "Invalid int value";

            if (vresult != null)
                return vresult;

            if (!(v >= MinValue && v <= MaxValue))
                return $"Value out of range {MinValue}-{MaxValue}";

            return null;
        }
    }
}