namespace IZEncoder.UI.ViewModel
{
    using System.Windows.Controls.Primitives;
    using View;

    public class SubtitleSettingsViewModel : IZEPopup<SubtitleSettingsView>
    {
        public SubtitleSettingsViewModel(Global g)
        {
            G = g;
        }

        public Global G { get; }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            Popup.StaysOpen = false;
            Popup.PopupAnimation = PopupAnimation.Fade;
        }
    }
}