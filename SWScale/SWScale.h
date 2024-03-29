#ifndef SWSCALE_H
#define SWSCALE_H

extern "C" {
#include <libavutil/opt.h>
#include <libavutil/imgutils.h>
#include <libavcodec/avcodec.h>
#include <libswscale/swscale.h>
}

#define AV_PIX_FMT(x) AV_PIX_FMT_##x

class SWScale : public GenericVideoFilter {
private:
	SwsContext * Context;
	int OrigWidth;
	int OrigHeight;
	bool FlipOutput;
public:
	SWScale(PClip Child, int ResizeToWidth, int ResizeToHeight, const char *ResizerName, const char *ConvertToFormatName, IScriptEnvironment *Env);
	~SWScale();
	PVideoFrame __stdcall GetFrame(int n, IScriptEnvironment *Env);
};


#endif

