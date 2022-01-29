#include "stdafx.h"

namespace IZEncoderNative {
	namespace Avisynth {
		void AvisynthVideoFrame::ThrowIfDisposed()
		{
			if (_disposed)
				throw gcnew System::ObjectDisposedException(String::Empty);
		}

		AvisynthVideoFrame::AvisynthVideoFrame(PVideoFrame frame)
		{
			h = new AvisynthVideoFrameHolder();
			h->vf = frame;
		}

		AvisynthVideoFrame::~AvisynthVideoFrame()
		{
			ThrowIfDisposed();
			h->vf = NULL;
			delete h;
			_disposed = true;
		}

		int AvisynthVideoFrame::GetPitch() {
			ThrowIfDisposed();
			return h->vf->GetPitch();
		}

		int AvisynthVideoFrame::GetPitch(YUVPlanes plane) {
			ThrowIfDisposed();
			return h->vf->GetPitch((int)plane);
		}

		int AvisynthVideoFrame::GetRowSize() {
			ThrowIfDisposed();
			return h->vf->GetRowSize();
		}

		int AvisynthVideoFrame::GetRowSize(YUVPlanes plane) {
			ThrowIfDisposed();
			return h->vf->GetRowSize((int)plane);
		}

		int AvisynthVideoFrame::GetHeight() {
			ThrowIfDisposed();
			return h->vf->GetHeight();
		}

		int AvisynthVideoFrame::GetHeight(YUVPlanes plane) {
			ThrowIfDisposed();
			return h->vf->GetHeight((int)plane);
		}

		IntPtr AvisynthVideoFrame::GetReadPtr() {
			ThrowIfDisposed();
			return IntPtr((void*)h->vf->GetReadPtr());
		}

		IntPtr AvisynthVideoFrame::GetReadPtr(YUVPlanes plane) {
			ThrowIfDisposed();
			return IntPtr((void*)h->vf->GetReadPtr((int)plane));
		}

		Stream^ AvisynthVideoFrame::GetReadStream() {
			ThrowIfDisposed();
			int frameSize = (h->vf->GetPitch()*h->vf->GetHeight()) & 0x7fffffff;
			return gcnew UnmanagedMemoryStream((unsigned char *)h->vf->GetReadPtr(), frameSize, frameSize, FileAccess::Read);
		}

		Stream^ AvisynthVideoFrame::GetReadStream(YUVPlanes plane) {
			ThrowIfDisposed();
			int frameSize = (h->vf->GetPitch((int)plane)*h->vf->GetHeight((int)plane)) & 0x7fffffff;
			return gcnew UnmanagedMemoryStream((unsigned char *)h->vf->GetReadPtr((int)plane), frameSize, frameSize, FileAccess::Read);
		}
	}
}
