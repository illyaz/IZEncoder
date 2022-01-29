namespace IZEncoder.Common.AvisynthFilter
{
    public class IntPointAvisynthParamUI : AvisynthParamUIBase, IAvisynthParamUIExtented
    {
        public int Interval { get; set; } = 1;
        public int MinValueX { get; set; } = int.MinValue;
        public int MaxValueX { get; set; } = int.MaxValue;
        public int MinValueY { get; set; } = int.MinValue;
        public int MaxValueY { get; set; } = int.MaxValue;
        public string StringFormatX { get; set; }
        public string StringFormatY { get; set; }
        public string NullTextX { get; set; } = "NULL";
        public string NullTextY { get; set; } = "NULL";

        public string NameY
        {
            get => ExtentedParamNames[0];
            set => ExtentedParamNames[0] = value;
        }

        public string[] ExtentedParamNames { get; set; } = new string[1];

        public override string Validate(object input)
        {
            if (input == null || string.IsNullOrEmpty(input.ToString()))
                return null;

            var vresult = input is IntPoint v || IntPoint.TryParse(input.ToString(), out v)
                ? base.Validate(v)
                : "Invalid int-point value";

            if (vresult != null)
                return vresult;

            if (v.X != null && !(v.X >= MinValueX && v.X <= MaxValueX))
                return $"X Value out of range {MinValueX}:{MaxValueX}";

            if (v.Y != null && !(v.Y >= MinValueX && v.Y <= MaxValueY))
                return $"Y Value out of range {MinValueY}:{MaxValueY}";

            return null;
        }
    }
}