#pragma once

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <string>

void PrintErrorMessage(HRESULT error);
VOID Eject(PCHAR, UINT);
BOOL Utf8ToUtf16(const std::string& utf8, OUT std::wstring& outWStr);
BOOL LoadConfig();
VOID Inject();

constexpr DWORD kFlags = MB_ERR_INVALID_CHARS;