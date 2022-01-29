namespace IZEncoder.Common.Helper
{
    using Newtonsoft.Json;

    public static class ObjectHelper
    {
        private static readonly JsonSerializerSettings _serializerSettings;

        static ObjectHelper()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }

        public static T Populate<T>(this T target, T from)
        {
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(from, _serializerSettings), target,
                _serializerSettings);
            return target;
        }

        public static T DeepCopy<T>(this T target)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(target, _serializerSettings));
        }
    }
}