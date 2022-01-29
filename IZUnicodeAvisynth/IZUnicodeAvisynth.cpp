// IZUnicodeAvisynth.cpp : Defines the exported functions for the DLL application.
//

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include "MinHook.h"
#include "avisynth.h"
#include <string>
#include <vector>
#include <mutex>
#include <VersionHelpers.h>
#include <clocale>

std::vector<unsigned int> cps;
const AVS_Linkage* AVS_linkage = nullptr;

const wchar_t* GetWC(const char* c)
{
	const size_t cSize = strlen(c) + 1;
	auto* wc = new wchar_t[cSize];
	mbstowcs(wc, c, cSize);

	return wc;
}

std::wstring Convert(const std::wstring& s, const unsigned int code, const unsigned int tocode)
{
	if (s.empty())
		return std::wstring();

	auto len = WideCharToMultiByte(code, 0, s.c_str(), s.length(), nullptr, 0, nullptr, nullptr);
	if (len == 0)
		throw std::exception("IZUnicodeAvisynth: WideCharToMultiByte() failed");

	std::vector<char> bytes(len);

	len = WideCharToMultiByte(code, 0, s.c_str(), s.length(), &bytes[0], len, nullptr, nullptr);
	if (len == 0)
		throw std::exception("IZUnicodeAvisynth: WideCharToMultiByte() failed");

	len = MultiByteToWideChar(tocode, 0, &bytes[0], bytes.size(), nullptr, 0);
	if (len == 0)
		throw std::exception("IZUnicodeAvisynth: MultiByteToWideChar() failed");

	std::wstring result;
	result.resize(len);

	len = MultiByteToWideChar(tocode, 0, &bytes[0], bytes.size(), &result[0], len);
	if (len == 0)
		throw std::exception("IZUnicodeAvisynth: MultiByteToWideChar() failed");

	return result;
}

typedef HANDLE (WINAPI *TCreateFileW)(LPCWSTR, DWORD, DWORD, LPSECURITY_ATTRIBUTES, DWORD, DWORD, HANDLE);
typedef HANDLE (WINAPI *TCreateFileA)(LPCSTR, DWORD, DWORD, LPSECURITY_ATTRIBUTES, DWORD, DWORD, HANDLE);
typedef HANDLE (WINAPI *TCreateFile2)(LPCWSTR, DWORD, DWORD, DWORD, LPCREATEFILE2_EXTENDED_PARAMETERS);
typedef DWORD (WINAPI *TSearchPathW)(LPCWSTR, LPCWSTR, LPCWSTR, DWORD, LPWSTR, LPWSTR*);
typedef DWORD (WINAPI *TGetFullPathNameW)(LPCWSTR, DWORD, LPWSTR, LPWSTR*);

TCreateFileW fpCreateFileW;
TCreateFileA fpCreateFileA;
TCreateFile2 fpCreateFile2;
TSearchPathW fpSearchPathW;
TGetFullPathNameW fpGetFullPathNameW;

DWORD WINAPI DetourGetFullPathNameW(LPCWSTR lpFileName, DWORD nBufferLength, LPWSTR lpBuffer, LPWSTR* lpFilePart)
{
	auto result = fpGetFullPathNameW(lpFileName, nBufferLength, lpBuffer, lpFilePart);

	for (const auto& cp : cps)
	{
		if (result != 0)
			break; // Success!

		auto lpwFileName = lpFileName == nullptr ? std::wstring() : Convert(std::wstring(lpFileName), cp, CP_UTF8);
		result = fpGetFullPathNameW(lpwFileName.empty() ? nullptr : lpwFileName.c_str(), nBufferLength, lpBuffer,
		                            lpFilePart);
	}

	return result;
}

void XTrace(const char* lpszFormat, ...)
{
	va_list args;
	va_start(args, lpszFormat);
	int nBuf;

	char szBuffer[512]; // get rid of this hard-coded buffer
	nBuf = _vsnprintf(szBuffer, 511, lpszFormat, args);
	OutputDebugStringA(szBuffer);
	va_end(args);
}

HANDLE WINAPI DetourCreateFileW(LPCWSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode,
                                LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition,
                                DWORD dwFlagsAndAttributes, HANDLE hTemplateFile)
{
	auto result = fpCreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition,
	                            dwFlagsAndAttributes, hTemplateFile);
	for (const auto& cp : cps)
	{
		try
		{
			auto lpwFileName = lpFileName == nullptr ? std::wstring() : Convert(std::wstring(lpFileName), cp, CP_UTF8);

			if (result != INVALID_HANDLE_VALUE)
				break; // Success!

			result = fpCreateFileW(lpwFileName.empty() ? nullptr : lpwFileName.c_str(), dwDesiredAccess, dwShareMode,
			                       lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
		}
		catch (std::exception e)
		{
		}
	}
	return result;
}

HANDLE WINAPI DetourCreateFileA(LPCSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode,
                                LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition,
                                DWORD dwFlagsAndAttributes, HANDLE hTemplateFile)
{
	return DetourCreateFileW(GetWC(lpFileName), dwDesiredAccess, dwShareMode, lpSecurityAttributes,
	                         dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
}

HANDLE WINAPI DetourCreateFile2(LPCWSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode,
                                DWORD dwCreationDisposition, LPCREATEFILE2_EXTENDED_PARAMETERS pCreateExParams)
{
	auto result = fpCreateFile2(lpFileName, dwDesiredAccess, dwShareMode, dwCreationDisposition, pCreateExParams);
	for (const auto& cp : cps)
	{
		try
		{
			if (result != INVALID_HANDLE_VALUE)
				break; // Success!

			auto lpwFileName = lpFileName == nullptr ? std::wstring() : Convert(std::wstring(lpFileName), cp, CP_UTF8);
			result = fpCreateFile2(lpwFileName.empty() ? nullptr : lpwFileName.c_str(), dwDesiredAccess, dwShareMode,
			                       dwCreationDisposition, pCreateExParams);
		}
		catch (std::exception e)
		{
		}
	}
	return result;
}

DWORD WINAPI DetourSearchPathW(LPCWSTR lpPath, LPCWSTR lpFileName, LPCWSTR lpExtension, DWORD nBufferLength,
                               LPWSTR lpBuffer, LPWSTR* lpFilePart)
{
	auto result = fpSearchPathW(lpPath, lpFileName, lpExtension, nBufferLength, lpBuffer, lpFilePart);

	for (const auto& cp : cps)
	{
		try
		{
			if (result != 0)
				break; // Success!

			auto lpwPath = lpPath == nullptr ? std::wstring() : Convert(std::wstring(lpPath), cp, CP_UTF8);
			auto lpwFileName = lpFileName == nullptr ? std::wstring() : Convert(std::wstring(lpFileName), cp, CP_UTF8);
			result = fpSearchPathW(lpwPath.empty() ? nullptr : lpwPath.c_str(),
			                       lpwFileName.empty() ? nullptr : lpwFileName.c_str(), lpExtension, nBufferLength,
			                       lpBuffer, lpFilePart);
		}
		catch (std::exception e)
		{
		}
	}

	return result;
}

void __cdecl Detach(void* user_data, IScriptEnvironment* env)
{
	MH_DisableHook(nullptr);
	MH_RemoveHook(nullptr);
	MH_Uninitialize();

	cps.clear();
}

typedef long NTSTATUS;

typedef struct _IO_STATUS_BLOCK
{
	union
	{
		NTSTATUS Status;
		PVOID Pointer;
	} DUMMYUNIONNAME;

	ULONG_PTR Information;
} IO_STATUS_BLOCK, *PIO_STATUS_BLOCK;

typedef struct _UNICODE_STRING
{
	USHORT Length;
	USHORT MaximumLength;
	PWSTR Buffer;
} UNICODE_STRING, *PUNICODE_STRING;

typedef struct _OBJECT_ATTRIBUTES
{
	ULONG Length;
	HANDLE RootDirectory;
	PUNICODE_STRING ObjectName;
	ULONG Attributes;
	PVOID SecurityDescriptor;
	PVOID SecurityQualityOfService;
} OBJECT_ATTRIBUTES, *POBJECT_ATTRIBUTES;

typedef NTSTATUS (__stdcall *_NtCreateFile)(
	PHANDLE FileHandle,
	ACCESS_MASK DesiredAccess,
	POBJECT_ATTRIBUTES ObjectAttributes,
	PIO_STATUS_BLOCK IoStatusBlock,
	PLARGE_INTEGER AllocationSize,
	ULONG FileAttributes,
	ULONG ShareAccess,
	ULONG CreateDisposition,
	ULONG CreateOptions,
	PVOID EaBuffer,
	ULONG EaLength
);

typedef VOID (__stdcall *_RtlInitUnicodeString)(
	PUNICODE_STRING DestinationString,
	PCWSTR SourceString
);

_RtlInitUnicodeString RtlInitUnicodeString = reinterpret_cast<_RtlInitUnicodeString>(GetProcAddress(
	GetModuleHandle(L"ntdll.dll"), "RtlInitUnicodeString"));

_NtCreateFile fpNtCreateFile;

NTSTATUS DetourNtCreateFile(PHANDLE FileHandle,
                            ACCESS_MASK DesiredAccess,
                            POBJECT_ATTRIBUTES ObjectAttributes,
                            PIO_STATUS_BLOCK IoStatusBlock,
                            PLARGE_INTEGER AllocationSize,
                            ULONG FileAttributes,
                            ULONG ShareAccess,
                            ULONG CreateDisposition,
                            ULONG CreateOptions,
                            PVOID EaBuffer,
                            ULONG EaLength)
{
	auto result = fpNtCreateFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock, AllocationSize,
	                             FileAttributes, ShareAccess, CreateDisposition, CreateOptions, EaBuffer, EaLength);
	for (const auto& cp : cps)
	{
		if (FileHandle != nullptr)
			break; // Success!

		try
		{
			auto lpw_path = ObjectAttributes->ObjectName->Buffer == nullptr
				                ? std::wstring()
				                : Convert(std::wstring(ObjectAttributes->ObjectName->Buffer,
				                                       ObjectAttributes->ObjectName->Length / sizeof(wchar_t)), cp,
				                          CP_UTF8);
			auto attributes = *ObjectAttributes;
			UNICODE_STRING new_string;
			RtlInitUnicodeString(&new_string, lpw_path.c_str());
			attributes.ObjectName = &new_string;

			result = fpNtCreateFile(FileHandle, DesiredAccess, &attributes, IoStatusBlock, AllocationSize,
			                        FileAttributes, ShareAccess, CreateDisposition, CreateOptions, EaBuffer, EaLength);
		}
		catch (std::exception)
		{
		}
	}
	return result;
}

extern "C" __declspec(dllexport) const char* __stdcall AvisynthPluginInit3(
	IScriptEnvironment* Env, AVS_Linkage* const vectors)
{
	AVS_linkage = vectors;

	MH_Initialize();
	setlocale(LC_ALL, "");

	cps.push_back(GetACP()); // Default
	cps.push_back(874); // ISO 8859-11 (Thai/Win)
	cps.push_back(65001); // UTF8 / Unicode

	cps.push_back(37);
	cps.push_back(437);
	cps.push_back(500);
	cps.push_back(708);
	cps.push_back(720);
	cps.push_back(737);
	cps.push_back(775);
	cps.push_back(850);
	cps.push_back(852);
	cps.push_back(855);
	cps.push_back(857);
	cps.push_back(858);
	cps.push_back(860);
	cps.push_back(861);
	cps.push_back(862);
	cps.push_back(863);
	cps.push_back(864);
	cps.push_back(865);
	cps.push_back(866);
	cps.push_back(869);
	cps.push_back(870);
	cps.push_back(875);
	cps.push_back(932);
	cps.push_back(936);
	cps.push_back(949);
	cps.push_back(950);
	cps.push_back(1026);
	cps.push_back(1047);
	cps.push_back(1140);
	cps.push_back(1141);
	cps.push_back(1142);
	cps.push_back(1143);
	cps.push_back(1144);
	cps.push_back(1145);
	cps.push_back(1146);
	cps.push_back(1147);
	cps.push_back(1148);
	cps.push_back(1149);
	cps.push_back(1200);
	cps.push_back(1201);
	cps.push_back(1250);
	cps.push_back(1251);
	cps.push_back(1252);
	cps.push_back(1253);
	cps.push_back(1254);
	cps.push_back(1255);
	cps.push_back(1256);
	cps.push_back(1257);
	cps.push_back(1258);
	cps.push_back(1361);
	cps.push_back(10000);
	cps.push_back(10001);
	cps.push_back(10002);
	cps.push_back(10003);
	cps.push_back(10004);
	cps.push_back(10005);
	cps.push_back(10006);
	cps.push_back(10007);
	cps.push_back(10008);
	cps.push_back(10010);
	cps.push_back(10017);
	cps.push_back(10021);
	cps.push_back(10029);
	cps.push_back(10079);
	cps.push_back(10081);
	cps.push_back(10082);
	cps.push_back(12000);
	cps.push_back(12001);
	cps.push_back(20000);
	cps.push_back(20001);
	cps.push_back(20002);
	cps.push_back(20003);
	cps.push_back(20004);
	cps.push_back(20005);
	cps.push_back(20105);
	cps.push_back(20106);
	cps.push_back(20107);
	cps.push_back(20108);
	cps.push_back(20127);
	cps.push_back(20261);
	cps.push_back(20269);
	cps.push_back(20273);
	cps.push_back(20277);
	cps.push_back(20278);
	cps.push_back(20280);
	cps.push_back(20284);
	cps.push_back(20285);
	cps.push_back(20290);
	cps.push_back(20297);
	cps.push_back(20420);
	cps.push_back(20423);
	cps.push_back(20424);
	cps.push_back(20833);
	cps.push_back(20838);
	cps.push_back(20866);
	cps.push_back(20871);
	cps.push_back(20880);
	cps.push_back(20905);
	cps.push_back(20924);
	cps.push_back(20932);
	cps.push_back(20936);
	cps.push_back(20949);
	cps.push_back(21025);
	cps.push_back(21866);
	cps.push_back(28591);
	cps.push_back(28592);
	cps.push_back(28593);
	cps.push_back(28594);
	cps.push_back(28595);
	cps.push_back(28596);
	cps.push_back(28597);
	cps.push_back(28598);
	cps.push_back(28599);
	cps.push_back(28603);
	cps.push_back(28605);
	cps.push_back(29001);
	cps.push_back(38598);
	cps.push_back(50220);
	cps.push_back(50221);
	cps.push_back(50222);
	cps.push_back(50225);
	cps.push_back(50227);
	cps.push_back(51932);
	cps.push_back(51936);
	cps.push_back(51949);
	cps.push_back(52936);
	cps.push_back(54936);
	cps.push_back(57002);
	cps.push_back(57003);
	cps.push_back(57004);
	cps.push_back(57005);
	cps.push_back(57006);
	cps.push_back(57007);
	cps.push_back(57008);
	cps.push_back(57009);
	cps.push_back(57010);
	cps.push_back(57011);
	cps.push_back(65000);

	MH_CreateHook(&CreateFileW, &DetourCreateFileW, reinterpret_cast<LPVOID*>(&fpCreateFileW));
	MH_CreateHook(&CreateFileA, &DetourCreateFileA, reinterpret_cast<LPVOID*>(&fpCreateFileA));
	//MH_CreateHook(&CreateFile2, &DetourCreateFile2, reinterpret_cast<LPVOID*>(&fpCreateFile2));
	MH_CreateHook(&SearchPathW, &DetourSearchPathW, reinterpret_cast<LPVOID*>(&fpSearchPathW));
	MH_CreateHook(&GetFullPathNameW, &DetourGetFullPathNameW, reinterpret_cast<LPVOID*>(&fpGetFullPathNameW));

	//Low-Level API Hooking
	MH_CreateHookApi(L"ntdll.dll", "NtCreateFile", DetourNtCreateFile, reinterpret_cast<LPVOID*>(&fpNtCreateFile));
	MH_EnableHook(nullptr);

	Env->AtExit(Detach, nullptr);

	return "IZEncoder Unicode Avisynth 1.0";
}
