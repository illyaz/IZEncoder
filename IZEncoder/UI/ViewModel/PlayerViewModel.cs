namespace IZEncoder.UI.ViewModel
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Windows;
    using System.Windows.Interop;
    using IZEncoderNative.Avisynth;
    using AvisynthPlayer;
    using AvisynthPlayer.WPFDX;
    using View;

    public class PlayerViewModel : IZEScreen<PlayerView>, IDisposable
    {
        public enum OpenModes
        {
            DirectShowSource,
            FFMS2,
            Import,
            Eval
        }

        private readonly bool _disposeEnv;
        private readonly Guid _guid;
        private readonly AvisynthPlayerWPFDX _player;
        private int _openCount;

        public PlayerViewModel(AvisynthBridge env, Window host)
        {
            Bridge = env;
            _player = new AvisynthPlayerWPFDX(env, host, D3DImage = host.Dispatcher.Invoke(() => new D3DImage()));
            _guid = Guid.NewGuid();
            _openCount = -1;
            _player.PropertyChanged += _player_PropertyChanged;
            Volume = 100;
        }

        public PlayerViewModel(Window host)
            : this(new AvisynthBridge(), host)
        {
            _disposeEnv = true;
        }

        public PlayerViewModel()
            : this(new AvisynthBridge(), Application.Current.MainWindow)
        {
            _disposeEnv = true;
        }

        public D3DImage D3DImage { get; }
        public AvisynthClip Clip { get; private set; }
        public bool IsPlaying { get; private set; }

        // Non PropertyChange
        public bool IsSeeking => _player.IsSeeking;

        public bool Prefetch
        {
            get => _player.PrefetchEnabled;
            set => _player.PrefetchEnabled = value;
        }

        public TimeSpan PrefetchSeekDelay
        {
            get => TimeSpan.FromMilliseconds(_player.PrefetchDisabledDelay);
            set => _player.PrefetchDisabledDelay = value.TotalMilliseconds;
        }

        public int CurrentFrame
        {
            get => _player?.CurrentFrame ?? 0;
            set
            {
                if (_player == null || Clip == null)
                    return;

                if (_player.IsSeeking)
                    _player.Seek(value);
            }
        }

        public TimeSpan CurrentTime
        {
            get
            {
                if (_player == null || Clip == null)
                    return TimeSpan.Zero;

                return TimeSpan.FromSeconds(_player.CurrentFrame / Clip.Info.FrameRate());
            }
            set
            {
                if (_player == null || Clip == null)
                    return;

                if (_player.IsSeeking)
                    _player.Seek((int) Math.Floor(value.TotalSeconds * Clip.Info.FrameRate()));
            }
        }

        public double Volume
        {
            get
            {
                if (_player == null || !_player.IsSoundAvailable())
                    return -1;

                return _player.Volume * 100d;
            }
            set
            {
                if (_player == null)
                    return;

                _player.Volume = value / 100d;
            }
        }

        public AvisynthBridge Bridge { get; }

        public void Dispose()
        {
            _player?.Dispose();

            if (_disposeEnv)
                Bridge?.Dispose();
        }

        public event EventHandler OnReady;

        private void _player_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(_player.CurrentFrame)))
            {
                NotifyOfPropertyChange(() => CurrentFrame);
                NotifyOfPropertyChange(() => CurrentTime);
            }
            else if (e.PropertyName.Equals(nameof(_player.State)))
            {
                IsPlaying = _player.State == AvisynthPlayerState.Playing;
            }
        }

        public void OnD3DImageChanged()
        {
            if (View == null)
                return;

            View.NormalImage.Source = D3DImage;
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            OnD3DImageChanged();
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            OnReady?.Invoke(this, EventArgs.Empty);
        }

        public void Open(string data, int? audioTrack = null, OpenModes mode = OpenModes.DirectShowSource)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException(@"Value cannot be null or empty.", nameof(data));

            var key = $"{_guid}{_openCount + 1}";

            switch (mode)
            {
                case OpenModes.DirectShowSource:
                    SetClip(Bridge.CreateClip(key, "DirectShowSource", data));
                    break;
                case OpenModes.FFMS2:
                    SetClip(Bridge.CreateClip(key, "FFMS2", new object[] {data, audioTrack ?? -2},
                        new[] {null, "atrack"}));
                    break;
                case OpenModes.Import:
                    if (!Path.GetExtension(data).Equals(".avs", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Import accept only .avs");
                    SetClip(Bridge.CreateClip(key, "Import", data));
                    break;
                case OpenModes.Eval:
                    SetClip(Bridge.CreateClip(key, "Eval", data));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            // Success
            Bridge.Remove($"{_guid}{_openCount}");
            _openCount++;
        }

        public void SetClip(AvisynthClip clip)
        {
            Clip = clip;
            _player.SetClip(Clip);
        }

        public void Play()
        {
            _player.Play();
        }

        public void Pause()
        {
            _player.Pause();
        }

        public void TogglePlay()
        {
            if (_player.State == AvisynthPlayerState.Playing)
                _player.Pause();
            else
                _player.Play();
        }

        public void StepBackward()
        {
            if (IsPlaying)
                Pause();

            BeginSeek();
            Seek(_player.CurrentFrame - 1);
            EndSeek();
        }

        public void StepForward()
        {
            if (IsPlaying)
                Pause();

            BeginSeek();
            Seek(_player.CurrentFrame + 1);
            EndSeek();
        }

        public void BeginSeek()
        {
            _player.BeginSeek();
        }

        public void Seek(int to)
        {
            _player.Seek(to);
        }

        public void EndSeek()
        {
            _player.EndSeek();
        }
    }
}