#include "stdafx.h"

namespace IZEncoderNative {
	namespace Avisynth {
		void AvisynthValue::ThrowIfReleased()
		{
			if (_released)
				throw gcnew InvalidOperationException("cannot access a released value");
		}

		AvisynthValue::AvisynthValue(AVSValue v, AvisynthBridge ^env)
		{
			h = new AvisynthValueHolder();
			SetNewValue(v, env);
		}

		void AvisynthValue::Release()
		{
			ThrowIfReleased();
			h->val = NULL;
			delete h;
			_released = true;
		}

		void AvisynthValue::SetNewValue(AVSValue v, AvisynthBridge ^ env)
		{
			ThrowIfReleased();
			h->val = v;
			this->env = env;
		}
	}
}
