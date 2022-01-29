using D2D1Bitmap1 = SharpDX.Direct2D1.Bitmap1;
using D2D1BitmapProperties1 = SharpDX.Direct2D1.BitmapProperties1;
using D2D1Device = SharpDX.Direct2D1.Device;
using D2D1DeviceContext = SharpDX.Direct2D1.DeviceContext;
using D2D1Factory1 = SharpDX.Direct2D1.Factory1;
using D2D1PixelFormat = SharpDX.Direct2D1.PixelFormat;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11RenderTargetView = SharpDX.Direct3D11.RenderTargetView;
using D3D11Texture2D = SharpDX.Direct3D11.Texture2D;
using DWriteFactory = SharpDX.DirectWrite.Factory;
using DWriteTextFormat = SharpDX.DirectWrite.TextFormat;
using DXGIDevice = SharpDX.DXGI.Device;

namespace IZEncoder.AvisynthPlayer.WPFDX
{
    using System;
    using System.Windows;
    using System.Windows.Interop;
    using IZEncoderNative.Avisynth;
    using SharpDX;
    using SharpDX.Direct2D1;
    using SharpDX.Direct3D;
    using SharpDX.Direct3D11;
    using SharpDX.Direct3D9;
    using SharpDX.DirectWrite;
    using SharpDX.DXGI;
    using SharpDX.Mathematics.Interop;
    using AlphaMode = SharpDX.Direct2D1.AlphaMode;
    using FactoryType = SharpDX.Direct2D1.FactoryType;
    using FeatureLevel = SharpDX.Direct3D.FeatureLevel;
    using Format = SharpDX.DXGI.Format;
    using InterpolationMode = SharpDX.Direct2D1.InterpolationMode;
    using PresentParameters = SharpDX.Direct3D9.PresentParameters;
    using Resource = SharpDX.DXGI.Resource;
    using Surface = SharpDX.DXGI.Surface;
    using SwapEffect = SharpDX.Direct3D9.SwapEffect;
    using TextAlignment = SharpDX.DirectWrite.TextAlignment;
    using Usage = SharpDX.Direct3D9.Usage;

    public class AvisynthPlayerWPFDX : AvisynthPlayerBase<AvisynthPlayerWPFDXFrameBuffer>
    {
        private static D2D1Factory1 _d2D1Factory;
        private readonly D3DImage _interopImage;
        private readonly string _rgbClipKey = Guid.NewGuid().ToString();
        private D2D1Bitmap1 _d2D1Bitmap;
        private D2D1Device _d2D1Device;
        private D2D1DeviceContext _d2D1DeviceContext;

        private D3D11Device _d3D11Device;
        private D3D11RenderTargetView _d3D11RenderTarget;
        private D3D11Texture2D _d3D11Texture;
        private Direct3DEx _d3Dex;
        private DeviceEx _deviceEx;
        private SolidColorBrush _dwriteBrush;

        private DWriteFactory _dwriteFactory;
        private DWriteTextFormat _dwriteTextFormat;
        private DXGIDevice _dxgiDevice;

        private int _rgbrefCount;

        public AvisynthPlayerWPFDX(AvisynthBridge env, Window host, D3DImage imageSource)
            : base(env)
        {
            _interopImage = imageSource
                            ?? throw new ArgumentNullException(nameof(imageSource));

            _d3Dex = new Direct3DEx();
            _deviceEx = new DeviceEx(_d3Dex, 0, DeviceType.Hardware, IntPtr.Zero,
                CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve,
                new PresentParameters
                {
                    Windowed = true,
                    SwapEffect = SwapEffect.Discard,
                    DeviceWindowHandle = host.Dispatcher.Invoke(() => new WindowInteropHelper(host).EnsureHandle()),
                    PresentationInterval = PresentInterval.Immediate
                });

            _d3D11Device = new D3D11Device(DriverType.Hardware,
                DeviceCreationFlags.BgraSupport, FeatureLevel.Level_12_1,
                FeatureLevel.Level_12_0, FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0, FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0, FeatureLevel.Level_9_3,
                FeatureLevel.Level_9_2, FeatureLevel.Level_9_1);

            _dxgiDevice = _d3D11Device.QueryInterface<DXGIDevice>();
            _d2D1Factory = new D2D1Factory1(FactoryType.MultiThreaded, DebugLevel.None);
            _d2D1Device = new D2D1Device(_d2D1Factory, _dxgiDevice);
            _d2D1DeviceContext =
                new D2D1DeviceContext(_d2D1Device, DeviceContextOptions.EnableMultithreadedOptimizations);
            _dwriteFactory = new DWriteFactory();
            _dwriteTextFormat = new DWriteTextFormat(_dwriteFactory, "Segoe UI", 30);
            _dwriteTextFormat.TextAlignment = TextAlignment.Center;
            _dwriteTextFormat.ParagraphAlignment = ParagraphAlignment.Center;
            _dwriteBrush = new SolidColorBrush(_d2D1DeviceContext, new RawColor4(1, 0, 0, 1));

            _d2D1Factory.RegisterEffect<YV12ConverterEffect>();
            _interopImage.Dispatcher.Invoke(() =>
                _interopImage.IsFrontBufferAvailableChanged += InteropImage_IsFrontBufferAvailableChanged);
        }

        public string LastError { get; set; }
        public bool HasError { get; set; }

        // ReSharper disable once InconsistentNaming
        public AvisynthPlayerWPFDXPixelConversionMethod YV12ConversionMethod { get; set; } =
            AvisynthPlayerWPFDXPixelConversionMethod.DirectX;

        public AvisynthPlayerWPFDXPixelConversionMethod CurrentPixelConversionnMethod { get; protected set; }
        public AvisynthVideoInfo OriginalInfo { get; protected set; }
        public bool IsCompareMode { protected get; set; }

        private void InteropImage_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool) e.NewValue)
                try
                {
                    _interopImage.Lock();
                    SetBackBuffer(_d3D11Texture, true);
                    _interopImage.AddDirtyRect(new Int32Rect(0, 0, _interopImage.PixelWidth,
                        _interopImage.PixelHeight));
                }
                finally
                {
                    _interopImage.Unlock();
                }
        }

        // ReSharper disable once ParameterHidesMember
        public override void SetClip(AvisynthClip clip)
        {
            var oldClip = _rgbClipKey + _rgbrefCount;
            var newClip = _rgbClipKey + ++_rgbrefCount;
            if (clip != null)
            {
                // ReSharper disable once InconsistentNaming
                var _vi = clip.GetVideoInfo();
                if (clip.Info.PixelType != ColorSpaces.CS_BGR32)
                {
                    if (((clip.Info.PixelType & ColorSpaces.CS_I420) == ColorSpaces.CS_I420 ||
                         (clip.Info.PixelType & ColorSpaces.CS_YV12) == ColorSpaces.CS_YV12) &&
                        YV12ConversionMethod == AvisynthPlayerWPFDXPixelConversionMethod.DirectX)
                    {
                        CurrentPixelConversionnMethod = AvisynthPlayerWPFDXPixelConversionMethod.DirectX;
                        base.SetClip(Env.CopyClip(newClip, clip));
                    }
                    else
                    {
                        if (!Env.FunctionExists("SWScale"))
                            throw new Exception("Require SWScale plugin for colorspace convertion");

                        CurrentPixelConversionnMethod = AvisynthPlayerWPFDXPixelConversionMethod.SWScale;
                        base.SetClip(Env.CreateClip(newClip, "SWScale", new object[] {clip, "RGB32"},
                            new[] {null, "colorspace"}));
                    }
                }
                else
                {
                    CurrentPixelConversionnMethod = AvisynthPlayerWPFDXPixelConversionMethod.BitmapFlip;
                    base.SetClip(Env.CopyClip(newClip, clip));
                }

                OriginalInfo = clip.GetVideoInfo();
            }

            Env.Remove(oldClip);
        }

        protected override AvisynthPlayerWPFDXFrameBuffer CreateBuffer(int index)
        {
            return new AvisynthPlayerWPFDXFrameBuffer
            {
                Index = index,
                Width = Clip.Info.Width,
                Height = Clip.Info.Height,
                BPP = 4
            };
        }

        protected override void FillBuffer(AvisynthPlayerWPFDXFrameBuffer buffer, AvisynthVideoFrame f)
        {
            try
            {
                //Thread.Sleep(55);
                if (CurrentPixelConversionnMethod == AvisynthPlayerWPFDXPixelConversionMethod.DirectX)
                {
                    buffer.YCbCrEffect = new Effect<YV12ConverterEffect>(_d2D1DeviceContext);

                    var properties = new D2D1BitmapProperties1(
                        new D2D1PixelFormat(Format.R8_UNorm, AlphaMode.Ignore), 96, 96,
                        BitmapOptions.None);

                    buffer.Data = new D2D1Bitmap1(_d2D1DeviceContext, new Size2(buffer.Width, buffer.Height),
                        new DataStream(f.GetReadPtr(YUVPlanes.PLANAR_Y),
                            f.GetPitch(YUVPlanes.PLANAR_Y) * f.GetHeight(YUVPlanes.PLANAR_Y), true, false),
                        f.GetPitch(YUVPlanes.PLANAR_Y), properties);
                    buffer.Cb = new D2D1Bitmap1(_d2D1DeviceContext, new Size2(buffer.Width / 2, buffer.Height / 2),
                        new DataStream(f.GetReadPtr(YUVPlanes.PLANAR_U),
                            f.GetPitch(YUVPlanes.PLANAR_U) * f.GetHeight(YUVPlanes.PLANAR_U), true, false),
                        f.GetPitch(YUVPlanes.PLANAR_U), properties);
                    buffer.Cr = new D2D1Bitmap1(_d2D1DeviceContext, new Size2(buffer.Width / 2, buffer.Height / 2),
                        new DataStream(f.GetReadPtr(YUVPlanes.PLANAR_V),
                            f.GetPitch(YUVPlanes.PLANAR_V) * f.GetHeight(YUVPlanes.PLANAR_V), true, false),
                        f.GetPitch(YUVPlanes.PLANAR_V), properties);

                    buffer.YCbCrEffect.SetInput(0, buffer.Data, true);
                    buffer.YCbCrEffect.SetInput(1, buffer.Cb, true);
                    buffer.YCbCrEffect.SetInput(2, buffer.Cr, true);
                }
                else
                {
                    buffer.Pitch = f.GetPitch();
                    var properties = new D2D1BitmapProperties1(
                        new D2D1PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied), 96, 96,
                        BitmapOptions.None);

                    buffer.Data = new D2D1Bitmap1(_d2D1DeviceContext, new Size2(buffer.Width, buffer.Height),
                        new DataStream(f.GetReadPtr(), buffer.Pitch * buffer.Height, true, false), buffer.Pitch,
                        properties);
                }
            }
            catch (Exception e)
            {
                buffer.IsErrored = true;
                buffer.ErrorText = e.Message;
            }
            finally
            {
                buffer.IsFilled = true;
            }
        }

        protected override void DeleteBuffer(AvisynthPlayerWPFDXFrameBuffer buffer)
        {
            if (buffer != null
                && !buffer.IsReleased
                && !buffer.IsErrored)
            {
                buffer.YCbCrEffect?.Dispose();
                buffer.Data?.Dispose();
                buffer.Cb?.Dispose();
                buffer.Cr?.Dispose();
                buffer.IsReleased = true;
            }
        }

        protected override void Render(AvisynthPlayerWPFDXFrameBuffer buffer)
        {
            var newbackbuffer = false;
            if (buffer.IsErrored)
            {
                LastError = $"Failed to request avisynth frame buffer ({buffer.Index})\n{buffer.ErrorText}";
                HasError = true;
            }
            else
            {
                try
                {
                    if (_d3D11Texture == null
                        || buffer.Width != _d3D11Texture.Description.Width
                        || buffer.Height != _d3D11Texture.Description.Height)
                    {
                        ResetRenderTarget(buffer.Width, buffer.Height);
                        newbackbuffer = true;
                    }

                    _d2D1DeviceContext.BeginDraw();
                    _d2D1DeviceContext.Clear(new RawColor4(0, 0, 0, buffer.IsErrored ? 1 : 0));


                    if (CurrentPixelConversionnMethod == AvisynthPlayerWPFDXPixelConversionMethod.DirectX ||
                        buffer.YCbCrEffect != null)
                    {
                        _d2D1DeviceContext.DrawImage(buffer.YCbCrEffect, InterpolationMode.Linear);
                    }
                    else
                    {
                        var m = Matrix.Scaling(1f, -1f, 1);
                        m.TranslationVector = new Vector3(0, buffer.Height, 1);
                        _d2D1DeviceContext.DrawBitmap(buffer.Data, 1, InterpolationMode.Linear,
                            m);
                    }

                    _d2D1DeviceContext.EndDraw();
                    _d3D11Device.ImmediateContext.Flush();

                    _interopImage.Dispatcher.Invoke(() =>
                    {
                        _interopImage.Lock();
                        if (newbackbuffer)
                            SetBackBuffer(_d3D11Texture, true);
                        _interopImage.AddDirtyRect(new Int32Rect(0, 0, _interopImage.PixelWidth,
                            _interopImage.PixelHeight));
                        _interopImage.Unlock();
                    });

                    LastError = string.Empty;
                    HasError = false;
                }
                catch (Exception e)
                {
                    LastError = $"Failed to render ({buffer.Index})\n{e.Message}";
                    HasError = true;
                }
            }
        }

        private void DrawCenteredRedString(string text, int width, int height)
        {
            var factor = (IsCompareMode ? width / 2 : width) / 1280.0f;
            using (var layout = new TextLayout(_dwriteFactory, text, _dwriteTextFormat,
                IsCompareMode ? width / 2 : width, height))
            {
                layout.SetFontSize(layout.FontSize * factor, new TextRange(0, text.Length));
                _d2D1DeviceContext.DrawTextLayout(new RawVector2(IsCompareMode ? width / 2.0f : 0, 0),
                    layout, _dwriteBrush);
            }
        }

        private void SetBackBuffer(D3D11Texture2D target, bool noLock = false)
        {
            if (!noLock)
                _interopImage.Lock();

            try
            {
                if (target == null)
                    _interopImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                else
                    using (var res = target.QueryInterface<Resource>())
                    {
                        var sharedHandle = res.SharedHandle;
                        using (var d3D9Texture = new Texture(_deviceEx, target.Description.Width,
                            target.Description.Height, 1, Usage.RenderTarget, SharpDX.Direct3D9.Format.A8R8G8B8,
                            Pool.Default, ref sharedHandle))
                        {
                            using (var surface = d3D9Texture.GetSurfaceLevel(0))
                            {
                                _interopImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                            }
                        }
                    }
            }
            finally
            {
                if (!noLock)
                    _interopImage.Unlock();
            }
        }

        private void ResetRenderTarget(int Width, int Height)
        {
            if (_d3D11Device == null)
                return;

            _d3D11Device.ImmediateContext.ClearState();

            DisposeHelper.DisposeAndNull(ref _d2D1Bitmap);
            DisposeHelper.DisposeAndNull(ref _d3D11RenderTarget);
            DisposeHelper.DisposeAndNull(ref _d3D11Texture);

            _d3D11Texture = new D3D11Texture2D(_d3D11Device, new Texture2DDescription
            {
                Width = Width,
                Height = Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                OptionFlags = ResourceOptionFlags.Shared
            });

            _d3D11RenderTarget = new D3D11RenderTargetView(_d3D11Device, _d3D11Texture);
            _d3D11Device.ImmediateContext.OutputMerger.SetTargets(_d3D11RenderTarget);

            // Specify the properties for the bitmap that we will use as the target of our Direct2D operations.
            // We want a 32-bit BGRA surface with premultiplied alpha.
            var properties = new D2D1BitmapProperties1(
                new D2D1PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied), 96, 96,
                BitmapOptions.Target | BitmapOptions.CannotDraw);

            using (var surface = _d3D11Texture.QueryInterface<Surface>())
            {
                _d2D1Bitmap = new D2D1Bitmap1(_d2D1DeviceContext, surface, properties);
            }

            _d2D1DeviceContext.Target = _d2D1Bitmap;
        }

        public override void Dispose()
        {
            base.Dispose();

            _interopImage.Dispatcher.Invoke(() =>
            {
                _interopImage.IsFrontBufferAvailableChanged -= InteropImage_IsFrontBufferAvailableChanged;
                SetBackBuffer(null);
            });

            DisposeHelper.DisposeAndNull(ref _d2D1Bitmap);
            DisposeHelper.DisposeAndNull(ref _d3D11RenderTarget);
            DisposeHelper.DisposeAndNull(ref _d3D11Texture);
            DisposeHelper.DisposeAndNull(ref _dwriteBrush);
            DisposeHelper.DisposeAndNull(ref _dwriteTextFormat);
            DisposeHelper.DisposeAndNull(ref _dwriteFactory);
            DisposeHelper.DisposeAndNull(ref _d2D1DeviceContext);
            DisposeHelper.DisposeAndNull(ref _d2D1Device);
            DisposeHelper.DisposeAndNull(ref _d2D1Factory);
            DisposeHelper.DisposeAndNull(ref _dxgiDevice);
            DisposeHelper.DisposeAndNull(ref _d3D11Device);
            DisposeHelper.DisposeAndNull(ref _deviceEx);
            DisposeHelper.DisposeAndNull(ref _d3Dex);


            Env.Remove(_rgbClipKey + _rgbrefCount);
        }
    }
}