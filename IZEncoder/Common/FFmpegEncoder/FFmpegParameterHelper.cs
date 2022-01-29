namespace IZEncoder.Common.FFmpegEncoder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using DynamicExpresso;
    using Newtonsoft.Json;

    public static class FFmpegParameterHelper
    {
        private static readonly JsonSerializer Serializer;

        static FFmpegParameterHelper()
        {
            Serializer = JsonSerializer.Create();
            Serializer.Formatting = Formatting.Indented;
            Serializer.TypeNameHandling = TypeNameHandling.Auto;
            Serializer.NullValueHandling = NullValueHandling.Ignore;
        }

        public static string Build(this FFmpegParameter enc)
        {
            return enc.Build(new Dictionary<string, object>(), 0);
        }

        public static string Build(this FFmpegParameter enc, Dictionary<string, object> values, int cmdIndex)
        {
            values = values ?? new Dictionary<string, object>();

            var missingRequiredProps =
                enc.Params?.Where(x => x.Default == null).Where(a => !values.Select(x => x.Key).Contains(a.Name))
                    .ToList() ?? new List<FFmpegParam>();

            if (missingRequiredProps.Count > 0)
                throw new Exception(
                    $"'{string.Join(",", missingRequiredProps.Select(x => x.Name))}' required");

            var interpreter = new Interpreter();

            foreach (var param in enc.Params)
                if (values.ContainsKey(param.Name))
                    interpreter.SetVariable(param.Name,
                        param.GetValue(values[param.Name]?.ToString()));
                else if (param.Default != null)
                    interpreter.SetVariable(param.Name,
                        param.GetValue(param.Default));

            var regx = new Regex(@"{([^}]*)}");
            var regx2 = new Regex(@"(?<=\{)[^}]*(?=\})");

            var cmd = string.Empty;

            switch (enc)
            {
                case FFmpegVideoParameter ve:
                    cmd = $"-c:v {ve.Codec} {enc.Commands[cmdIndex]}";
                    break;
                case FFmpegAudioParameter ae:
                    cmd = $"-c:a {ae.Codec} {enc.Commands[cmdIndex]}";
                    break;
                case FFmpegContainerParameter ce:
                    cmd = $"{enc.Commands[cmdIndex]} -f {ce.Format}";
                    break;
            }

            return regx.Replace(cmd, r =>
            {
                if (r.Value.Trim().StartsWith("{{") && r.Value.Trim().EndsWith("}}"))
                    return r.Value;

                var result = interpreter.Parse(regx2.Match(r.Value).Groups[0].Value).Invoke();
                return result?.ToString() ?? "";
            }).Trim();
        }

        public static Dictionary<string, object> CreateDefaultSettings(this FFmpegParameter enc)
        {
            if (enc == null)
                throw new ArgumentNullException(nameof(enc));

            return enc.Params?.Where(x => x.Default != null).ToDictionary(k => k.Name, v => v.GetValue(v.Default)) ??
                   new Dictionary<string, object>();
        }

        public static T Find<T>(this IEnumerable<FFmpegParameter> collection, Guid id, bool includeNotAvailable = false)
            where T : FFmpegParameter
        {
            return includeNotAvailable
                ? collection.OfType<T>().FirstOrDefault(x => x.Guid == id)
                : collection.OfType<T>().Where(x => x.IsAvailable).FirstOrDefault(x => x.Guid == id);
        }

        public static string Save(this FFmpegParameter filt)
        {
            using (var writer = new StringWriter())
            {
                Save(filt, writer);
                return writer.ToString();
            }
        }

        public static void Save(this FFmpegParameter filt, string path)
        {
            using (var stream = File.OpenWrite(path))
            {
                stream.SetLength(0);
                using (var writer = new StreamWriter(stream))
                {
                    Save(filt, writer);
                }
            }
        }

        public static void Save(this FFmpegParameter filt, TextWriter writer)
        {
            Serializer.Serialize(writer, filt);
        }

        public static T LoadFromFile<T>(string path)
            where T : FFmpegParameter
        {
            return LoadFromStream<T>(File.OpenRead(path));
        }

        public static T LoadFromString<T>(string json)
            where T : FFmpegParameter
        {
            using (var reader = new StringReader(json))
            {
                using (var jreader = new JsonTextReader(reader))
                {
                    return Serializer.Deserialize<T>(jreader);
                }
            }
        }

        public static T LoadFromStream<T>(Stream stream)
            where T : FFmpegParameter
        {
            using (var reader = new StreamReader(stream))
            {
                using (var jreader = new JsonTextReader(reader))
                {
                    return Serializer.Deserialize<T>(jreader);
                }
            }
        }
    }
}