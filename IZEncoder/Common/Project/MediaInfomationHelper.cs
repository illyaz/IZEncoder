namespace IZEncoder.Common.Project
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using IZEncoderNative.Avisynth;
    using Helper;
    using JetBrains.Annotations;
    using MediaInfo;

    public static class MediaInfomationHelper
    {
        public static string GetFFIndexPath(this MediaInfomation media, Config config)
        {
            return Path.GetFullPath(Path.Combine(config.Application.TempPath, "ffindex", media.Filename.CRC32String()));
        }

        public static MediaInfomation Create(string file)
        {
            var mediaInfomation = new MediaInfomation();
            var info = new MediaInfoWrapper(file);

            if (info.VideoStreams.Any())
                foreach (var videoStream in info.VideoStreams)
                    mediaInfomation.VideoTracks.Add(new VideoTrackInfomation
                    {
                        Index = videoStream.Id - 1,
                        Title = videoStream.Name,
                        Lang = videoStream.Language,
                        Lcid = videoStream.Lcid,
                        Default = videoStream.Default,
                        Forced = videoStream.Forced,

                        Width = videoStream.Width,
                        Height = videoStream.Height,
                        Codec = videoStream.CodecName,
                        Format = videoStream.Format,
                        BitDepth = videoStream.BitDepth,
                        BitRate = videoStream.Bitrate,
                        Interlaced = videoStream.Interlaced,
                        Duration = videoStream.Duration,
                        FrameRate = videoStream.FrameRate,
                        IsVfr = videoStream.IsVfr
                    });

            if (info.AudioStreams.Any())
                foreach (var audioStream in info.AudioStreams)
                    mediaInfomation.AudioTracks.Add(new AudioTrackInfomation
                    {
                        Index = audioStream.Id - 1,
                        Title = audioStream.Name,
                        Lang = audioStream.Language,
                        Lcid = audioStream.Lcid,
                        Default = audioStream.Default,
                        Forced = audioStream.Forced,

                        CodecName = audioStream.CodecName,
                        CodecFriendly = audioStream.CodecFriendly,
                        Format = audioStream.Format,
                        Channels = audioStream.Channel,
                        ChannelsFriendly = audioStream.AudioChannelsFriendly,
                        BitRate = audioStream.Bitrate,
                        SampleRate = audioStream.SamplingRate,
                        Duration = audioStream.Duration
                    });

            var fi = new FileInfo(file);
            mediaInfomation.Filename = fi.FullName;
            mediaInfomation.Filesize = fi.Length;
            mediaInfomation.FileExtension =
                string.IsNullOrEmpty(fi.Extension) ? null : fi.Extension.Substring(1).ToLower();
            return mediaInfomation;
        }

        public static MediaInfomation Create(string file, AvisynthBridge bridge)
        {
            var mediaInfomation = new MediaInfomation();
            var guid = Guid.NewGuid().ToString();
            var clp = bridge.CreateClip(guid, "Import", file);

            if (clp.Info.HasVideo())
                mediaInfomation.VideoTracks.Add(new VideoTrackInfomation
                {
                    Index = 0,
                    Title = null,
                    Lang = null,
                    Lcid = -1,
                    Default = true,
                    Forced = true,

                    Width = clp.Info.Width,
                    Height = clp.Info.Height,
                    Codec = "avisynth",
                    Format = "avs",
                    BitDepth = 0,
                    BitRate = 0,
                    Interlaced = false,
                    Duration = TimeSpan.FromSeconds(clp.Info.Frames / clp.Info.FrameRate()),
                    FrameRate = clp.Info.FrameRate(),
                    IsVfr = false
                });

            if (clp.Info.HasAudio())
                mediaInfomation.AudioTracks.Add(new AudioTrackInfomation
                {
                    Index = 0,
                    Title = null,
                    Lang = null,
                    Lcid = -1,
                    Default = true,
                    Forced = true,

                    CodecName = "PCM",
                    CodecFriendly = "PCM",
                    Format = "PCM",
                    Channels = clp.Info.Channels,
                    ChannelsFriendly = clp.Info.Channels.ToString(),
                    BitRate = 0,
                    SampleRate = clp.Info.SampleRate,
                    Duration = TimeSpan.FromSeconds((double)clp.Info.Samples / clp.Info.SampleRate)
                });

            bridge.Remove(guid);
            var fi = new FileInfo(file);
            mediaInfomation.Filename = fi.FullName;
            mediaInfomation.Filesize = fi.Length;
            mediaInfomation.FileExtension =
                string.IsNullOrEmpty(fi.Extension) ? null : fi.Extension.Substring(1).ToLower();
            return mediaInfomation;
        }

        public static Task<MediaInfomation> CreateAsync(string file)
        {
            return Task.Factory.StartNew(() => Create(file));
        }

        public static Task<MediaInfomation> CreateAsync(string file, AvisynthBridge bridge)
        {
            return Task.Factory.StartNew(() => Create(file, bridge));
        }

        public static int? GetDefaultAudioTrack([NotNull] this MediaInfomation mediaInfomation)
        {
            if (mediaInfomation == null) throw new ArgumentNullException(nameof(mediaInfomation));
            return mediaInfomation.AudioTracks.Any()
                ? mediaInfomation.AudioTracks.FirstOrDefault(x => x.Default || x.Forced)?.Index
                : null;
        }
    }
}