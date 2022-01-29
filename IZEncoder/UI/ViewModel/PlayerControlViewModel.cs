namespace IZEncoder.UI.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using MaterialDesignThemes.Wpf;
    using View;

    public class PlayerControlViewModel : IZEScreen<PlayerControlView>
    {
        private readonly Dictionary<Button, int> _buttons;

        public PlayerControlViewModel(PlayerViewModel player)
        {
            Player = player;
            _buttons = new Dictionary<Button, int>();
            player.PropertyChanged += Player_PropertyChanged;
        }

        public PlayerViewModel Player { get; }
        public bool ShowVolumeSliderPopup { get; set; }
        public bool ShowVolumeButton { get; set; } = true;

        public PackIconKind VolumeButtonKind
        {
            get
            {
                if (!(Player.Volume > -1))
                    return PackIconKind.VolumeMute;

                if (Player.Volume > 75)
                    return PackIconKind.VolumeHigh;
                if (Player.Volume > 50)
                    return PackIconKind.VolumeMedium;
                if (Player.Volume > 0)
                    return PackIconKind.VolumeLow;

                return PackIconKind.VolumeOff;
            }
        }

        public bool VolumeButtonEnabled => VolumeButtonKind != PackIconKind.VolumeMute;

        public string TimeDisplayText
        {
            get
            {
                TimeSpan current = TimeSpan.Zero,
                    length = TimeSpan.Zero;

                if (Player?.Clip != null)
                {
                    current = Player.CurrentTime;
                    length = TimeSpan.FromSeconds(Player.Clip.Info.Frames / Player.Clip.Info.FrameRate());
                }

                return length.TotalSeconds >= 3600
                    ? $"{current:hh\\:mm\\:ss\\.fff} / {length:hh\\:mm\\:ss\\.fff}"
                    : $"{current:mm\\:ss\\.fff} / {length:mm\\:ss\\.fff}";
            }
        }

        private void Player_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ShowVolumeButton)
                if (!(e.PropertyName == nameof(VolumeButtonKind)
                      || e.PropertyName == nameof(VolumeButtonEnabled)))
                {
                    NotifyOfPropertyChange(() => VolumeButtonKind);
                    NotifyOfPropertyChange(() => VolumeButtonEnabled);
                }

            if (e.PropertyName == nameof(Player.CurrentFrame))
                NotifyOfPropertyChange(() => TimeDisplayText);
        }

        public void AddButton(PackIconKind kind, int index = 0, Action<Button, PackIcon> extend = null)
        {
            var btn = new Button
            {
                Content = new PackIcon {Kind = kind},
                Style = Application.Current.FindResource("MaterialDesignFlatButton") as Style
            };

            _buttons.Add(btn, index);
            extend?.Invoke(btn, btn.Content as PackIcon);
            View?.ButtonStack?.Children.Insert(index, btn);
        }

        public void SeekBarLoaded(PlayerControlTimeSlider slider)
        {
            slider.PlayerControlViewModel = this;
            if (!(slider.Template.FindName("PART_Track", slider) is Track track))
                return;

            var thumb = track.Thumb;
            thumb.DragDelta += (sender, args) =>
            {
                if (!Player.IsSeeking)
                    Player.BeginSeek();
            };

            thumb.DragCompleted += (sender, args) =>
            {
                if (Player.IsSeeking)
                    Player.EndSeek();
            };
        }

        public void OpenVolumeSlider()
        {
            ShowVolumeSliderPopup = !ShowVolumeSliderPopup;
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            foreach (var button in _buttons)
            {
                if (View.ButtonStack.Children.Contains(button.Key))
                    continue;

                View.ButtonStack.Children.Insert(button.Value, button.Key);
            }
        }
    }
}