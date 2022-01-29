namespace IZEncoder.UI.ViewModel.AvisynthParamUI
{
    public interface IAvisynthParamUIOverlayContext { }

    public interface IAvisynthParamUIOverlay
    {
        void AttachOverlay(IAvisynthParamUIOverlayContext context);
        void DetachOverlay(IAvisynthParamUIOverlayContext context);
    }
}