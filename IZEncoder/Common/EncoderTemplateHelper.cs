namespace IZEncoder.Common
{
    using System.IO;
    using Newtonsoft.Json;

    public static class EncoderTemplateHelper
    {
        private static readonly JsonSerializer Serializer;

        static EncoderTemplateHelper()
        {
            Serializer = JsonSerializer.Create();
            Serializer.Formatting = Formatting.Indented;
            Serializer.TypeNameHandling = TypeNameHandling.Auto;
            Serializer.NullValueHandling = NullValueHandling.Ignore;
        }

        public static string Save(this EncoderTemplate filt)
        {
            using (var writer = new StringWriter())
            {
                Save(filt, writer);
                return writer.ToString();
            }
        }

        public static void Save(this EncoderTemplate filt, string path)
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

        public static void Save(this EncoderTemplate filt, TextWriter writer)
        {
            Serializer.Serialize(writer, filt);
        }

        public static EncoderTemplate LoadFromFile(string path)
        {
            return LoadFromStream(File.OpenRead(path));
        }

        public static EncoderTemplate LoadFromString(string json)
        {
            using (var reader = new StringReader(json))
            {
                using (var jreader = new JsonTextReader(reader))
                {
                    return Serializer.Deserialize<EncoderTemplate>(jreader);
                }
            }
        }

        public static EncoderTemplate LoadFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                using (var jreader = new JsonTextReader(reader))
                {
                    return Serializer.Deserialize<EncoderTemplate>(jreader);
                }
            }
        }
    }
}