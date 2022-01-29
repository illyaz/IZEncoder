namespace IZEncoder.Common.AvisynthFilter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    public class AvisynthFilter
    {
        public AvisynthFilter() { }

        public AvisynthFilter(string name)
        {
            Name = name;
        }

        [JsonRequired]
        public Guid Guid { get; set; } = Guid.NewGuid();

        [JsonRequired]
        public string Name { get; set; }

        public string DisplayName { get; set; }
        public string Category { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, FilterMtModes> MtModes { get; set; } = new Dictionary<string, FilterMtModes>();

        public string Description { get; set; }
        public string Version { get; set; }

        [JsonRequired]
        public List<AvisynthParam> Params { get; set; } = new List<AvisynthParam>();

        public string GetEvalString(Dictionary<string, object> config)
        {
            return GetEvalString(null, config);
        }

        public string GetEvalString(string clipName = null, Dictionary<string, object> config = null)
        {
            var p = new List<string>();

            foreach (var avisynthParam in Params.Where(x => x.IsRequired))
            {
                if (config != null && config.ContainsKey(avisynthParam.Name) &&
                    config[avisynthParam.Name] != null)
                    continue;

                throw new Exception($"param '{avisynthParam.Name}' is required");
            }

            var ix = 0;
            for (var i = 0; i < Params.Count; i++)
            {
                var avisynthParam = Params[i];
                if (config != null && config.ContainsKey(avisynthParam.Name))
                {
                    var paramRaw = config[avisynthParam.Name]?.ToString();
                    var paramString = paramRaw != null ? avisynthParam.GetAvisynthValue(paramRaw) : null;
                    if (paramString == null || avisynthParam.IgnoreDefault && avisynthParam.Default != null &&
                        paramString == avisynthParam.GetAvisynthValue(avisynthParam.Default))
                        continue;

                    p.Add(ix++ != i ? $"{avisynthParam.Name}={paramString}" : paramString);
                }
                else
                {
                    if (avisynthParam.IgnoreDefault || avisynthParam.Default == null)
                        continue;

                    var paramString = avisynthParam.GetAvisynthValue(avisynthParam.Default);
                    p.Add(ix++ != i ? $"{avisynthParam.Name}={paramString}" : paramString);
                }
            }

            return $"{Name}({(string.IsNullOrEmpty(clipName) ? "" : $"{clipName}, ")}{string.Join(", ", p)})";
        }
    }
}