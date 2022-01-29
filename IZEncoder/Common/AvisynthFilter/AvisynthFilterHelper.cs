namespace IZEncoder.Common.AvisynthFilter
{
    using System.IO;
    using Newtonsoft.Json;

    internal static class AvisynthFilterHelper
    {
        private static readonly JsonSerializer Serializer;

        static AvisynthFilterHelper()
        {
            Serializer = JsonSerializer.Create();
            Serializer.Formatting = Formatting.Indented;
            Serializer.TypeNameHandling = TypeNameHandling.Auto;
            Serializer.NullValueHandling = NullValueHandling.Ignore;
        }

        public static string Save(this AvisynthFilter filt)
        {
            using (var writer = new StringWriter())
            {
                Save(filt, writer);
                return writer.ToString();
            }
        }

        public static void Save(this AvisynthFilter filt, string path)
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

        public static void Save(this AvisynthFilter filt, TextWriter writer)
        {
            Serializer.Serialize(writer, filt);
        }

        public static AvisynthFilter LoadFromFile(string path)
        {
            return LoadFromStream(File.OpenRead(path));
        }

        public static AvisynthFilter LoadFromString(string json)
        {
            using (var reader = new StringReader(json))
            {
                using (var jreader = new JsonTextReader(reader))
                {
                    return Serializer.Deserialize<AvisynthFilter>(jreader);
                }
            }
        }

        public static AvisynthFilter LoadFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                using (var jreader = new JsonTextReader(reader))
                {
                    return Serializer.Deserialize<AvisynthFilter>(jreader);
                }
            }
        }
    }
}