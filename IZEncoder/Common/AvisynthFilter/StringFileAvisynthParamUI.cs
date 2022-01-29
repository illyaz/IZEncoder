namespace IZEncoder.Common.AvisynthFilter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class StringFileAvisynthParamUI : AvisynthParamUIBase
    {
        public string NullText { get; set; }
        public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();

        public override string Validate(object input)
        {
            if (input == null)
                return null;

            return input is string s
                ? File.Exists(s)
                    ? Filters.Keys.Where(x => !x.Equals("_ALL_", StringComparison.OrdinalIgnoreCase)).Any(x =>
                        Path.GetExtension(s).Equals("." + x, StringComparison.OrdinalIgnoreCase))
                        ? base.Validate(s)
                        : "Invalid file extension"
                    : "File not exist"
                : "Invalid string value";
        }
    }
}