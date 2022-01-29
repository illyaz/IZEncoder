#include "stdafx.h"
#include "NativeString.h"

using namespace System::Text;

namespace IZEncoder {
	namespace AvisynthBridge {
		NativeString::NativeString(String^ s) {
			//Hack
			auto enc = Encoding::UTF8;
			int len = enc->GetByteCount(s);
			array<Byte>^ buffer = gcnew array<Byte>(len + 1);
			enc->GetBytes(s, 0, s->Length, buffer, 0);
			ip = Marshal::AllocHGlobal(buffer->Length);
			Marshal::Copy(buffer, 0, ip, buffer->Length);
			auto wstr = static_cast<const wchar_t*>(ip.ToPointer());
			str = (char*)ip.ToPointer();
			buffer = nullptr;;

			//ip = Marshal::StringToHGlobalAnsi(s);
			//str = static_cast<const char*>(ip.ToPointer());
		} 

		NativeString::~NativeString() {
			CleanUp();
		}

		NativeString::!NativeString() {
			// FIXME
			//CleanUp();
		}

		const char* NativeString::GetPointer() {
			return str;
		}

		void NativeString::CleanUp() {
			if (str) {
				str = NULL;
				Marshal::FreeHGlobal(ip);
				ip = IntPtr::Zero;
			}
		}
	}
}
