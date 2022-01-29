namespace IZEncoder.Common.AvisynthFilter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class AvisynthParam
    {
        private string _default;

        public AvisynthParam() { }

        public AvisynthParam(string name, AvisynthParamType type, object def = null)
        {
            Name = name;
            Type = type;
            Default = def?.ToString();
        }

        public string Name { get; set; }
        public AvisynthParamType Type { get; set; }

        public string Default
        {
            get => _default;
            set => _default = value != null ? GetValue(value).ToString() : null;
        }

        public bool IgnoreDefault { get; set; }
        public bool IsRequired { get; set; }

        public AvisynthParamUIBase UI { get; set; }

        public static IEnumerable<AvisynthParam> Parse(string paramString)
        {
            var namedSection = false;
            var namedData = string.Empty;
            for (var i = 0; i < paramString.Length; i++)
            {
                var c = paramString[i];
                if (c == '[')
                {
                    if (namedSection)
                        throw new FormatException($"Expected identifier '[' at position {i}");

                    namedSection = true;
                    namedData = string.Empty;
                    continue;
                }

                if (c == ']')
                {
                    if (!namedSection)
                        throw new FormatException($"Expected identifier ']' at position {i}");

                    namedSection = false;
                    continue;
                }

                if (namedSection)
                {
                    namedData += c;
                    continue;
                }

                var ap = new AvisynthParam {Name = string.IsNullOrEmpty(namedData) ? null : namedData};
                if (ap.Name != null)
                    namedData = string.Empty;
                ap.Type = GetParamType(c);
                if (ap.Type == AvisynthParamType.Unknown)
                    throw new FormatException($"Expected identifier '{c}' at position {i}");

                if (i + 1 < paramString.Length)
                {
                    var ec = paramString[i + 1];
                    if (!(ec == '[' || ec == ']'))
                    {
                        var t = GetParamType(ec);
                        switch (t)
                        {
                            case AvisynthParamType.Unknown:
                                throw new FormatException($"Expected identifier '{ec}' at position {i + 1}");
                            case AvisynthParamType.EmptyOrMore:
                            case AvisynthParamType.OneOrMore:
                                ap.Type |= t;
                                i++;
                                break;
                        }
                    }
                }

                yield return ap;
            }
        }

        private static AvisynthParamType GetParamType(char c)
        {
            switch (c)
            {
                case '.':
                    return AvisynthParamType.Any;
                case '+':
                    return AvisynthParamType.EmptyOrMore;
                case '*':
                    return AvisynthParamType.OneOrMore;
                case 'c':
                    return AvisynthParamType.Clip;
                case 'i':
                    return AvisynthParamType.Int;
                case 'f':
                    return AvisynthParamType.Float;
                case 'b':
                    return AvisynthParamType.Boolean;
                case 's':
                    return AvisynthParamType.String;
                default:
                    return AvisynthParamType.Unknown;
            }
        }

        public object GetValue(string input)
        {
            switch (Type)
            {
                case AvisynthParamType.Color:
                {
                    // Int color
                    if (int.TryParse(input, out var result) && IsValidIntColor(result))
                        return result;

                    switch (input.Length)
                    {
                        // Hex color
                        case 6 when int.TryParse(input, NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture, out result):
                            return result;
                        // Html/Avisynth Color
                        case 7 when (input[0] == '$' || input[0] == '#') && int.TryParse(input.Substring(1),
                                        NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                                        out result):
                            return result;
                    }

                    throw new FormatException("Invalid color value");
                }
                case AvisynthParamType.Int:
                {
                    return int.TryParse(input, out var result)
                        ? result
                        : throw new FormatException("Invalid int value");
                }
                case AvisynthParamType.Float:
                {
                    return float.TryParse(input, out var result)
                        ? result
                        : throw new FormatException("Invalid float value");
                }
                case AvisynthParamType.Boolean:
                {
                    return bool.TryParse(input, out var result)
                        ? result
                        : throw new FormatException("Invalid boolean value");
                }
                case AvisynthParamType.String:
                    return input;
                default:
                    return input;
            }
        }

        public string GetAvisynthValue(string input)
        {
            switch (Type)
            {
                case AvisynthParamType.Color:
                    return $"${GetValue(input):X6}";
                case AvisynthParamType.String:
                    return $"\"\"\"{GetValue(input)}\"\"\"";
                default:
                    return GetValue(input).ToString();
            }
        }

        private bool IsValidIntColor(int color)
        {
            return color >= 0 && color <= 16777215;
        }
    }

    [Flags]
    public enum AvisynthParamType
    {
        Unknown = 0,
        Any = 1 << 0,
        Clip = 1 << 1,
        Color = 1 << 2,
        Int = 1 << 3,
        Float = 1 << 4,
        Boolean = 1 << 5,
        String = 1 << 6,
        EmptyOrMore = 1 << 7,
        OneOrMore = 1 << 8
    }
}