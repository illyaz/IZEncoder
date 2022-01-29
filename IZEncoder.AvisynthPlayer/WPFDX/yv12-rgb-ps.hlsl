//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//// PARTICULAR PURPOSE.
////
//// Copyright (c) Microsoft Corporation. All rights reserved

Texture2D yTexture : register(t0);
Texture2D uTexture : register(t1);
Texture2D vTexture : register(t2);
SamplerState texSampler;

static const float3x3 yuvCoef = {
	1.164f,  1.164f, 1.164f,
	0.000f, -0.213f, 2.112f,
	1.793f, -0.533f, 0.000f };

float4 main(
    float4 pos      : SV_POSITION,
    float4 posScene : SCENE_POSITION,
    float4 uv0      : TEXCOORD0
) : SV_Target
{
	float3 yuv = float3(yTexture.Sample(texSampler, uv0).r,
						uTexture.Sample(texSampler, uv0).r,
						vTexture.Sample(texSampler, uv0).r);

	// Do YUV->RGB conversion
	yuv -= float3(0.0625f, 0.5f, 0.5f);
	yuv = mul(yuv, yuvCoef); // `yuv` now contains RGB
	yuv = saturate(yuv);

	// Return RGBA
	return float4(yuv,1);
}