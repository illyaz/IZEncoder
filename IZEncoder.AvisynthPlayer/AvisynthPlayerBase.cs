

namespace IZEncoder.AvisynthPlayer
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using IZEncoderNative.Avisynth;
    using CSCore;
    using CSCore.CoreAudioAPI;
    using CSCore.SoundOut;
    using Sanford.Collections.Generic;
    using SourceTouch;

    public abstract class AvisynthPlayerBase<TFrameBuffer>
        : IDisposable, INotifyPropertyChanged where TFrameBuffer : IFrameBuffer
    {
        private const int TrueValue = 1;
        private const int FalseValue = 0;

        /* no AV sync correction is done if below the AV sync threshold */
        // ReSharper disable once InconsistentNaming
        private const double AV_SYNC_THRESHOLD = 0.2;
        private readonly Deque<TFrameBuffer> _bufferStack;
        private readonly Stopwatch _clock;
        private readonly Deque<TFrameBuffer> _highBufferStack;
        private readonly object _lock = new object();
        private readonly string _prefetchKey = "Prefetch_" + Guid.NewGuid();
        private readonly Stopwatch _prefetchTimer = Stopwatch.StartNew();
        protected readonly AvisynthBridge Env;
        private AvisynthWaveSource _avsSoundSample;

        private Thread _bufferThread;
        private AvisynthClip _clip;

        private int _exitRequested,
            _exitBufferRequested,
            _exitRenderRequested,
            _seeking,
            _resetBufferRequested,
            _invalidateRequested;

        private TFrameBuffer _lastBuffer, _lastRender;

        private AvisynthPlayerState _oldStateSeek;
        private bool _prefetchEnabledReq;

        private int _prefetchVersion;
        private Thread _renderThread;
        private Exception _soundError;
        private WasapiOut _soundOut;
        private SoundTouchSample _soundSample;

        private bool _sourceHasPrefetched;
        private AvisynthPlayerSyncType _syncType = AvisynthPlayerSyncType.Video;
        private AvisynthClip _userClip;

        public AvisynthPlayerBase(AvisynthBridge env)
        {
            Env = env;
            _clock = new Stopwatch();
            _bufferStack = Deque<TFrameBuffer>.Synchronized(new Deque<TFrameBuffer>());
            _highBufferStack = Deque<TFrameBuffer>.Synchronized(new Deque<TFrameBuffer>());
        }

        protected AvisynthClip Clip
        {
            get => _clip;
            set
            {
                if (value != _clip)
                {
                    _clip = value;
                    if (IsSoundAvailable())
                        _avsSoundSample.ReplaceClip(value);
                }
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public int BufferSize { get; set; } = 5;
        public int BufferCount => _bufferStack.Count;

        // ReSharper disable once MemberCanBePrivate.Global
        public AvisynthPlayerState State { get; private set; } = AvisynthPlayerState.Init;
        public PrefetchState PrefetchState { get; private set; } = PrefetchState.Init;

        public bool SourceHasPrefetched
        {
            get => _sourceHasPrefetched;
            set
            {
                _sourceHasPrefetched = value;
                if (_sourceHasPrefetched)
                    PrefetchState = PrefetchState.AlreadyPrefetched;
            }
        }

        public bool PrefetchEnabled { get; set; } = true;
        public bool PrefetchDisabledOnSeek { get; set; } = true;
        public bool PrefetchDisabledOnSetClip { get; set; } = true;
        public double PrefetchDisabledDelay { get; set; } = 3000; // 3 Sec

        public AvisynthPlayerSyncType SyncType
        {
            get => _syncType;
            set
            {
                // TODO
                if (value == _syncType)
                    return;

                _syncType = value;
            }
        }

        // ReSharper disable once InconsistentNaming
        private TimeSpan _clockTime => _clock.Elapsed;

        public bool IsSeeking => _seeking == TrueValue;

        // ReSharper disable once InconsistentNaming
        public double Volume { get; set; }
        public int CurrentFrame { get; private set; }

        public virtual void Dispose()
        {
            Interlocked.Exchange(ref _exitRequested, TrueValue);
            Stop();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public AvisynthClip GetClip()
        {
            return Clip;
        }

        public void BeginSeek()
        {
            _oldStateSeek = State;
            if (State == AvisynthPlayerState.Playing)
                InternalPause();

            if (PrefetchEnabled && !SourceHasPrefetched && PrefetchDisabledOnSeek)
            {
                _prefetchTimer.Reset();
                _prefetchEnabledReq = true;
                PrefetchState = PrefetchState.Waiting;
                InternalReplaceClip(false);
            }

            Interlocked.Exchange(ref _seeking, TrueValue);
        }

        public void Seek(int to)
        {
            if (_seeking == FalseValue)
                return;

            //if (_lastRender != null)
            //{
            //    if (!_lastRender.IsReleased)
            //    {
            //        DeleteBuffer(_lastRender);
            //        _lastRender = default(TFrameBuffer);
            //    }
            //}
            CurrentFrame = to;
        }

        public void EndSeek()
        {
            if (PrefetchEnabled && !SourceHasPrefetched && PrefetchDisabledOnSeek)
                _prefetchTimer.Restart();

            while (_bufferStack.TryPopBack(out var buf))
                DeleteBuffer(buf);

            if (_lastBuffer != null)
                _lastBuffer.Index = CurrentFrame - 1;

            if (_oldStateSeek == AvisynthPlayerState.Playing)
                InternalPlay();

            Interlocked.Exchange(ref _seeking, FalseValue);
        }

        public void EnablePrefetch()
        {
            if (PrefetchEnabled)
                return;
        }

        public void DisablePrefetch()
        {
            if (!PrefetchEnabled)
                return;

            _prefetchTimer.Reset();
            _prefetchEnabledReq = false;
            PrefetchState = PrefetchState.Waiting;
            InternalReplaceClip(false);
        }

        public void ClearBufferStack()
        {
            while (_bufferStack.TryPopFront(out var buf))
                DeleteBuffer(buf);
        }

        public void Start()
        {
            State = AvisynthPlayerState.Paused;
            InternalStart();
        }

        private void InternalStart()
        {
            _statFrameTimes = new CircularBuffer<double>((int) Clip.Info.FrameRate());
            _statDrawTimes = new CircularBuffer<double>((int) Clip.Info.FrameRate());
            _statAvisynthTimes = new CircularBuffer<double>((int) Clip.Info.FrameRate());
            _statColorConvertTimes = new CircularBuffer<double>((int) Clip.Info.FrameRate());

            StartBufferLoop();
            StartRenderLoop();

            if (Clip.Info.HasAudio())
                try
                {
                    _soundOut = new WasapiOut(true, AudioClientShareMode.Shared, 100);
                    _soundSample = new SoundTouchSample(
                        (_avsSoundSample = new AvisynthWaveSource(Clip)).ToSampleSource(),
                        _soundOut.Latency,
                        new SoundTouchProfile(true, true));
                    _soundSample.Volume = (float) Volume;
                    _soundOut.Initialize(_soundSample.ToWaveSource());
                }
                catch (Exception e)
                {
                    _soundError = e;

                    // Dispose SoundOut first to avoid null exception
                    DisposeHelper.DisposeAndNull(ref _soundOut);
                    DisposeHelper.DisposeAndNull(ref _avsSoundSample);
                    DisposeHelper.DisposeAndNull(ref _soundSample);
                }
        }

        public void Stop()
        {
            if (State == AvisynthPlayerState.Stopped || State == AvisynthPlayerState.Init)
                return;

            State = AvisynthPlayerState.Stopped;

            InternalStop();
        }

        private void InternalStop()
        {
            // Stop renderloop first to avoid deadlock
            StopRenderLoop();
            StopBufferLoop();

            // Clear frame cache
            while (_bufferStack.TryPopBack(out var b))
                DeleteBuffer(b);

            while (_highBufferStack.TryPopBack(out var b))
                DeleteBuffer(b);

            if (!_lastBuffer.IsReleased)
                DeleteBuffer(_lastBuffer);

            if (!_lastRender.IsReleased)
                DeleteBuffer(_lastRender);

            // Dispose SoundOut first to avoid null exception
            DisposeHelper.DisposeAndNull(ref _soundOut);
            DisposeHelper.DisposeAndNull(ref _avsSoundSample);
            DisposeHelper.DisposeAndNull(ref _soundSample);
        }

        public void Play()
        {
            if (State == AvisynthPlayerState.Playing)
                return;

            if (CurrentFrame >= Clip.Info.Frames)
            {
                BeginSeek();
                Seek(0);
                EndSeek();
            }

            InternalPlay();
            State = AvisynthPlayerState.Playing;
        }

        private void InternalPlay()
        {
            _clock.Start();
            if (IsSoundAvailable())
                _soundOut.Play();
        }

        public void Pause()
        {
            if (State == AvisynthPlayerState.Paused)
                return;

            InternalPause();
            State = AvisynthPlayerState.Paused;
        }

        private void InternalPause()
        {
            _clock.Stop();
            if (IsSoundAvailable())
                _soundOut.Pause();
        }

        private void BufferLoop()
        {
            while (_exitRequested == FalseValue
                   && _exitBufferRequested == FalseValue)
            {
                if (!Clip.Info.HasVideo())
                    goto end;

                if (_highBufferStack.TryPopFront(out var highBuffer))
                {
                    InternalFillBuffer(highBuffer);
                    goto end;
                }

                if (PrefetchEnabled && !SourceHasPrefetched && _prefetchEnabledReq &&
                    _prefetchTimer.Elapsed.TotalMilliseconds > PrefetchDisabledDelay)
                {
                    _prefetchTimer.Stop();
                    _prefetchEnabledReq = false;
                    PrefetchState = PrefetchState.Prefetched;
                    InternalReplaceClip();
                }
                else if (!PrefetchEnabled)
                {
                    if (PrefetchState != PrefetchState.Disabled && !SourceHasPrefetched)
                        InternalReplaceClip(false);
                    PrefetchState = PrefetchState.Disabled;
                }
                else if (PrefetchEnabled)
                {
                    if (PrefetchState == PrefetchState.Disabled && !SourceHasPrefetched)
                        InternalReplaceClip();

                    if (PrefetchState != PrefetchState.Waiting)
                        PrefetchState = SourceHasPrefetched
                            ? PrefetchState.AlreadyPrefetched
                            : PrefetchState.Prefetched;
                }


                if (IsFull(_bufferStack, BufferSize))
                    goto end;

                if (IsSeeking)
                    goto end;

                var frame = CurrentFrame;
                var index = _lastBuffer?.Index ?? frame;


                if (_resetBufferRequested == TrueValue)
                {
                    while (_bufferStack.TryPopBack(out var tmpBuf))
                        DeleteBuffer(tmpBuf);

                    index = frame;
                    Interlocked.Exchange(ref _resetBufferRequested, FalseValue);
                }

                if (_resetBufferRequested == FalseValue)
                {
                    if (SyncType == AvisynthPlayerSyncType.Video)
                    {
                        index = index + 1;
                    }
                    else
                    {
                        if (frame > index + 1)
                            index = frame;
                        else
                            index = index + 1;
                    }

                    if (index == (_lastBuffer?.Index ?? -1) || index > Clip.Info.Frames)
                        goto end;
                }
                else
                {
                    Interlocked.Exchange(ref _resetBufferRequested, FalseValue);
                    index = frame;
                }

                var buf = _lastBuffer = CreateBuffer(index);
                _bufferStack.PushBack(buf);
                InternalFillBuffer(buf);
                end:
                Thread.Sleep(1);
            }
        }

        private TFrameBuffer GetHighPriorityBuffer(int index)
        {
            var buf = CreateBuffer(index);
            buf.IsHighPriority = true;
            _highBufferStack.PushBack(buf);
            return buf;
        }

        private void StartBufferLoop()
        {
            _bufferThread = new Thread(BufferLoop)
            {
                Name = "AvisynthPlayerBase::BufferLoop",
                Priority = ThreadPriority.Normal
            };
            _bufferThread.Start();
        }

        private void StopBufferLoop()
        {
            if (_bufferThread == null || !_bufferThread.IsAlive)
                return;

            Interlocked.Exchange(ref _exitBufferRequested, TrueValue);

            while (_bufferThread.IsAlive)
                Thread.Sleep(1);

            Interlocked.Exchange(ref _exitBufferRequested, FalseValue);
        }

        private TimeSpan TSSubtract(TimeSpan left, TimeSpan right)
        {
            return TimeSpan.FromTicks(left.Ticks - right.Ticks);
        }

        private void RenderLoop()
        {
            ;
            var lastRenderTime = TimeSpan.Zero;
            var renderLoopClock = Stopwatch.StartNew();
            var renderLoopLastFrame = -1;

            while (_exitRequested == FalseValue
                   && _exitRenderRequested == FalseValue)
            {
                if (!Clip.Info.HasVideo())
                    goto end;

                var frame = CurrentFrame;
                var lastIndex = _lastRender?.Index ?? -1;
                var renderLoopFrame = (int) Math.Floor(renderLoopClock.Elapsed.TotalSeconds * Clip.Info.FrameRate());
                if (State == AvisynthPlayerState.Playing && renderLoopLastFrame != renderLoopFrame ||
                    frame != lastIndex || _invalidateRequested == TrueValue)
                {
                    renderLoopLastFrame = renderLoopFrame;
                    TFrameBuffer buf;
                    if (_invalidateRequested == TrueValue && lastIndex == -1)
                        Interlocked.Exchange(ref _invalidateRequested, FalseValue); // Invalidate ??

                    if (_invalidateRequested == TrueValue)
                    {
                        buf = GetHighPriorityBuffer(lastIndex);
                    }
                    else if (_seeking == TrueValue)
                    {
                        if (frame == lastIndex)
                            goto end;
                        buf = GetHighPriorityBuffer(frame);
                    }
                    else if (!HasData(_bufferStack)
                             || !_bufferStack.TryPopFront(out buf))
                    {
                        goto end;
                    }

                    if (_invalidateRequested == FalseValue && buf.Index == lastIndex)
                    {
                        WaitFilled(buf);
                        DeleteBuffer(buf);
                        goto end;
                    }

                    if (_lastRender != null
                        && !_lastRender.IsReleased)
                        DeleteBuffer(_lastRender);

                    WaitFilled(buf);
                    InternalRender(buf);

                    buf.IsRendered = true;
                    _lastRender = buf;

                    if (_invalidateRequested == TrueValue)
                        Interlocked.Exchange(ref _invalidateRequested, FalseValue);

                    if (SyncType == AvisynthPlayerSyncType.Video)
                        CurrentFrame = buf.Index;

                    SyncAudio(TimeSpan.FromTicks((long) (buf.Index / Clip.Info.FrameRate() * TimeSpan.TicksPerSecond)));

                    if (State == AvisynthPlayerState.Playing && !IsSeeking && IsSoundAvailable() &&
                        _soundOut.PlaybackState != PlaybackState.Playing)
                        _soundOut.Play();

                    if (State == AvisynthPlayerState.Playing && buf.Index >= Clip.Info.Frames)
                        Pause();
                }
                else
                {
                    goto end;
                }

                if (State == AvisynthPlayerState.Playing && !IsSeeking)
                {
                    if (lastRenderTime != TimeSpan.Zero)
                        _statFrameTimes.PushFront(TSSubtract(_clockTime, lastRenderTime).TotalSeconds);
                    lastRenderTime = _clockTime;
                }

                end:
                Thread.Sleep(1);
            }
        }

        private void StartRenderLoop()
        {
            _renderThread = new Thread(RenderLoop)
            {
                Name = "AvisynthPlayerBase::RenderLoop",
                Priority = ThreadPriority.Normal
            };
            _renderThread.Start();
        }

        private void StopRenderLoop()
        {
            if (_renderThread == null || !_renderThread.IsAlive)
                return;

            Interlocked.Exchange(ref _exitRenderRequested, TrueValue);

            while (_renderThread.IsAlive)
                Thread.Sleep(1);

            Interlocked.Exchange(ref _exitRenderRequested, FalseValue);
        }

        private void SyncAudio(TimeSpan? to = null, bool force = false)
        {
            if (!IsSoundAvailable())
                return;

            var rate = (float) (1 / StatFrameTimes.Average() / Clip.Info.FrameRate());
            if (float.IsInfinity(rate) || float.IsNaN(rate))
                _soundSample.PlaybackRate = 1;
            else
                _soundSample.PlaybackRate = Math.Min(1, Math.Abs(rate - 1) > 0.02 ? rate : 1);
            var time = to ?? _clockTime;
            var diff = TSSubtract(_soundSample.GetPosition(), time).TotalSeconds;
            if (!force && !(Math.Abs(diff) > AV_SYNC_THRESHOLD))
                return;

            _soundSample.SetPosition(time);
            _soundSample.Reposition();
        }

        private bool IsFull<T>(Deque<T> stack, int maxSize)
        {
            return stack.Count >= maxSize;
        }

        private bool HasData<T>(Deque<T> stack)
        {
            return stack.Count > 0;
        }

        private void WaitFilled(TFrameBuffer buf)
        {
            while (!buf.IsFilled)
                Thread.Sleep(1);
        }

        public bool IsSoundAvailable()
        {
            return Clip != null && Clip.Info.HasAudio() && _soundError == null && _soundSample != null &&
                   _soundOut != null;
        }

        protected bool IsSameClipProperty(AvisynthClip x, AvisynthClip y)
        {
            if (x == null || y == null)
                return false;

            if (x.Info.Width != y.Info.Width)
                return false;

            if (x.Info.Height != y.Info.Height)
                return false;

            if (x.Info.PixelType != y.Info.PixelType)
                return false;

            if (x.Info.SampleRate != y.Info.SampleRate)
                return false;

            if (x.Info.Channels != y.Info.Channels)
                return false;

            return x.Info.SampleType == y.Info.SampleType;
        }

        public virtual void SetClip(AvisynthClip clip)
        {
            if (!SourceHasPrefetched && PrefetchDisabledOnSetClip)
            {
                PrefetchState = PrefetchState.Waiting;
                _prefetchTimer.Restart();
                _prefetchEnabledReq = true;

                InternalSetClip(clip, false);
            }
            else
            {
                InternalSetClip(clip, !SourceHasPrefetched);
            }
        }

        private void InternalSetClip(AvisynthClip clip, bool prefetch)
        {
            var s = State;
            var lastFrame = CurrentFrame;
            var lastFps = Clip?.Info.FrameRate() ?? 0;
            var sameProperty = IsSameClipProperty(clip, Clip);
            var reqStart = State == AvisynthPlayerState.Stopped || State == AvisynthPlayerState.Init;
            if (!sameProperty && !(State == AvisynthPlayerState.Stopped || State == AvisynthPlayerState.Init))
            {
                reqStart = true;
                InternalStop();
            }

            lock (_lock)
            {
                _userClip = clip;
                Clip = prefetch
                    ? Env.CreateClip($"{_prefetchKey}{++_prefetchVersion}", "Prefetch", clip,
                        Environment.ProcessorCount)
                    : clip;
            }

            Interlocked.Exchange(ref _resetBufferRequested, TrueValue);
            if (s == AvisynthPlayerState.Paused)
                Interlocked.Exchange(ref _invalidateRequested, TrueValue);

            if (reqStart)
            {
                InternalStart();
                if (IsSoundAvailable() && State == AvisynthPlayerState.Playing)
                    _soundOut.Play();
            }

            //if (s == AvisynthPlayerState.Playing)
            //    InternalPlay();
        }

        private void InternalReplaceClip(bool prefetch = true)
        {
            Env.Remove($"{_prefetchKey}{_prefetchVersion}");

            Clip = prefetch
                ? Env.CreateClip($"{_prefetchKey}{++_prefetchVersion}", "Prefetch", _userClip,
                    Environment.ProcessorCount)
                : _userClip;
        }

        protected int LimitToRange(int value, int inclusiveMinimum, int inclusiveMaximum)
        {
            if (value < inclusiveMinimum) return inclusiveMinimum;
            if (value > inclusiveMaximum) return inclusiveMaximum;
            return value;
        }

        private void InternalRender(TFrameBuffer buffer)
        {
            var c = _clockTime;
            //Thread.Sleep(3);
            Render(buffer);
            _statDrawTimes.PushBack((_clockTime - c).TotalSeconds);
        }

        private void InternalFillBuffer(TFrameBuffer buffer)
        {
            var c = _clockTime;
            lock (_lock)
            {
                using (var f = Clip.GetFrame(buffer.Index))
                {
                    _statAvisynthTimes.PushBack((_clockTime - c).TotalSeconds);

                    c = _clockTime;
                    FillBuffer(buffer, f);
                    _statColorConvertTimes.PushBack((_clockTime - c).TotalSeconds);
                }
            }
        }

        protected abstract TFrameBuffer CreateBuffer(int frame);
        protected abstract void FillBuffer(TFrameBuffer buffer, AvisynthVideoFrame f);
        protected abstract void DeleteBuffer(TFrameBuffer buffer);
        protected abstract void Render(TFrameBuffer buffer);

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName != null && propertyName.Equals(nameof(Volume)))
                if (IsSoundAvailable())
                    _soundSample.Volume = (float) Volume;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Stat

        private CircularBuffer<double> _statFrameTimes;
        public IEnumerable<double> StatFrameTimes => _statFrameTimes;

        private CircularBuffer<double> _statDrawTimes;
        public IEnumerable<double> StatDrawTimes => _statDrawTimes;

        private CircularBuffer<double> _statAvisynthTimes;
        public IEnumerable<double> StatAvisynthTimes => _statAvisynthTimes;

        private CircularBuffer<double> _statColorConvertTimes;
        public IEnumerable<double> StatColorConvertTimes => _statColorConvertTimes;

        #endregion
    }
}