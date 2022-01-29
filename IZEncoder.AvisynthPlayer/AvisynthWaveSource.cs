namespace IZEncoder.AvisynthPlayer
{
    using System;
    using IZEncoderNative.Avisynth;
    using CSCore;

    public class AvisynthWaveSource : IWaveSource
    {
        private readonly object _lock = new object();
        protected AvisynthClip Clip;

        public AvisynthWaveSource(AvisynthClip clip, int? overrideSampleRate = null)
        {
            Clip = clip;
            WaveFormat = new WaveFormat(overrideSampleRate ?? Clip.Info.SampleRate,
                Clip.Info.BytesPerChannelSample() * 8,
                Clip.Info.Channels,
                Clip.Info.SampleType == AudioSampleType.SAMPLE_FLOAT ? AudioEncoding.IeeeFloat : AudioEncoding.Pcm);
        }

        public object SyncRoot { get; } = new object();

        public bool CanSeek => true;

        public WaveFormat WaveFormat { get; }

        public long Position { get; set; }

        public long Length => Clip.Info.Samples * Clip.Info.BytesPerAudioSample();

        public int Read(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                var numRead =
                    Math.Max(0, (int) Clip.GetAudio(buffer, (Position + offset) / Clip.Info.BytesPerAudioSample(),
                                    count / Clip.Info.BytesPerAudioSample()) * Clip.Info.BytesPerAudioSample());

                Position += numRead;
                return numRead;
            }
        }

        public void Dispose() { }

        public void ReplaceClip(AvisynthClip newClip)
        {
            lock (_lock)
            {
                Clip = newClip;
            }

            //Info = newClip.GetVideoInfo();
        }
    }
}