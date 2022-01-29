namespace IZEncoder.Common.Project
{
    using System.IO;
    using Newtonsoft.Json;

    public static class AvisynthProjectHelper
    {
        private static readonly JsonSerializer Serializer;

        static AvisynthProjectHelper()
        {
            Serializer = JsonSerializer.Create();
            Serializer.Formatting = Formatting.Indented;
            Serializer.TypeNameHandling = TypeNameHandling.Auto;
            Serializer.NullValueHandling = NullValueHandling.Ignore;
        }

        public static string Save(this AvisynthProject filt)
        {
            using (var writer = new StringWriter())
            {
                Save(filt, writer);
                return writer.ToString();
            }
        }

        public static void Save(this AvisynthProject filt, string path)
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

        public static void Save(this AvisynthProject filt, TextWriter writer)
        {
            Serializer.Serialize(writer, filt);
        }

        public static AvisynthProject LoadFromFile(string path)
        {
            return LoadFromStream(File.OpenRead(path));
        }

        public static AvisynthProject LoadFromString(string json)
        {
            using (var reader = new StringReader(json))
            {
                var ap = new AvisynthProject();
                ap.Filters.Clear();
                Serializer.Populate(reader, ap);
                return ap;
            }
        }

        public static AvisynthProject LoadFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var ap = new AvisynthProject();
                ap.Filters.Clear();
                Serializer.Populate(reader, ap);
                return ap;
            }
        }
    }
}