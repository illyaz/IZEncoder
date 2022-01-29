#include "stdafx.h"


namespace IZEncoderNative {
	namespace Avisynth {
		void AvisynthClip::ThrowIfReleased()
		{
			if (_released)
				throw gcnew InvalidOperationException("cannot access a released clip");			
		}

		AvisynthClip::AvisynthClip(PClip clip, AvisynthBridge ^env)
		{
			h = new AvisynthClipHolder();
			SetNewClip(clip, env);
		}

		void AvisynthClip::Release()
		{
			ThrowIfReleased();
			h->clip = NULL;
			delete h;
			_released = true;
		}

		void AvisynthClip::SetNewClip(PClip clip, AvisynthBridge ^ env)
		{
			ThrowIfReleased();
			h->clip = clip;
			h->vi = clip->GetVideoInfo();
			Info = AvisynthVideoInfo::FromNative(&h->vi);

			this->env = env;
		}

		AvisynthVideoFrame ^ AvisynthClip::GetFrame(int n)
		{
			return GetFrame(n, false);
		}

		AvisynthVideoFrame ^ AvisynthClip::GetFrame(int n, bool ignoreError)
		{
			ThrowIfReleased();
			try {
				return gcnew AvisynthVideoFrame(h->clip->GetFrame(n, env->GetScriptEnvironment()));
			}
			catch (AvisynthError e) {
				if (ignoreError)
					return gcnew AvisynthVideoFrame(env->GetScriptEnvironment()->NewVideoFrame(h->clip->GetVideoInfo()));
				else
					throw gcnew AvisynthBridgeException(gcnew String(e.msg));
			}
			catch (Exception^ e) {
				if (ignoreError)
					return gcnew AvisynthVideoFrame(env->GetScriptEnvironment()->NewVideoFrame(h->clip->GetVideoInfo()));
				else
					throw gcnew AvisynthBridgeException(e->Message, e);
			}
			catch (const std::runtime_error& re)
			{
				if (ignoreError)
					return gcnew AvisynthVideoFrame(env->GetScriptEnvironment()->NewVideoFrame(h->clip->GetVideoInfo()));
				else
					throw gcnew AvisynthBridgeException(String::Format("C++ Runtime Exception: {0}", gcnew String(re.what())));
			}
			catch (const std::exception& ex)
			{
				if (ignoreError)
					return gcnew AvisynthVideoFrame(env->GetScriptEnvironment()->NewVideoFrame(h->clip->GetVideoInfo()));
				else
					throw gcnew AvisynthBridgeException(String::Format("C++ Exception: {0}", gcnew String(ex.what())));
			}
			catch (...) {
				if (ignoreError)
					return gcnew AvisynthVideoFrame(env->GetScriptEnvironment()->NewVideoFrame(h->clip->GetVideoInfo()));
				else
					throw gcnew AvisynthBridgeException("Unknown error occurred. Possible memory corruption");
			}
		}

		long long AvisynthClip::GetAudio(array<Byte>^ buf, long long offset, long long count) {
			ThrowIfReleased();
			if (buf == nullptr)
				throw gcnew ArgumentNullException("buf", "Buffer cannot be null.");
			if (offset < 0)
				throw gcnew ArgumentOutOfRangeException("offset", "Index is less than zero.");
			if (count < 0)
				throw gcnew ArgumentOutOfRangeException("count", "Index is less than zero.");
			if (buf->Length < count)
				throw gcnew ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");

			pin_ptr<Byte> p = &buf[0];
			h->clip->GetAudio(p, offset, count, env->GetScriptEnvironment());

			return offset + count < h->vi.num_audio_samples ? count : h->vi.num_audio_samples - offset;
		};

		long long AvisynthClip::GetAudio(array<short>^ buf, long long offset, long long count) {
			ThrowIfReleased();
			if (buf == nullptr)
				throw gcnew ArgumentNullException("buf", "Buffer cannot be null.");
			if (offset < 0)
				throw gcnew ArgumentOutOfRangeException("offset", "Index is less than zero.");
			if (count < 0)
				throw gcnew ArgumentOutOfRangeException("count", "Index is less than zero.");
			if (buf->Length < count)
				throw gcnew ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");

			pin_ptr<short> p = &buf[0];
			h->clip->GetAudio(p, offset, count, env->GetScriptEnvironment());

			return offset + count < h->vi.num_audio_samples ? count : h->vi.num_audio_samples - offset;
		};

		long long AvisynthClip::GetAudio(array<int>^ buf, long long offset, long long count) {
			ThrowIfReleased();
			if (buf == nullptr)
				throw gcnew ArgumentNullException("buf", "Buffer cannot be null.");
			if (offset < 0)
				throw gcnew ArgumentOutOfRangeException("offset", "Index is less than zero.");
			if (count < 0)
				throw gcnew ArgumentOutOfRangeException("count", "Index is less than zero.");
			if (buf->Length < count)
				throw gcnew ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");

			pin_ptr<int> p = &buf[0];
			h->clip->GetAudio(p, offset, count, env->GetScriptEnvironment());

			return offset + count < h->vi.num_audio_samples ? count : h->vi.num_audio_samples - offset;
		};

		long long AvisynthClip::GetAudio(array<float>^ buf, long long offset, long long count) {
			ThrowIfReleased();
			if (buf == nullptr)
				throw gcnew ArgumentNullException("buf", "Buffer cannot be null.");
			if (offset < 0)
				throw gcnew ArgumentOutOfRangeException("offset", "Index is less than zero.");
			if (count < 0)
				throw gcnew ArgumentOutOfRangeException("count", "Index is less than zero.");
			if (buf->Length < count)
				throw gcnew ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");

			pin_ptr<float> p = &buf[0];
			h->clip->GetAudio(p, offset, count, env->GetScriptEnvironment());

			return offset + count < h->vi.num_audio_samples ? count : h->vi.num_audio_samples - offset;
		};

		AvisynthVideoInfo AvisynthClip::GetVideoInfo()
		{
			ThrowIfReleased();
			return AvisynthVideoInfo::FromNative(&h->vi);
		}
	}
}
