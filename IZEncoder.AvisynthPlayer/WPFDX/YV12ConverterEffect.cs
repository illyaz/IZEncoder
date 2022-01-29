namespace IZEncoder.AvisynthPlayer.WPFDX
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using SharpDX;
    using SharpDX.Direct2D1;
    using SharpDX.Direct3D9;
    using SharpDX.Mathematics.Interop;
    using Filter = SharpDX.Direct2D1.Filter;

    [CustomEffectInput("yTexture")]
    [CustomEffectInput("uTexture")]
    [CustomEffectInput("vTexture")]
    internal class YV12ConverterEffect : CustomEffectBase, DrawTransform
    {
        private static readonly Guid GUID_YV12ConverterPixelShader = Guid.NewGuid();

        private static readonly ShaderBytecode _yv12Ps;
        private static readonly byte[] _yv12PsBytes;

        static YV12ConverterEffect()
        {
            var asm = Assembly.GetExecutingAssembly();
            using (var s = asm.GetManifestResourceStream("IZEncoder.AvisynthPlayer.WPFDX.yv12-rgb-ps.hlsl"))
            {
                var buffer = new byte[s.Length];
                s.Read(buffer, 0, buffer.Length);

                var result = ShaderBytecode.Compile(buffer, "main", "ps_4_0_level_9_1", ShaderFlags.OptimizationLevel3);
                if (result.Bytecode == null)
                    Debugger.Break();

                _yv12Ps = result.Bytecode;
                _yv12PsBytes = new byte[_yv12Ps.BufferSize];
                Marshal.Copy(_yv12Ps.BufferPointer, _yv12PsBytes, 0, _yv12PsBytes.Length);
            }
        }

        public int InputCount => 3;

        public void MapOutputRectangleToInputRectangles(RawRectangle outputRect, RawRectangle[] inputRects)
        {
            inputRects[0].Left = outputRect.Left;
            inputRects[0].Top = outputRect.Top;
            inputRects[0].Right = outputRect.Right;
            inputRects[0].Bottom = outputRect.Bottom;

            inputRects[1].Left = outputRect.Left;
            inputRects[1].Top = outputRect.Top;
            inputRects[1].Right = outputRect.Right;
            inputRects[1].Bottom = outputRect.Bottom;

            inputRects[2].Left = outputRect.Left;
            inputRects[2].Top = outputRect.Top;
            inputRects[2].Right = outputRect.Right;
            inputRects[2].Bottom = outputRect.Bottom;
        }

        public RawRectangle MapInputRectanglesToOutputRectangle(RawRectangle[] inputRects,
            RawRectangle[] inputOpaqueSubRects,
            out RawRectangle outputOpaqueSubRect)
        {
            outputOpaqueSubRect = default(Rectangle);
            return inputRects[0];
        }

        public RawRectangle MapInvalidRect(int inputIndex, RawRectangle invalidInputRect)
        {
            return invalidInputRect;
        }

        public void SetDrawInformation(DrawInformation drawInfo)
        {
            drawInfo.SetPixelShader(GUID_YV12ConverterPixelShader, PixelOptions.None);
            drawInfo.SetInputDescription(0, new InputDescription(Filter.MinimumMagMipPoint, 1));
            drawInfo.SetInputDescription(1, new InputDescription(Filter.MinimumMagMipPoint, 1));
            drawInfo.SetInputDescription(2, new InputDescription(Filter.MinimumMagMipPoint, 1));
        }

        /// <inheritdoc />
        public override void Initialize(EffectContext effectContext, TransformGraph transformGraph)
        {
            effectContext.LoadPixelShader(GUID_YV12ConverterPixelShader, _yv12PsBytes);
            transformGraph.SetSingleTransformNode(this);
        }
    }
}