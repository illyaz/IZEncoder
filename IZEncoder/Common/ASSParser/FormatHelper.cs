namespace IZEncoder.Common.ASSParser
{
    using System;
    using System.Globalization;

    internal static class FormatHelper
    {
        private static readonly char[] retChar = {'\n', '\r'};

        private static readonly char[] split = {':'};

        public static IFormatProvider DefaultFormat { get; } = CultureInfo.InvariantCulture;

        public static bool SingleLineStringValueValid(ref string value)
        {
            if (value == null)
                return false;
            value = value.Trim();
            if (value.Length == 0)
                return false;
            if (value.IndexOfAny(retChar) != -1)
                throw new ArgumentException("value must be single line.", nameof(value));
            return true;
        }

        public static bool FieldStringValueValid(ref string value)
        {
            if (SingleLineStringValueValid(ref value))
            {
                value = value.Replace(',', ';');
                return true;
            }

            return false;
        }

        public static bool TryPraseLine(out string key, out string value, string rawString)
        {
            if (string.IsNullOrEmpty(rawString) || rawString.IndexOf(':') == -1)
            {
                key = value = null;
                return false;
            }

            var s = rawString.Split(split, 2);
            key = s[0].TrimEnd(null);
            value = s[1].TrimStart(null);
            return true;
        }
    }
}