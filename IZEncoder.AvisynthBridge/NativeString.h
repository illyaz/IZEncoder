#pragma once
#ifndef NATIVESTRING_H
#define NATIVESTRING_H

#define string2char(_) ((gcnew IZEncoder::AvisynthBridge::NativeString(_))->GetPointer())

using namespace System;
using namespace System::Runtime::InteropServices;

namespace IZEncoder {
	namespace AvisynthBridge {
		private ref class NativeString sealed {
		private:
			IntPtr ip;
			const char* str;
			void CleanUp();

		public:
			NativeString(String^ s);
			~NativeString();
			!NativeString();
			const char* GetPointer();
		};
	}
}

#endif // !NATIVESTRING_H
