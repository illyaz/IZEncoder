namespace IZEncoder.UI.View
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using ViewModel;

    public class PlayerControlTimeSlider : Slider
    {
        private ToolTip _autoToolTip;
        public PlayerControlViewModel PlayerControlViewModel { get; set; }

        private ToolTip AutoToolTip
        {
            get
            {
                if (_autoToolTip != null) return _autoToolTip;
                var field = typeof(Slider).GetField(
                    "_autoToolTip",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                _autoToolTip = field.GetValue(this) as ToolTip;

                return _autoToolTip;
            }
        }

        protected override void OnThumbDragStarted(DragStartedEventArgs e)
        {
            base.OnThumbDragStarted(e);
            FormatAutoToolTipContent();
        }

        protected override void OnThumbDragDelta(DragDeltaEventArgs e)
        {
            base.OnThumbDragDelta(e);
            FormatAutoToolTipContent();
        }

        private void FormatAutoToolTipContent()
        {
            if (PlayerControlViewModel?.Player?.Clip?.Info == null)
            {
                AutoToolTip.Content = Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                var t = TimeSpan
                    .FromSeconds(PlayerControlViewModel.Player.CurrentFrame /
                                 PlayerControlViewModel.Player.Clip.Info.FrameRate());
                AutoToolTip.Content = t.ToString((t.TotalHours > 1.0 ? "hh\\:" : "") + "mm\\:ss\\.fff");
            }
        }
    }
}