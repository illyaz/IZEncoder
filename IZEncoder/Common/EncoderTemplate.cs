namespace IZEncoder.Common
{
    using System;
    using System.Collections.Generic;
    using FFmpegEncoder;
    using Newtonsoft.Json;

    public class EncoderTemplate : PropertyChangedBaseJson
    {
        public EncoderTemplate() { }

        public EncoderTemplate(FFmpegVideoParameter vp, FFmpegAudioParameter ap, FFmpegContainerParameter c)
        {
            Name = "Unnamd Encoder Template";
            Video = vp.Guid;
            Audio = ap.Guid;
            Container = c.Guid;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public Guid Guid { get; set; } = Guid.NewGuid();

        public Guid Video { get; set; }
        public Dictionary<string, object> VideoSettings { get; set; } = new Dictionary<string, object>();

        public Guid Audio { get; set; }
        public Dictionary<string, object> AudioSettings { get; set; } = new Dictionary<string, object>();

        public Guid Container { get; set; }
        public Dictionary<string, object> ContainerSettings { get; set; } = new Dictionary<string, object>();

        [JsonIgnore]
        public string Filepath { get; set; }
    }
}