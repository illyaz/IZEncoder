#ifndef AVISYNTHVALUE_H
#define AVISYNTHVALUE_H

namespace IZEncoderNative {
	namespace Avisynth {
		ref class AvisynthBridge;

		private class AvisynthValueHolder {
		public:
			AVSValue val;
		};

		public ref class AvisynthValue {
			AvisynthValueHolder* h;
			AvisynthBridge ^env;
			bool _released;
			void ThrowIfReleased();
		internal:
			AvisynthValue(AVSValue v, AvisynthBridge ^env);
			void Release();
			void SetNewValue(AVSValue v, AvisynthBridge ^env);
		public:
			AvisynthValue(bool v, AvisynthBridge ^env)
				: AvisynthValue(AVSValue(v), env) { }

			AvisynthValue(int v, AvisynthBridge ^env)
				: AvisynthValue(AVSValue(v), env) { }

			AvisynthValue(float v, AvisynthBridge ^env)
				: AvisynthValue(AVSValue(v), env) { }

			AvisynthValue(double v, AvisynthBridge ^env)
				: AvisynthValue(AVSValue(v), env) { }

			AvisynthValue(String^ v, AvisynthBridge ^env)
				: AvisynthValue(AVSValue(string2char(v)), env) { }


			AVSValue GetAVSValue() { ThrowIfReleased(); return h->val; }
			int ArraySize() { ThrowIfReleased(); return h->val.ArraySize(); }
			bool Defined() { ThrowIfReleased(); return h->val.Defined(); }
			bool IsClip() { ThrowIfReleased(); return h->val.IsClip(); }
			bool IsBool() { ThrowIfReleased(); return h->val.IsBool(); }
			bool IsInt() { ThrowIfReleased(); return h->val.IsInt(); }
			bool IsFloat() { ThrowIfReleased(); return h->val.IsFloat(); }
			bool IsString() { ThrowIfReleased(); return h->val.IsString(); }
			bool IsArray() { ThrowIfReleased(); return h->val.IsArray(); }

			property AvisynthValue^ default[int]{
				virtual AvisynthValue^ get(int index) { return gcnew AvisynthValue(h->val[index], env); }
			}

			bool AsBool() { ThrowIfReleased(); return h->val.AsBool(); }
			int AsInt() { ThrowIfReleased(); return h->val.AsInt(); }
			String^ AsString() { ThrowIfReleased(); return gcnew String(h->val.AsString()); }
			double AsFloat() { ThrowIfReleased(); return h->val.AsFloat(); }
			float AsFloatf() { ThrowIfReleased(); return h->val.AsFloatf(); }

			bool AsBool(bool def) { ThrowIfReleased(); return h->val.AsBool(def); }
			int AsInt(int def) { ThrowIfReleased(); return h->val.AsInt(def); }
			String^ AsString(String^ def) { ThrowIfReleased(); return gcnew String(h->val.AsString(string2char(def))); }
			double AsFloat(double def) { ThrowIfReleased(); return h->val.AsFloat((float)def); }
			float AsFloatf(float def) { ThrowIfReleased(); return h->val.AsFloatf(def); }
		};
	}
}
#endif
