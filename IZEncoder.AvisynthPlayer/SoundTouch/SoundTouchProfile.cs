namespace IZEncoder.AvisynthPlayer.SourceTouch
{
    public class SoundTouchProfile
    {
        public SoundTouchProfile(bool useTempo, bool useAntiAliasing)
        {
            UseTempo = useTempo;
            UseAntiAliasing = useAntiAliasing;
        }

        public bool UseTempo { get; set; }
        public bool UseAntiAliasing { get; set; }
        public bool UseQuickSeek { get; set; }
    }
}