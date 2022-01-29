#ifndef IZENCODER_AVISYNTHBRIDGE_H
#define IZENCODER_AVISYNTHBRIDGE_H

#include <map>
using namespace System;
using namespace System::Linq;
using namespace System::Collections::Generic;

namespace IZEncoderNative {
	namespace Avisynth {
		struct cmp_str
		{
			bool operator()(char const *a, char const *b) const
			{
				return std::strcmp(a, b) < 0;
			}
		};

		struct Trash {
			virtual ~Trash();
		};

		private class ApplyFuncWrapHolder {
		public:
			void* m_env;
			void* m_func_target;
			void* m_func;
			void* m_udata;
		};

		private class AvisynthBridgeHolder {
		public:
			IScriptEnvironment2 * env;
			HMODULE module;
			const char* version_string;
			std::map<const char*, AVSValue, cmp_str> values;
			std::map<const char*, PClip, cmp_str> clips;
		};

		public delegate AvisynthValue^ ApplyFuncWrapDelegate(AvisynthValue^ args, AvisynthBridge^ env, Object^ user_data);
		typedef AVSValue(*ApplyFuncWrapInternalDelegateC)(AVSValue, void*);
		static AVSValue ApplyFuncWrap(AVSValue args, void* user_data, IScriptEnvironment* env) {
			return ((ApplyFuncWrapInternalDelegateC)((ApplyFuncWrapHolder*)user_data)->m_func)(args, user_data);
		}

		public ref class AvisynthBridge {
			AvisynthBridgeHolder *h;
			Dictionary<String^, AvisynthClip^>^ m_clips;

			bool _disposed;

			void ThrowIfDisposed();
			AVSValue AVSValueFromObjectArray(array<Object^>^ args);
			const char** ToCPPCharArray(array<String^>^ args);
			AVSValue Invoke(String^ functionName, array<Object^>^ args, array<String^>^ argNames);

		internal:
			delegate AVSValue ApplyFuncWrapInternalDelegate(AVSValue, void*);
			AVSValue ApplyFuncWrapInternal(AVSValue args, void* user_data);
			ApplyFuncWrapInternalDelegate^ _applyFuncWrapInternal;
		public:
			AvisynthBridge();
			~AvisynthBridge();
			virtual property String^ Version { String^ get(); void set(String^ val); }
			IScriptEnvironment* GetScriptEnvironment() { return h->env; }
			IntPtr GetScriptEnvironmenPointer() { return IntPtr(h->env); }
	 		bool FunctionExists(String^ functionName);
			bool InternalFunctionExists(String^ functionName);
			String^ GetPluginFunctionParams(String^ functionName);
			String^ GetInternalFunctionParams(String^ functionName);
			array<String^>^ GetPluginFunctions();
			array<String^>^ GetInternalFunctions();
			void LoadPlugin(String^ pluginPath);
			void InvokeOnly(String^ functionName, ...array<Object^>^ args) { InvokeOnly(functionName, args, nullptr); }
			void InvokeOnly(String^ functionName, array<Object^>^ args, array<String^>^ argNames) { auto result = Invoke(functionName, args, argNames); result = NULL; }
			void SetFilterMTMode(String^ filterName, int mode)
			{
				h->env->SetFilterMTMode(string2char(filterName), (MtMode)mode, true);
			}
			AvisynthClip^ CreateClip(String^ key, String^ functionName, ...array<Object^>^ args);
			AvisynthClip^ CreateClip(String^ key, String^ functionName, array<Object^>^ args, array<String^>^ argNames);
			AvisynthClip^ CopyClip(String^ key, AvisynthClip^ clip);
			AvisynthClip^ GetClip(String^ key);
			String^ GetClipKey(AvisynthClip^ clip);
			void Remove(String^ key);
			void AddFunction(String^ name, String^ params, ApplyFuncWrapDelegate^ func, Object^ user_data);
			void ThrowError(String^ msg, ...array<Object^>^ fmt) { h->env->ThrowError(string2char(String::Format(msg, fmt))); }
			bool IsPrefetched()
			{
				/* C#, Prefetcher object find code
				*for (var i = 0; i < 1024; i++)
				{
				if (Marshal.ReadIntPtr(env_pointer, i) == prefetcher_pointer)
				Console.WriteLine(Marshal.ReadIntPtr(env_pointer, i));
				}*/

				// 504
				auto prefetcher = Marshal::ReadIntPtr(IntPtr(h->env), 500).ToPointer();
				if(prefetcher != 0)
				{
					try {
						if (dynamic_cast<Prefetcher*>((IClip*)prefetcher))
							return true;
						else
							throw;
					}
					catch (...)
					{
						throw gcnew AvisynthBridgeException("Unknown error occurred. Possible unsupported avisynth version");
					}
				}

				return false;
			}
		};
	}
}

#endif // !IZENCODER_AVISYNTHBRIDGE_H
