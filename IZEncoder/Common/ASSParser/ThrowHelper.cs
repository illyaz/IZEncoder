namespace IZEncoder.Common.ASSParser
{
    internal static class ThrowHelper
    {
        public static bool IsLessThanZeroOrOutOfRange(int max, int value)
        {
            unchecked
            {
                return (uint) value >= (uint) max;
            }
        }

        public static bool IsInvalidDouble(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value);
        }
    }
}