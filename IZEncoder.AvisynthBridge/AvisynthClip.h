#pragma once
#include "AvisynthVideoinfo.h"
#include "AvisynthVideoFrame.h"

namespace IZEncoderNative {
	namespace Avisynth {
		ref class AvisynthBridge;

		private class AvisynthClipHolder {
		public:
			PClip clip;
			VideoInfo vi;
		};
		public ref class AvisynthClip
		{
			AvisynthClipHolder* h;
			AvisynthBridge ^env;
			bool _released;
		internal:
			AvisynthClip(PClip clip, AvisynthBridge ^env);
			void Release();
			void SetNewClip(PClip clip, AvisynthBridge ^env);
			void ThrowIfReleased();
		public:
			property AvisynthVideoInfo Info;
			AvisynthVideoFrame ^ GetFrame(int n);
			AvisynthVideoFrame ^ GetFrame(int n, bool ignoreError);
			long long GetAudio(array<Byte>^ buf, long long offset, long long count);
			long long GetAudio(array<short>^ buf, long long offset, long long count);
			long long GetAudio(array<int>^ buf, long long offset, long long count);
			long long GetAudio(array<float>^ buf, long long offset, long long count);
			AvisynthVideoInfo GetVideoInfo();
			PClip GetPClip() { return h->clip; }
			IntPtr GetPrefetcher() { return IntPtr(dynamic_cast<Prefetcher*>((IClip*)(void*)h->clip)); }
			bool IsPrefetched() { return GetPrefetcher() != IntPtr::Zero; }
		};
	}
}

