#include "stdafx.h"

AVPixelFormat CSNameToPIXFMT(const char *CSName, AVPixelFormat Default, bool HighBitDepth) {
	if (!CSName)
		return AV_PIX_FMT(NONE);
	std::string s = CSName;
	std::transform(s.begin(), s.end(), s.begin(), toupper);
	if (s == "")
		return Default;
	if (s == "YUV9" || s == "YUV410P8")
		return AV_PIX_FMT(YUV410P);
	if (s == "YV411" || s == "YUV411P8")
		return AV_PIX_FMT(YUV411P);
	if (s == "YV12" || s == "YUV420P8")
		return AV_PIX_FMT(YUV420P);
	if (s == "YV16" || s == "YUV422P8")
		return AV_PIX_FMT(YUV422P);
	if (s == "YV24" || s == "YUV444P8")
		return AV_PIX_FMT(YUV444P);
	if (s == "Y8" || s == "GRAY8")
		return AV_PIX_FMT(GRAY8);
	if (s == "YUY2")
		return AV_PIX_FMT(YUYV422);
	if (s == "RGB24")
		return AV_PIX_FMT(BGR24);
	if (s == "RGB32")
		return AV_PIX_FMT(RGB32);
	if (HighBitDepth) {
		if (s == "YUVA420P8")
			return AV_PIX_FMT(YUVA420P);
		if (s == "YUVA422P8")
			return AV_PIX_FMT(YUVA422P);
		if (s == "YUVA444P8")
			return AV_PIX_FMT(YUVA444P);
		if (s == "YUV420P16")
			return AV_PIX_FMT(YUV420P16);
		if (s == "YUVA420P16")
			return AV_PIX_FMT(YUVA420P16);
		if (s == "YUV422P16")
			return AV_PIX_FMT(YUV422P16);
		if (s == "YUVA422P16")
			return AV_PIX_FMT(YUVA422P16);
		if (s == "YUV444P16")
			return AV_PIX_FMT(YUV444P16);
		if (s == "YUVA444P16")
			return AV_PIX_FMT(YUVA444P16);
		if (s == "YUV420P10")
			return AV_PIX_FMT(YUV420P10);
		if (s == "YUVA420P10")
			return AV_PIX_FMT(YUVA420P10);
		if (s == "YUV422P10")
			return AV_PIX_FMT(YUV422P10);
		if (s == "YUVA422P10")
			return AV_PIX_FMT(YUVA422P10);
		if (s == "YUV444P10")
			return AV_PIX_FMT(YUV444P10);
		if (s == "YUVA444P10")
			return AV_PIX_FMT(YUVA444P10);
		if (s == "RGBP16")
			return AV_PIX_FMT(GBRP16);
		if (s == "RGBAP16")
			return AV_PIX_FMT(GBRAP16);
		if (s == "Y16" || s == "GRAY16")
			return AV_PIX_FMT(GRAY16);
	}

	return AV_PIX_FMT(NONE);
}

int ResizerNameToSWSResizer(const char *ResizerName) {
	if (!_stricmp(ResizerName, "FAST_BILINEAR"))
		return SWS_FAST_BILINEAR;
	if (!_stricmp(ResizerName, "BILINEAR"))
		return SWS_BILINEAR;
	if (!_stricmp(ResizerName, "BICUBIC"))
		return SWS_BICUBIC;
	if (!_stricmp(ResizerName, "X"))
		return SWS_X;
	if (!_stricmp(ResizerName, "POINT"))
		return SWS_POINT;
	if (!_stricmp(ResizerName, "AREA"))
		return SWS_AREA;
	if (!_stricmp(ResizerName, "BICUBLIN"))
		return SWS_BICUBLIN;
	if (!_stricmp(ResizerName, "GAUSS"))
		return SWS_GAUSS;
	if (!_stricmp(ResizerName, "SINC"))
		return SWS_SINC;
	if (!_stricmp(ResizerName, "LANCZOS"))
		return SWS_LANCZOS;
	if (!_stricmp(ResizerName, "SPLINE"))
		return SWS_SPLINE;
	return 0;
}

SwsContext *GetSwsContext(int SrcW, int SrcH, AVPixelFormat SrcFormat, int SrcColorSpace, int SrcColorRange, int DstW, int DstH, AVPixelFormat DstFormat, int DstColorSpace, int DstColorRange, int64_t Flags) {
	Flags |= SWS_FULL_CHR_H_INT | SWS_FULL_CHR_H_INP | SWS_ACCURATE_RND;
	SwsContext *Context = sws_alloc_context();
	if (!Context) return nullptr;

	// 0 = limited range, 1 = full range
	int SrcRange = SrcColorRange == AVCOL_RANGE_JPEG;
	int DstRange = DstColorRange == AVCOL_RANGE_JPEG;

	av_opt_set_int(Context, "sws_flags", Flags, 0);
	av_opt_set_int(Context, "srcw", SrcW, 0);
	av_opt_set_int(Context, "srch", SrcH, 0);
	av_opt_set_int(Context, "dstw", DstW, 0);
	av_opt_set_int(Context, "dsth", DstH, 0);
	av_opt_set_int(Context, "src_range", SrcRange, 0);
	av_opt_set_int(Context, "dst_range", DstRange, 0);
	av_opt_set_int(Context, "src_format", SrcFormat, 0);
	av_opt_set_int(Context, "dst_format", DstFormat, 0);

	sws_setColorspaceDetails(Context,
		sws_getCoefficients(SrcColorSpace), SrcRange,
		sws_getCoefficients(DstColorSpace), DstRange,
		0, 1 << 16, 1 << 16);

	if (sws_init_context(Context, nullptr, nullptr) < 0) {
		sws_freeContext(Context);
		return nullptr;
	}

	return Context;
}

AVColorSpace GetAssumedColorSpace(int W, int H) {
	if (W > 1024 || H >= 600)
		return AVCOL_SPC_BT709;
	else
		return AVCOL_SPC_BT470BG;
}

int GetPixFmt(const char *Name) {
	return av_get_pix_fmt(Name);
}

SWScale::SWScale(PClip Child, int ResizeToWidth, int ResizeToHeight, const char *ResizerName, const char *ConvertToFormatName, IScriptEnvironment *Env) : GenericVideoFilter(Child) {
	Context = NULL;
	OrigWidth = vi.width;
	OrigHeight = vi.height;
	FlipOutput = vi.IsYUV();

	AVPixelFormat ConvertFromFormat = AV_PIX_FMT_NONE;

	if (vi.pixel_type == VideoInfo::CS_I420)
		ConvertFromFormat = av_get_pix_fmt("yuv420p");
	else if (vi.pixel_type == VideoInfo::CS_YUVA420)
		ConvertFromFormat = av_get_pix_fmt("yuva420p");
	else if (vi.pixel_type == VideoInfo::CS_YV16)
		ConvertFromFormat = av_get_pix_fmt("yuv422p");
	else if (vi.pixel_type == VideoInfo::CS_YUVA422)
		ConvertFromFormat = av_get_pix_fmt("yuva422p");
	else if (vi.pixel_type == VideoInfo::CS_YV24)
		ConvertFromFormat = av_get_pix_fmt("yuv444p");
	else if (vi.pixel_type == VideoInfo::CS_YUVA444)
		ConvertFromFormat = av_get_pix_fmt("yuva444p");
	else if (vi.pixel_type == VideoInfo::CS_YV411)
		ConvertFromFormat = av_get_pix_fmt("yuv411p");
	else if (vi.pixel_type == VideoInfo::CS_YUV9)
		ConvertFromFormat = av_get_pix_fmt("yuv410p");
	else if (vi.pixel_type == VideoInfo::CS_Y8)
		ConvertFromFormat = av_get_pix_fmt("gray8");
	else if (vi.pixel_type == VideoInfo::CS_YUY2)
		ConvertFromFormat = av_get_pix_fmt("yuyv422");
	else if (vi.pixel_type == VideoInfo::CS_BGR32)
		ConvertFromFormat = av_get_pix_fmt("rgb32");
	else if (vi.pixel_type == VideoInfo::CS_BGR24)
		ConvertFromFormat = av_get_pix_fmt("bgr24");
	else if (vi.pixel_type == VideoInfo::CS_YUV420P16)
		ConvertFromFormat = av_get_pix_fmt("yuv420p16");
	else if (vi.pixel_type == VideoInfo::CS_YUVA420P16)
		ConvertFromFormat = av_get_pix_fmt("yuva420p16");
	else if (vi.pixel_type == VideoInfo::CS_YUV422P16)
		ConvertFromFormat = av_get_pix_fmt("yuv422p16");
	else if (vi.pixel_type == VideoInfo::CS_YUVA422P16)
		ConvertFromFormat = av_get_pix_fmt("yuva422p16");
	else if (vi.pixel_type == VideoInfo::CS_YUV444P16)
		ConvertFromFormat = av_get_pix_fmt("yuv444p16");
	else if (vi.pixel_type == VideoInfo::CS_YUVA444P16)
		ConvertFromFormat = av_get_pix_fmt("yuva444p16");
	else if (vi.pixel_type == VideoInfo::CS_YUV420P10)
		ConvertFromFormat = av_get_pix_fmt("yuv420p10");
	else if (vi.pixel_type == VideoInfo::CS_YUVA420P10)
		ConvertFromFormat = av_get_pix_fmt("yuva420p10");
	else if (vi.pixel_type == VideoInfo::CS_YUV422P10)
		ConvertFromFormat = av_get_pix_fmt("yuv422p10");
	else if (vi.pixel_type == VideoInfo::CS_YUVA422P10)
		ConvertFromFormat = av_get_pix_fmt("yuva422p10");
	else if (vi.pixel_type == VideoInfo::CS_YUV444P10)
		ConvertFromFormat = av_get_pix_fmt("yuv444p10");
	else if (vi.pixel_type == VideoInfo::CS_YUVA444P10)
		ConvertFromFormat = av_get_pix_fmt("yuva444p10");
	else if (vi.pixel_type == VideoInfo::CS_RGBP16)
		ConvertFromFormat = av_get_pix_fmt("gbrp16");
	else if (vi.pixel_type == VideoInfo::CS_RGBAP16)
		ConvertFromFormat = av_get_pix_fmt("gbrap16");
	else if (vi.pixel_type == VideoInfo::CS_Y16)
		ConvertFromFormat = av_get_pix_fmt("gray16");


	if (ResizeToHeight <= 0)
		ResizeToHeight = OrigHeight;
	else
		vi.height = ResizeToHeight;

	if (ResizeToWidth <= 0)
		ResizeToWidth = OrigWidth;
	else
		vi.width = ResizeToWidth;

	AVPixelFormat ConvertToFormat = CSNameToPIXFMT(ConvertToFormatName, ConvertFromFormat, true);
	if (ConvertToFormat == AV_PIX_FMT_NONE)
		Env->ThrowError("SWScale: Invalid colorspace specified (%s)", ConvertToFormatName);

	if (ConvertToFormat == av_get_pix_fmt("yuvj420p") || ConvertToFormat == av_get_pix_fmt("yuv420p"))
		vi.pixel_type = VideoInfo::CS_I420;
	else if (ConvertToFormat == av_get_pix_fmt("yuva420p"))
		vi.pixel_type = VideoInfo::CS_YUVA420;
	else if (ConvertToFormat == av_get_pix_fmt("yuvj422p") || ConvertToFormat == av_get_pix_fmt("yuv422p"))
		vi.pixel_type = VideoInfo::CS_YV16;
	else if (ConvertToFormat == av_get_pix_fmt("yuva422p"))
		vi.pixel_type = VideoInfo::CS_YUVA422;
	else if (ConvertToFormat == av_get_pix_fmt("yuvj444p") || ConvertToFormat == av_get_pix_fmt("yuv444p"))
		vi.pixel_type = VideoInfo::CS_YV24;
	else if (ConvertToFormat == av_get_pix_fmt("yuva444p"))
		vi.pixel_type = VideoInfo::CS_YUVA444;
	else if (ConvertToFormat == av_get_pix_fmt("yuv411p"))
		vi.pixel_type = VideoInfo::CS_YV411;
	else if (ConvertToFormat == av_get_pix_fmt("yuv410p"))
		vi.pixel_type = VideoInfo::CS_YUV9;
	else if (ConvertToFormat == av_get_pix_fmt("gray8"))
		vi.pixel_type = VideoInfo::CS_Y8;
	else if (ConvertToFormat == av_get_pix_fmt("yuyv422"))
		vi.pixel_type = VideoInfo::CS_YUY2;
	else if (ConvertToFormat == av_get_pix_fmt("rgb32"))
		vi.pixel_type = VideoInfo::CS_BGR32;
	else if (ConvertToFormat == av_get_pix_fmt("bgr24"))
		vi.pixel_type = VideoInfo::CS_BGR24;
	else if (ConvertToFormat == av_get_pix_fmt("yuv420p16"))
		vi.pixel_type = VideoInfo::CS_YUV420P16;
	else if (ConvertToFormat == av_get_pix_fmt("yuva420p16"))
		vi.pixel_type = VideoInfo::CS_YUVA420P16;
	else if (ConvertToFormat == av_get_pix_fmt("yuv422p16"))
		vi.pixel_type = VideoInfo::CS_YUV422P16;
	else if (ConvertToFormat == av_get_pix_fmt("yuva422p16"))
		vi.pixel_type = VideoInfo::CS_YUVA422P16;
	else if (ConvertToFormat == av_get_pix_fmt("yuv444p16"))
		vi.pixel_type = VideoInfo::CS_YUV444P16;
	else if (ConvertToFormat == av_get_pix_fmt("yuva444p16"))
		vi.pixel_type = VideoInfo::CS_YUVA444P16;
	else if (ConvertToFormat == av_get_pix_fmt("yuv420p10"))
		vi.pixel_type = VideoInfo::CS_YUV420P10;
	else if (ConvertToFormat == av_get_pix_fmt("yuva420p10"))
		vi.pixel_type = VideoInfo::CS_YUVA420P10;
	else if (ConvertToFormat == av_get_pix_fmt("yuv422p10"))
		vi.pixel_type = VideoInfo::CS_YUV422P10;
	else if (ConvertToFormat == av_get_pix_fmt("yuva422p10"))
		vi.pixel_type = VideoInfo::CS_YUVA422P10;
	else if (ConvertToFormat == av_get_pix_fmt("yuv444p10"))
		vi.pixel_type = VideoInfo::CS_YUV444P10;
	else if (ConvertToFormat == av_get_pix_fmt("yuva444p10"))
		vi.pixel_type = VideoInfo::CS_YUVA444P10;
	else if (ConvertToFormat == av_get_pix_fmt("gbrp16"))
		vi.pixel_type = VideoInfo::CS_RGBP16;
	else if (ConvertToFormat == av_get_pix_fmt("gbrap16"))
		vi.pixel_type = VideoInfo::CS_RGBAP16;
	else if (ConvertToFormat == av_get_pix_fmt("gray16"))
		vi.pixel_type = VideoInfo::CS_Y16;

	FlipOutput ^= vi.IsYUV();

	int Resizer = ResizerNameToSWSResizer(ResizerName);
	if (Resizer == 0)
		Env->ThrowError("SWScale: Invalid resizer specified (%s)", ResizerName);

	if (ConvertToFormat == AV_PIX_FMT_YUV420P && vi.height & 1)
		Env->ThrowError("SWScale: mod 2 output height required");

	if ((ConvertToFormat == AV_PIX_FMT_YUV420P || ConvertToFormat == AV_PIX_FMT_YUYV422) && vi.width & 1)
		Env->ThrowError("SWScale: mod 2 output width required");

	Context = GetSwsContext(
		OrigWidth, OrigHeight, ConvertFromFormat, GetAssumedColorSpace(OrigWidth, OrigHeight), AVCOL_RANGE_UNSPECIFIED,
		vi.width, vi.height, ConvertToFormat, GetAssumedColorSpace(OrigWidth, OrigHeight), AVCOL_RANGE_UNSPECIFIED,
		Resizer);

	if (Context == NULL)
		Env->ThrowError("SWScale: Context creation failed");
}

SWScale::~SWScale() {
	if (Context)
		sws_freeContext(Context);
}

PVideoFrame SWScale::GetFrame(int n, IScriptEnvironment *Env) {
	//child->SetCacheHints(CachePolicyHint::CACHE_NOTHING, 0);
	//SetCacheHints(CachePolicyHint::CACHE_NOTHING, 0);
	PVideoFrame Src = child->GetFrame(n, Env);
	PVideoFrame Dst = Env->NewVideoFrame(vi);

	const uint8_t *SrcData[4] = { Src->GetReadPtr(PLANAR_Y), Src->GetReadPtr(PLANAR_U), Src->GetReadPtr(PLANAR_V) };
	int SrcStride[4] = { Src->GetPitch(PLANAR_Y), Src->GetPitch(PLANAR_U), Src->GetPitch(PLANAR_V) };

	if (FlipOutput)
	{
		uint8_t *DstData[4] = { Dst->GetWritePtr(PLANAR_Y) + Dst->GetPitch(PLANAR_Y) * (Dst->GetHeight(PLANAR_Y) - 1), Dst->GetWritePtr(PLANAR_U) + Dst->GetPitch(PLANAR_U) * (Dst->GetHeight(PLANAR_U) - 1), Dst->GetWritePtr(PLANAR_V) + Dst->GetPitch(PLANAR_V) * (Dst->GetHeight(PLANAR_V) - 1) };
		int DstStride[4] = { -Dst->GetPitch(PLANAR_Y), -Dst->GetPitch(PLANAR_U), -Dst->GetPitch(PLANAR_V) };

		sws_scale(Context, SrcData, SrcStride, 0, OrigHeight, DstData, DstStride);
	}
	else
	{
		uint8_t *DstData[4] = { Dst->GetWritePtr(PLANAR_Y), Dst->GetWritePtr(PLANAR_U), Dst->GetWritePtr(PLANAR_V) };
		int DstStride[4] = { Dst->GetPitch(PLANAR_Y), Dst->GetPitch(PLANAR_U), Dst->GetPitch(PLANAR_V) };
		sws_scale(Context, SrcData, SrcStride, 0, OrigHeight, DstData, DstStride);
	}
	return Dst;
}
