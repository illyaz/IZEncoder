#ifndef VIDEOFRAME_H
#define VIDEOFRAME_H

using namespace System::Runtime::InteropServices;
using namespace System;
using namespace System::IO;

namespace IZEncoderNative {
	namespace Avisynth {
		private class AvisynthVideoFrameHolder {
		public:
			PVideoFrame vf;
		};
		public ref class AvisynthVideoFrame sealed {
		private:
			AvisynthVideoFrameHolder * h;
			bool _disposed;
			void ThrowIfDisposed();
		internal:
			AvisynthVideoFrame(PVideoFrame frame);
			~AvisynthVideoFrame();
		public:
			int GetPitch();
			int GetPitch(YUVPlanes plane);

			int GetRowSize();
			int GetRowSize(YUVPlanes plane);

			int GetHeight();
			int GetHeight(YUVPlanes plane);

			IntPtr GetReadPtr();
			IntPtr GetReadPtr(YUVPlanes plane);
			Stream^ GetReadStream();
			Stream^ GetReadStream(YUVPlanes plane);
		};
	}
}
#endif
