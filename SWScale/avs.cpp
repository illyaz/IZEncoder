#include "stdafx.h"

const AVS_Linkage *AVS_linkage = nullptr;

static AVSValue __cdecl CreateSWScale(AVSValue Args, void* UserData, IScriptEnvironment* Env) {
	return new SWScale(Args[0].AsClip(), Args[1].AsInt(0), Args[2].AsInt(0), Args[3].AsString("BICUBIC"), Args[4].AsString(""), Env);
}

extern "C" __declspec(dllexport) const char* __stdcall AvisynthPluginInit3(IScriptEnvironment* Env, const AVS_Linkage* const vectors) {
	AVS_linkage = vectors;
	Env->AddFunction("SWScale", "c[width]i[height]i[resizer]s[colorspace]s", CreateSWScale, nullptr);
	return "SWScale Colorspace Converter for IZEncoder 1.0";
}
