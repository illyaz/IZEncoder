#include "stdafx.h"

const AVS_Linkage *AVS_linkage = 0;

namespace IZEncoderNative {
	namespace Avisynth {
		static AVSValue CustomVersionString(AVSValue args, void* user_data, IScriptEnvironment* env) {
			return AVSValue(((AvisynthBridgeHolder*)user_data)->version_string);
		}

		AvisynthBridge::AvisynthBridge()
		{
			h = new AvisynthBridgeHolder();
			h->module = LoadLibraryEx(L"avisynth", NULL, 0);
			if(h->module == NULL)
				throw gcnew AvisynthBridgeException("Could not load avisynth library");

			auto createScriptEnv2 = (IScriptEnvironment2*(*)(int))GetProcAddress(h->module, "CreateScriptEnvironment2");
			
			if (createScriptEnv2 == NULL) {
				FreeLibrary(h->module);
				throw gcnew AvisynthBridgeException("Could not create avisynth script environment");
			}

			h->env = createScriptEnv2(AVISYNTH_INTERFACE_VERSION);
			if (h->env == NULL) {
				FreeLibrary(h->module);
				throw gcnew AvisynthBridgeException("Could not create avisynth script environment");
			}

			AVS_linkage = h->env->GetAVSLinkage();
			m_clips = gcnew Dictionary<String^, AvisynthClip^>();
			h->version_string = h->env->Invoke("VersionString", AVSValue(0, 0)).AsString();
			h->env->AddFunction("VersionString", "", CustomVersionString, (void*)h);
			_applyFuncWrapInternal = gcnew ApplyFuncWrapInternalDelegate(this, &AvisynthBridge::ApplyFuncWrapInternal);
		}

		void AvisynthBridge::ThrowIfDisposed()
		{
			if (_disposed)
				throw gcnew System::ObjectDisposedException(String::Empty);
		}

		AVSValue AvisynthBridge::AVSValueFromObjectArray(array<Object^>^ args)
		{
			// FIXME
			if (args && args->Length > 0) {
				auto _arrptr = new AVSValue[args->Length];
				for (int i = 0; i < args->Length; i++)
				{
					auto t = args[i]->GetType();
					if (args[i]->GetType() == bool::typeid)
						_arrptr[i] = AVSValue((bool)args[i]);
					else if (args[i]->GetType() == int::typeid)
						_arrptr[i] = AVSValue((int)args[i]);
					else if (args[i]->GetType() == float::typeid)
						_arrptr[i] = AVSValue((float)args[i]);
					else if (args[i]->GetType() == double::typeid)
						_arrptr[i] = AVSValue((double)args[i]);
					else if (args[i]->GetType() == String::typeid)
						_arrptr[i] = AVSValue(string2char((String^)args[i]));
					else if (args[i]->GetType() == AvisynthClip::typeid) {
						_arrptr[i] = AVSValue(((AvisynthClip^)args[i])->GetPClip());
					}
					else
						throw gcnew ArgumentException("Invalid value type");
				}

				return AVSValue(_arrptr, args->Length);
				/*std::vector<AVSValue> _arrptr(args->Length, AVSValue());
				for (int i = 0; i < args->Length; i++)
				{
				auto t = args[i]->GetType();
				if (args[i]->GetType() == bool::typeid)
				_arrptr[i] = AVSValue((bool)args[i]);
				else if (args[i]->GetType() == int::typeid)
				_arrptr[i] = AVSValue((int)args[i]);
				else if (args[i]->GetType() == float::typeid)
				_arrptr[i] = AVSValue((float)args[i]);
				else if (args[i]->GetType() == double::typeid)
				_arrptr[i] = AVSValue((double)args[i]);
				else if (args[i]->GetType() == String::typeid)
				_arrptr[i] = AVSValue(string2char((String^)args[i]));
				else if (args[i]->GetType() == AvisynthClip::typeid) {
				_arrptr[i] = AVSValue(((AvisynthClip^)args[i])->GetPClip());
				}
				else
				throw gcnew ArgumentException("Invalid value type");
				}

				return AVSValue(_arrptr.data(), _arrptr.size());*/
			}
			else
				return AVSValue(0, 0);
		}

		const char ** AvisynthBridge::ToCPPCharArray(array<String^>^ s_array)
		{
			const char** cpp_s_array = NULL;
			if (s_array && s_array->Length > 0) {
				cpp_s_array = (const char**)malloc(sizeof(char*) * s_array->Length);
				for (int i = 0; i < s_array->Length; i++) {
					if (String::IsNullOrEmpty(s_array[i])) {
						cpp_s_array[i] = NULL;
						continue;
					}

					cpp_s_array[i] = string2char(s_array[i]);
				}
			}

			return cpp_s_array;
		}

		AVSValue AvisynthBridge::Invoke(String ^ functionName, array<Object^>^ args, array<String^>^ argNames)
		{
			AVSValue avs_args;
			const char ** avs_argNames;
			try {
				avs_args = AVSValueFromObjectArray(args);
				avs_argNames = ToCPPCharArray(argNames);

				return h->env->Invoke((const char*)Marshal::StringToHGlobalAnsi(functionName).ToPointer(), avs_args, avs_argNames);
			}
			catch (IScriptEnvironment::NotFound) {
				throw gcnew AvisynthBridgeException(String::Format("There is no function named '{0}'.", functionName));
			}
			catch (AvisynthError e) {
				throw gcnew AvisynthBridgeException(gcnew String(e.msg));
			}
			catch (...) {
				throw gcnew AvisynthBridgeException("Unknown error");
			}
			finally
			{
				// Avisynth does not release value, bug ? (my bug)
				if (avs_args.IsArray()) {
					for (int i = 0; i < avs_args.ArraySize(); i++)
						avs_args[i].~AVSValue();
				}

				avs_args = NULL;
				delete[] avs_argNames;
			}
		}

		AvisynthBridge::~AvisynthBridge()
		{
			ThrowIfDisposed();
			auto clipList = Enumerable::ToList(m_clips->Keys);
			clipList->Reverse();
			for each(auto clip in clipList)
				Remove(clip);
			m_clips->Clear();
			h->env->DeleteScriptEnvironment();
			h->env = NULL;
			FreeLibrary(h->module);
			delete h;
			_disposed = true;
		}

		bool AvisynthBridge::FunctionExists(String ^ functionName)
		{
			return h->env->FunctionExists(string2char(functionName));
		}

		bool AvisynthBridge::InternalFunctionExists(String ^ functionName)
		{
			return h->env->InternalFunctionExists(string2char(functionName));
		}

		String^ AvisynthBridge::GetPluginFunctionParams(String^ functionName)
		{
			AVSValue result;
			try {
				result = h->env->GetVar(string2char("$Plugin!" + functionName + "!Param$"));
				return gcnew String(result.AsString());
			}
			catch (IScriptEnvironment::NotFound) {
				throw gcnew AvisynthBridgeException(String::Format("There is no function named '{0}'.", functionName));
			}
			catch (...) {
				throw gcnew AvisynthBridgeException("Unknown error");
			}
			finally
			{
				result = NULL;
			}
		}

		String^ AvisynthBridge::GetInternalFunctionParams(String^ functionName)
		{
			try {
				return gcnew String(h->env->GetVar(string2char("$InternalFunctions!" + functionName + "!Param$")).AsString());
			}
			catch (IScriptEnvironment::NotFound) {
				throw gcnew AvisynthBridgeException(String::Format("There is no function named '{0}'.", functionName));
			}
			catch (...) {
				throw gcnew AvisynthBridgeException("Unknown error");
			}
		}

		array<String^>^ AvisynthBridge::GetPluginFunctions()
		{
			try
			{
				return (gcnew String(h->env->GetVar("$PluginFunctions$").AsString()))->Trim()->Split(' ');
			}
			catch (...) {
				throw gcnew AvisynthBridgeException("Unknown error");
			}
		}

		array<String^>^ AvisynthBridge::GetInternalFunctions()
		{
			try
			{
				return (gcnew String(h->env->GetVar("$InternalFunctions$").AsString()))->Trim()->Split(' ');
			}
			catch (...) {
				throw gcnew AvisynthBridgeException("Unknown error");
			}
		}

		void AvisynthBridge::LoadPlugin(String ^ pluginPath)
		{
			AVSValue result;
			try {				
				h->env->LoadPlugin(string2char(pluginPath), true, &result);
			}
			catch (AvisynthError e) {
				throw gcnew AvisynthBridgeException(gcnew String(e.msg));
			}
			catch (...) {
				throw gcnew AvisynthBridgeException("Unknown error");
			}
			finally
			{
				result = NULL;
			}
		}

		AvisynthClip ^ AvisynthBridge::CreateClip(String ^ key, String ^ functionName, ...array<Object^>^ args)
		{
			return CreateClip(key, functionName, args, nullptr);
		}

		AvisynthClip ^ AvisynthBridge::CreateClip(String ^ key, String ^ functionName, array<Object^>^ args, array<String^>^ argNames)
		{
			ThrowIfDisposed();

			bool success = false;
			try {
				auto avs_args = AVSValueFromObjectArray(args);
				auto cKey = string2char(key);
				if (h->values.find(cKey) != h->values.end())
					h->values.erase(cKey);

				if (h->clips.find(cKey) != h->clips.end())
					h->clips.erase(cKey);

				try {
					auto val = h->env->Invoke(string2char(functionName), avs_args, ToCPPCharArray(argNames));
					if (val.IsClip())
					{
						h->values[cKey] = val;
						h->clips[cKey] = val.AsClip();
						auto clip = h->clips[cKey];
						if (m_clips->ContainsKey(key)) {
							m_clips[key]->SetNewClip(h->clips[cKey], this);
							success = true;
							return m_clips[key];
						}
						else {
							auto mclip = gcnew AvisynthClip(clip, this);
							m_clips->Add(key, mclip);
							success = true;
							return mclip;
						}
					}
					else
						throw gcnew AvisynthBridgeException("Filter does not return a clip.");
				}
				finally // Avisynth does not release value, idk
				{
					for (int i = 0; i < avs_args.ArraySize(); i++)
					avs_args[i].~AVSValue();

				avs_args = NULL;
				}
			}
			catch (IScriptEnvironment::NotFound) {
				throw gcnew AvisynthBridgeException(String::Format("There is no function named '{0}'.", functionName));
			}
			catch (AvisynthError e) {
				throw gcnew AvisynthBridgeException(gcnew String(e.msg));
			}
			catch (Exception^ e) {
				throw gcnew AvisynthBridgeException(e->Message, e);
			}
			catch (const std::runtime_error& re) {
				throw gcnew AvisynthBridgeException(String::Format("C++ Runtime Exception: {0}", gcnew String(re.what())));
			}
			catch (const std::exception& ex) {
				throw gcnew AvisynthBridgeException(String::Format("C++ Exception: {0}", gcnew String(ex.what())));
			}
			catch (...) {
				throw gcnew AvisynthBridgeException("Unknown error occurred. Possible memory corruption");
			}
			finally {
				if (!success)
				Remove(key);
			}
		}

		AvisynthClip ^ AvisynthBridge::CopyClip(String ^ key, AvisynthClip ^ clip)
		{
			ThrowIfDisposed();
			clip->ThrowIfReleased();
			if (GetClip(key) != nullptr)
				throw gcnew AvisynthBridgeException("Duplicate key");
			auto srcKey = string2char(GetClipKey(clip));
			auto cKey = string2char(key);
			h->values[cKey] = h->values[srcKey];
			h->clips[cKey] = h->clips[srcKey];

			auto mclip = gcnew AvisynthClip(h->clips[cKey], this);
			m_clips->Add(key, mclip);
			return mclip;
		}

		AvisynthClip ^ AvisynthBridge::GetClip(String ^ key)
		{
			ThrowIfDisposed();
			if (key == nullptr)
				return nullptr;

			if (m_clips->ContainsKey(key))
				return m_clips[key];
			else
				return nullptr;
		}

		String ^ AvisynthBridge::GetClipKey(AvisynthClip ^ clip)
		{
			ThrowIfDisposed();
			for each(auto kv in m_clips) {
				if (Object::ReferenceEquals(kv.Value, clip))
					return kv.Key;
			}

			return nullptr;
		}

		void AvisynthBridge::Remove(String ^ key)
		{
			ThrowIfDisposed();
			if (h->values.find(string2char(key)) != h->values.end())
				h->values.erase(string2char(key));

			if (h->clips.find(string2char(key)) != h->clips.end())
				h->clips.erase(string2char(key));

			if (m_clips->ContainsKey(key)) {
				m_clips[key]->Release();
				m_clips->Remove(key);
			}
		}

		AVSValue AvisynthBridge::ApplyFuncWrapInternal(AVSValue args, void * user_data)
		{
			auto ud = (ApplyFuncWrapHolder*)user_data;
			auto env = (AvisynthBridge^)GCHandle::FromIntPtr(IntPtr(ud->m_env)).Target;
			auto m_args = gcnew AvisynthValue(args, env);
			try {
				auto target = Marshal::GetDelegateForFunctionPointer<ApplyFuncWrapDelegate^>(IntPtr(ud->m_func_target));
				auto result = target(m_args, env, GCHandle::FromIntPtr(IntPtr(ud->m_udata)).Target);
				return result == nullptr ? NULL : result->GetAVSValue();
			}
			catch (AvisynthBridgeException^ e) {
				env->ThrowError(e->Message);
			}
			catch (Exception^ e) {
				env->ThrowError("Error in .net code: {0}", e->Message);
			}
			finally{
				if (m_args)
				{
					for (int i = 0; i < m_args->ArraySize(); i++)
						m_args[i]->Release();

					m_args->Release();
				}
			}

			throw "Nah ?";
		}

		void AvisynthBridge::AddFunction(String ^ name, String ^ params, ApplyFuncWrapDelegate ^ func, Object^ user_data)
		{
			auto func_h = new ApplyFuncWrapHolder();
			func_h->m_func_target = Marshal::GetFunctionPointerForDelegate(func).ToPointer();
			func_h->m_func = Marshal::GetFunctionPointerForDelegate(_applyFuncWrapInternal).ToPointer();
			func_h->m_env = GCHandle::ToIntPtr(GCHandle::Alloc(this)).ToPointer();
			func_h->m_udata = GCHandle::ToIntPtr(GCHandle::Alloc(user_data)).ToPointer();
			h->env->AddFunction(string2char(name), string2char(params), ApplyFuncWrap, func_h);
		}

		String^ AvisynthBridge::Version::get()
		{
			//ThrowIfDisposed();
			return gcnew String(h->version_string);
		}

		void AvisynthBridge::Version::set(String^ val)
		{
			/*ThrowIfDisposed();*/
			h->version_string = string2char(val);
		}
	}
}
