namespace IZEncoder.AvisynthPlayer
{
    public abstract class IFrameBuffer
    {
        public abstract int Index { get; set; }
        public abstract bool IsReleased { get; set; }
        public abstract bool IsRendered { get; set; }
        public abstract bool IsFilled { get; set; }
        public bool IsRefresh { get; set; }
        public bool IsHighPriority { get; set; }
    }
}