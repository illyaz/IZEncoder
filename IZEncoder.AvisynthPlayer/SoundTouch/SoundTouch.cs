namespace IZEncoder.AvisynthPlayer.SourceTouch
{
    using System;
    using System.Text;

    public class SoundTouch : IDisposable
    {
        private IntPtr handle;
        private string versionString;

        public SoundTouch()
        {
            handle = SoundTouchInterop32.soundtouch_createInstance();
        }

        public string VersionString
        {
            get
            {
                if (versionString == null)
                {
                    var s = new StringBuilder(100);
                    SoundTouchInterop32.soundtouch_getVersionString2(s, s.Capacity);
                    versionString = s.ToString();
                }

                return versionString;
            }
        }

        public bool IsEmpty
            => SoundTouchInterop32.soundtouch_isEmpty(handle) != 0;

        public int NumberOfSamplesAvailable
            => (int) SoundTouchInterop32.soundtouch_numSamples(handle);

        public int NumberOfUnprocessedSamples
            => SoundTouchInterop32.soundtouch_numUnprocessedSamples(handle);

        public void Dispose()
        {
            DestroyInstance();
            GC.SuppressFinalize(this);
        }

        public void SetPitchOctaves(float pitchOctaves)
        {
            SoundTouchInterop32.soundtouch_setPitchOctaves(handle, pitchOctaves);
        }

        public void SetSampleRate(int sampleRate)
        {
            SoundTouchInterop32.soundtouch_setSampleRate(handle, (uint) sampleRate);
        }

        public void SetChannels(int channels)
        {
            SoundTouchInterop32.soundtouch_setChannels(handle, (uint) channels);
        }

        private void DestroyInstance()
        {
            if (handle != IntPtr.Zero)
            {
                SoundTouchInterop32.soundtouch_destroyInstance(handle);
                handle = IntPtr.Zero;
            }
        }

        ~SoundTouch()
        {
            DestroyInstance();
        }

        public void PutSamples(float[] samples, int numSamples)
        {
            SoundTouchInterop32.soundtouch_putSamples(handle, samples, numSamples);
        }

        public int ReceiveSamples(float[] outBuffer, int maxSamples)
        {
            return (int) SoundTouchInterop32.soundtouch_receiveSamples(handle, outBuffer, (uint) maxSamples);
        }

        public void Flush()
        {
            SoundTouchInterop32.soundtouch_flush(handle);
        }

        public void Clear()
        {
            SoundTouchInterop32.soundtouch_clear(handle);
        }

        public void SetRate(float newRate)
        {
            SoundTouchInterop32.soundtouch_setRate(handle, newRate);
        }

        public void SetTempo(float newTempo)
        {
            SoundTouchInterop32.soundtouch_setTempo(handle, newTempo);
        }

        public int GetUseAntiAliasing()
        {
            return SoundTouchInterop32.soundtouch_getSetting(handle, SoundTouchSettings.UseAaFilter);
        }

        public void SetUseAntiAliasing(bool useAntiAliasing)
        {
            SoundTouchInterop32.soundtouch_setSetting(handle, SoundTouchSettings.UseAaFilter, useAntiAliasing ? 1 : 0);
        }

        public void SetUseQuickSeek(bool useQuickSeek)
        {
            SoundTouchInterop32.soundtouch_setSetting(handle, SoundTouchSettings.UseQuickSeek, useQuickSeek ? 1 : 0);
        }

        public int GetUseQuickSeek()
        {
            return SoundTouchInterop32.soundtouch_getSetting(handle, SoundTouchSettings.UseQuickSeek);
        }
    }
}