namespace IZEncoder.AvisynthPlayer.WPFDX
{
    using SharpDX.Direct2D1;

    public class AvisynthPlayerWPFDXFrameBuffer : IFrameBuffer
    {
        public override int Index { get; set; }
        public override bool IsReleased { get; set; }
        public override bool IsRendered { get; set; }
        public override bool IsFilled { get; set; }
        public Bitmap1 Data { get; set; }
        public Bitmap1 Cb { get; set; }
        public Bitmap1 Cr { get; set; }
        public Effect YCbCrEffect { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int BPP { get; set; }
        public int Pitch { get; set; }
        public bool IsErrored { get; set; }
        public string ErrorText { get; set; }
    }
}