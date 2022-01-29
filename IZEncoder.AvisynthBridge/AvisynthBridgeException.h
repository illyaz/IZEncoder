#pragma once
namespace IZEncoderNative {
	namespace Avisynth {
		using namespace System;

		public ref class AvisynthBridgeException :
			public ApplicationException
		{
		public:
			AvisynthBridgeException()
				: ApplicationException() { }

			AvisynthBridgeException(String^ message)
				: ApplicationException(message) { }

			AvisynthBridgeException(String^ message, Exception^ innerException)
				: ApplicationException(message, innerException) { }
		};
	}
}
