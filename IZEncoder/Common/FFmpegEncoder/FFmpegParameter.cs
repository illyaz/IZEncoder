namespace IZEncoder.Common.FFmpegEncoder
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public abstract class FFmpegParameter
    {
        [JsonRequired]
        public Guid Guid { get; set; } = Guid.NewGuid();

        [JsonRequired]
        public string Name { get; set; }

        public string Description { get; set; }
        public string Author { get; set; }
        public List<string> Commands { get; set; } = new List<string>();
        public List<FFmpegParam> Params { get; set; } = new List<FFmpegParam>();

        [JsonIgnore]
        public string Path { get; set; }

        [JsonIgnore]
        public bool IsAvailable { get; set; }

        [JsonIgnore]
        public bool IsEditable { get; set; }
    }

    public class FFmpegVideoParameter : FFmpegParameter
    {
        public string Codec { get; set; }
        public string Output { get; set; }
    }

    public class FFmpegAudioParameter : FFmpegParameter
    {
        public string Codec { get; set; }
        public string Output { get; set; }
    }

    public class FFmpegContainerParameter : FFmpegParameter
    {
        public string Format { get; set; }
        public List<string> Inputs { get; set; } = new List<string>();
        public string Output { get; set; }
    }
}