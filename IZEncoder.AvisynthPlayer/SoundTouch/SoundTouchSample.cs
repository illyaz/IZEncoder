namespace IZEncoder.AvisynthPlayer.SourceTouch
{
    using CSCore;

    public class SoundTouchSample : SampleAggregatorBase
    {
        private readonly int channelCount;

        private readonly SoundTouch soundTouch;
        private readonly float[] soundTouchReadBuffer;
        private readonly float[] sourceReadBuffer;
        private SoundTouchProfile currentSoundTouchProfile;
        private float playbackRate = 1.0f;
        private bool repositionRequested;

        public SoundTouchSample(ISampleSource sourceProvider, int readDurationMilliseconds,
            SoundTouchProfile soundTouchProfile)
            : base(sourceProvider)
        {
            soundTouch = new SoundTouch();

            SetSoundTouchProfile(soundTouchProfile);
            soundTouch.SetSampleRate(WaveFormat.SampleRate);
            channelCount = WaveFormat.Channels;
            soundTouch.SetChannels(channelCount);
            sourceReadBuffer = new float[WaveFormat.SampleRate * channelCount * (long) readDurationMilliseconds / 1000];
            soundTouchReadBuffer = new float[sourceReadBuffer.Length * 10]; // support down to 0.1 speed
        }

        public float PlaybackRate
        {
            get => playbackRate;
            set
            {
                if (playbackRate != value)
                {
                    UpdatePlaybackRate(value);
                    playbackRate = value;
                }
            }
        }

        public float Volume { get; set; } = 1.0f;

        public bool Mute { get; set; } = false;

        public override int Read(float[] buffer, int offset, int count)
        {
            if (playbackRate == 0 || Mute) // play silence
            {
                for (var n = 0; n < count; n++) buffer[offset++] = 0;
                return count;
            }

            if (repositionRequested)
            {
                soundTouch.Clear();
                repositionRequested = false;
            }

            var samplesRead = 0;
            var reachedEndOfSource = false;
            while (samplesRead < count)
            {
                if (soundTouch.NumberOfSamplesAvailable == 0)
                {
                    var readFromSource = BaseSource.Read(sourceReadBuffer, 0, sourceReadBuffer.Length);
                    if (readFromSource > 0)
                    {
                        soundTouch.PutSamples(sourceReadBuffer, readFromSource / channelCount);
                    }
                    else
                    {
                        reachedEndOfSource = true;
                        // we've reached the end, tell SoundTouch we're done
                        soundTouch.Flush();
                    }
                }

                var desiredSampleFrames = (count - samplesRead) / channelCount;

                var received = soundTouch.ReceiveSamples(soundTouchReadBuffer, desiredSampleFrames) * channelCount;
                // use loop instead of Array.Copy due to WaveBuffer
                for (var n = 0; n < received; n++) buffer[offset + samplesRead++] = soundTouchReadBuffer[n] * Volume;
                if (received == 0 && reachedEndOfSource) break;
            }

            return samplesRead;
        }

        private void UpdatePlaybackRate(float value)
        {
            if (value != 0)
            {
                if (currentSoundTouchProfile.UseTempo)
                    soundTouch.SetTempo(value);
                else
                    soundTouch.SetRate(value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            soundTouch?.Dispose();
            base.Dispose(disposing);
        }

        public void SetSoundTouchProfile(SoundTouchProfile soundTouchProfile)
        {
            if (currentSoundTouchProfile != null &&
                playbackRate != 1.0f &&
                soundTouchProfile.UseTempo != currentSoundTouchProfile.UseTempo)
            {
                if (soundTouchProfile.UseTempo)
                {
                    soundTouch.SetRate(1.0f);
                    soundTouch.SetPitchOctaves(0f);
                    soundTouch.SetTempo(playbackRate);
                }
                else
                {
                    soundTouch.SetTempo(1.0f);
                    soundTouch.SetRate(playbackRate);
                }
            }

            currentSoundTouchProfile = soundTouchProfile;
            soundTouch.SetUseAntiAliasing(soundTouchProfile.UseAntiAliasing);
            soundTouch.SetUseQuickSeek(soundTouchProfile.UseQuickSeek);
        }

        public void Reposition()
        {
            repositionRequested = true;
        }
    }
}