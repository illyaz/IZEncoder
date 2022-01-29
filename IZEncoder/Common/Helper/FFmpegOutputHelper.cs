namespace IZEncoder.Common.Helper
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public static class FFmpegOutputHelper
    {
        private static readonly Regex FFMpegProgressOutputRegex = new Regex(@"(?<key>\w+)=(?<val>\s*\S+.*?)");

        public static Dictionary<string, string> GetDictionary(string output)
        {
            var m = FFMpegProgressOutputRegex.Match(output);
            var vals = new Dictionary<string, string>();
            while (m.Success)
            {
                var key = m.Groups["key"].Value.Trim();
                var val = m.Groups["val"].Value.Trim();

                if (vals.ContainsKey(key))
                    vals[key] = val;
                else
                    vals.Add(key, val);
                m = m.NextMatch();
            }

            return vals;
        }
    }
}