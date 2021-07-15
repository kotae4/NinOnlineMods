#include "pch.h"
#include "main.h"

LPCSTR CONFIG_FILE_PATH = "net_bootstrapper_config.txt";
HMODULE hModSelf;
std::vector<std::string> config;


// terraria:
// LPCWSTR TargetRuntimeVersion = L"v4.0.30319";
// nin online:
/*
LPCWSTR TargetRuntimeVersion = L"v4.0.30319";
LPCWSTR InjectedLibraryPath = L"NinMods.dll";
LPCWSTR InjectedLibraryFullTypeName = L"InjectedLibrary.InjectedClass";
// Method must be implemented like so:
// static int pwzMethodName (string pwzArgument)
LPCWSTR InjectedLibraryMethod = L"InjectedEntryPoint";
*/
LPCWSTR InjectedLibraryPassedStringArgument = L"Bootstrap";
DWORD InjectedLibraryResult = -1;

void PrintErrorMessage(HRESULT error)
{
	LPTSTR errorText = NULL;

	FormatMessage(
		// use system message tables to retrieve error text
		FORMAT_MESSAGE_FROM_SYSTEM
		// allocate buffer on local heap for error text
		| FORMAT_MESSAGE_ALLOCATE_BUFFER
		// Important! will fail otherwise, since we're not 
		// (and CANNOT) pass insertion parameters
		| FORMAT_MESSAGE_IGNORE_INSERTS,
		NULL,    // unused with FORMAT_MESSAGE_FROM_SYSTEM
		error,
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		(LPTSTR)&errorText,  // output 
		0, // minimum size for output buffer
		NULL);   // arguments - see note 

	if (errorText != NULL)
	{
		// ... do something with the string `errorText` - log it, display it to the user, etc.
		_tprintf(TEXT("[Bootstrapper] Error code (0x%lx): %s"), error, errorText);
		// release memory allocated by FormatMessage()
		LocalFree(errorText);
		errorText = NULL;
	}
}

VOID Eject(DWORD exitCode = 0, LPCSTR reason = NULL, UINT icon = MB_ICONINFORMATION)
{
	if (reason)
	{
		MessageBoxA(NULL, reason, "NET Injector", MB_OK | icon | MB_TOPMOST);
	}
	if (!DeleteFileA(CONFIG_FILE_PATH))
	{
		PrintErrorMessage(GetLastError());
	}
	FreeLibraryAndExitThread(hModSelf, exitCode);
}

void RegisterConsole()
{
	if (AllocConsole() == FALSE)
	{
		// process already has a console, so we need to manually hook up our stdin/stdout/stderr to the console (well, we're only interested in stdout for now)
		MessageBoxA(NULL, "Process already has console, hooking up stdin/out/err", "Mono Injector", MB_OK | MB_ICONWARNING | MB_TOPMOST);
	}
	// if AllocConsole returns true, then
	// the process did NOT have a console, and so the function call did two things: 
	// 1. created a new console associated with the process
	// 2. initialized standard i/o/e streams
	// BUT! we need to re-open our standard streams to point to the new console streams:
	// ah, i finally figured out what the first arg does: if the function succeeds, it's identical to the fourth arg. if it fails, it'll be null or something else.
	// i was confused for so long because it's weird to have an OUT parameter as the first parameter
	FILE* fDummy = nullptr;
	freopen_s(&fDummy, "CONOUT$", "w", stderr);
	freopen_s(&fDummy, "CONOUT$", "w", stdout);
}

// credit: https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/september/c-unicode-encoding-conversions-with-stl-strings-and-win32-apis
BOOL Utf8ToUtf16(const std::string& utf8, OUT std::wstring& outWStr)
{
	if (utf8.empty())
	{
		return FALSE;
	}
	const int utf8Length = static_cast<int>(utf8.length());
	const int utf16Length = ::MultiByteToWideChar(
		CP_UTF8,       // Source string is in UTF-8
		kFlags,        // Conversion flags
		utf8.data(),   // Source UTF-8 string pointer
		utf8Length,    // Length of the source UTF-8 string, in chars
		nullptr,       // Unused - no conversion done in this step
		0              // Request size of destination buffer, in wchar_ts
	);

	if (utf16Length == 0)
	{
		PrintErrorMessage(GetLastError());
		return FALSE;
	}
	outWStr.resize(utf16Length);

	int result = ::MultiByteToWideChar(
		CP_UTF8,       // Source string is in UTF-8
		kFlags,        // Conversion flags
		utf8.data(),   // Source UTF-8 string pointer
		utf8Length,    // Length of source UTF-8 string, in chars
		&outWStr[0],     // Pointer to destination buffer
		utf16Length    // Size of destination buffer, in wchar_ts          
	);
	if (result == 0)
	{
		PrintErrorMessage(GetLastError());
		return FALSE;
	}
	return TRUE;
}

BOOL LoadConfig()
{
	std::ifstream myfile(CONFIG_FILE_PATH);
	if (myfile.is_open())
	{
		// [unsafe] assumptions:
		// line 1: runtime_version [v4.0.30319]
		// line 2: path/to/hack.dll
		// line 3: full type name in hack.dll [MyCompany.MyProject.InjectedLibrary.InjectedClass]
		// line 4: method to act as entrypoint [MyHackEntrypoint]
		// NOTE:
		// should really read this into a CONFIG struct for extra neatness, but vector of strings is fine for now.
		std::string line;
		int lineCount = 0;
		while (std::getline(myfile, line))
		{
			config.push_back(line);
			printf("Read config setting: '%s'\n", line.c_str());
			lineCount++;
		}
		myfile.close();
	}
	else
	{
		return FALSE;
	}
	if (config.size() < 4)
		return FALSE;

	return TRUE;
}

VOID Inject()
{
	//RegisterConsole();

	printf("[Bootstrapper] Injected, loading managed DLL now...\n");
	if (LoadConfig() == FALSE)
	{
		Eject(504, "Could not load settings from config file");
		return;
	}
	ICLRMetaHost* pMetaHost = NULL;
	ICLRRuntimeInfo* pRuntimeInfo = NULL;
	ICLRRuntimeHost* pRuntimeHost = NULL;
	HRESULT hr;

	BOOL configParseSuccess = TRUE;
	std::wstring runtime_version, dll_path, injectTypename, injectEntrypointMethod;
	BOOL convResult = Utf8ToUtf16(config.at(0), runtime_version);
	if (convResult == FALSE)
	{
		printf("Could not convert runtime_version from config\n");
		configParseSuccess = FALSE;
	}
	else
		printf("config.runtime_version: %S\n", runtime_version.c_str());

	convResult = Utf8ToUtf16(config.at(1), dll_path);
	if (convResult == FALSE)
	{
		printf("Could not convert dll_path from config\n");
		configParseSuccess = FALSE;
	}
	else
		printf("config.dll_path: %S\n", dll_path.c_str());

	convResult = Utf8ToUtf16(config.at(2), injectTypename);
	if (convResult == FALSE)
	{
		printf("Could not convert injectTypename from config\n");
		configParseSuccess = FALSE;
	}
	else
		printf("config.injectTypename: %S\n", injectTypename.c_str());

	convResult = Utf8ToUtf16(config.at(3), injectEntrypointMethod);
	if (convResult == FALSE)
	{
		printf("Could not convert injectEntrypointMethod from config\n");
		configParseSuccess = FALSE;
	}
	else
		printf("config.injectEntrypointMethod: %S\n", injectEntrypointMethod.c_str());

	if (configParseSuccess == FALSE)
	{
		Eject(505, "Could not convert config strings to wchar_t");
		return;
	}

	hr = CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, (LPVOID*)&pMetaHost);
	if (hr != S_OK)
	{
		PrintErrorMessage(hr);
		Eject(503, "Could not get/create CLR metahost instance");
		return;
	}

	printf("[Bootstrapper] Got MetaHost [%tx]\n", pMetaHost);

	hr = pMetaHost->GetRuntime(runtime_version.c_str(), IID_ICLRRuntimeInfo, (LPVOID*)&pRuntimeInfo);
	if (hr != S_OK)
	{
		PrintErrorMessage(hr);
		Eject(502, "Could not get runtime instance");
		return;
	}

	printf("[Bootstrapper] Got RuntimeInfo [%tx]\n", pRuntimeInfo);

	hr = pRuntimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, (LPVOID*)&pRuntimeHost);
	if (hr != S_OK)
	{
		PrintErrorMessage(hr);
		Eject(501, "Could not get runtime interface");
		return;
	}

	printf("[Bootstrapper] Got RuntimeHost [%tx]\n", pRuntimeHost);

	printf("[Bootstrapper] Prepared the CLR, executing managed DLL now\n");

	hr = pRuntimeHost->ExecuteInDefaultAppDomain(dll_path.c_str(), injectTypename.c_str(), injectEntrypointMethod.c_str(), InjectedLibraryPassedStringArgument, &InjectedLibraryResult);
	printf("[Bootstrapper] executed\n");
	if (hr != S_OK)
	{
		if (hr == HOST_E_CLRNOTAVAILABLE)
		{
			printf("[Bootstrapper] CLR was in bad state, could not execute (%lx)\n", hr);
		}
		else if (hr == HOST_E_TIMEOUT)
		{
			printf("[Bootstrapper] execution timed out (%lx)\n", hr);
		}
		else if (hr == HOST_E_NOT_OWNER)
		{
			printf("[Bootstrapper] CLR owned by someone else, could not execute (%lx)\n", hr);
		}
		else if (hr == HOST_E_ABANDONED)
		{
			printf("[Bootstrapper] An event was canceled while a blocked thread or fiber was waiting on it. (%lx)\n", hr);
		}
		else if (hr == E_FAIL)
		{
			printf("[Bootstrapper][CRITICAL] An unknown catastrophic failure occurred. (%lx)\n", hr);
		}
		PrintErrorMessage(hr);
		Eject(500, "Could not execute injected DLL's entrypoint");
		return;
	}

	printf("[Bootstrapper] done, exiting...\n");
	Eject(1);
	/*
	if (pRuntimeHost != NULL)
	{
		pRuntimeHost->Stop();
		pRuntimeHost->Release();
	}
	if (pRuntimeInfo != NULL)
		pRuntimeInfo->Release();
	if (pMetaHost != NULL)
		pMetaHost->Release();
	*/
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
	hModSelf = hModule;
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		CreateThread(0, 0, (LPTHREAD_START_ROUTINE)Inject, 0, 0, 0);
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}