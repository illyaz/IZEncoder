#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "avisynth.h"

class IZTransparentClip : public IClip
{
private:
	int w, h, n, d, f;
	VideoInfo vi;
public:
	IZTransparentClip(int width, int height, int numerator, int denominator, int frames, IScriptEnvironment* env)
		: w(width), h(height), n(numerator), d(denominator), f(frames)
	{
		memset(&vi, 0, sizeof(VideoInfo));
		vi.width = w;
		vi.height = h;
		vi.fps_numerator = n;
		vi.fps_denominator = d;
		vi.num_frames = f;
		vi.pixel_type = VideoInfo::CS_BGR32;
	}

	PVideoFrame GetFrame(int n, IScriptEnvironment* Env) override
	{
		auto frame = Env->NewVideoFrame(vi);
		auto s = vi.BMPSize();

		int y, i;
		BYTE* p;
		int fpitch, fheight, fwidth;

		p = frame->GetWritePtr();
		fpitch = frame->GetPitch();
		fwidth = frame->GetRowSize(); // in bytes
		fheight = frame->GetHeight(); // in pixels

		for (y = 0; y < fheight; y++)
		{
			for (i = 0; i < fwidth; i += 4)
			{
				p[i + 3] = 255;
			}
			p += fpitch;
		}
		return frame;
	}

	void __stdcall GetAudio(void* buf, __int64 start, __int64 count, IScriptEnvironment* env) override
	{
	};
	bool __stdcall GetParity(int n) override { return false; };
	int __stdcall SetCacheHints(int cachehints, int frame_range) override { return 0; };
	const VideoInfo& __stdcall GetVideoInfo() override { return vi; };
};

static AVSValue __cdecl Create_IZTransparentClip(AVSValue args, void* user_data, IScriptEnvironment* env)
{
	return new IZTransparentClip(args[0].AsInt(640), args[1].AsInt(480), args[2].AsInt(24000), args[3].AsInt(1001),
	                             args[4].AsInt(1000), env);
}

const AVS_Linkage* AVS_linkage = nullptr;

extern "C" __declspec(dllexport) const char* __stdcall AvisynthPluginInit3(
	IScriptEnvironment* env, const AVS_Linkage* const vectors)
{
	AVS_linkage = vectors;
	env->AddFunction("IZTransparentClip", "[width]i[height]i[numerator]i[denominator]i[frames]i",
	                 Create_IZTransparentClip, nullptr);
	return "IZTransparentClip V1.0";
}
