using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Injector
{
    public static class Injector
    {
        public static bool Inject(Process targetProcess, string fullPathToDLL, System.Windows.Forms.RichTextBox rtxtLog = null)
        {
            Logger.Log.Write("Injector", "Inject", "Injecting '" + fullPathToDLL + "' into " + targetProcess.ProcessName + " (PID: " + targetProcess.Id.ToString() + ")", Logger.ELogType.Info, rtxtLog);
            IntPtr procHandle = WinAPI.OpenProcess((uint)(WinAPI.ProcessAccessFlags.CreateThread | WinAPI.ProcessAccessFlags.QueryInformation | WinAPI.ProcessAccessFlags.VirtualMemoryOperation | WinAPI.ProcessAccessFlags.VirtualMemoryWrite | WinAPI.ProcessAccessFlags.VirtualMemoryRead), 0, (uint)targetProcess.Id);
            if (procHandle == (IntPtr)0)
            {
                Logger.Log.WriteError("Injector", "Inject", "Could not get handle to target process: " + Marshal.GetLastWin32Error().ToString(), rtxtLog);
                WinAPI.CloseHandle(procHandle);
                return false;
            }

            PEHeader kernel32Header = PEHeader.ParseModuleHeader(targetProcess, "kernel32.dll");
            Logger.Log.Write("Injector", "Inject", "Parsed kernel32.dll PE header, scanning EAT now...", Logger.ELogType.Info, rtxtLog);
            IntPtr loadLibraryAddr = kernel32Header.GetAddressOfExportedFunction(targetProcess, "LoadLibraryA");
            if (loadLibraryAddr == (IntPtr)0)
            {
                Logger.Log.WriteError("Injector", "Inject", "Could not find LoadLibrary in process: " + Marshal.GetLastWin32Error().ToString(), rtxtLog);
                WinAPI.CloseHandle(procHandle);
                return false;
            }
            Logger.Log.Write("Injector", "Inject", "Got LoadLibraryA address: " + loadLibraryAddr.ToString("X2"), Logger.ELogType.Info, rtxtLog);

            IntPtr allocMemAddress = WinAPI.VirtualAllocEx(procHandle, (IntPtr)null, (IntPtr)fullPathToDLL.Length + 1, (uint)(WinAPI.AllocationType.Reserve | WinAPI.AllocationType.Commit), (uint)WinAPI.MemoryProtection.ExecuteReadWrite);
            if (allocMemAddress == (IntPtr)0)
            {
                Logger.Log.WriteError("Injector", "Inject", "Could not allocate memory in target process: " + Marshal.GetLastWin32Error().ToString(), rtxtLog);
                WinAPI.CloseHandle(procHandle);
                return false;
            }
            Logger.Log.Write("Injector", "Inject", "Allocated " + ((IntPtr)fullPathToDLL.Length + 1).ToString() + " bytes in target process at address " + allocMemAddress.ToString("X2"), Logger.ELogType.Info, rtxtLog);

            byte[] bytes = Encoding.ASCII.GetBytes(fullPathToDLL);
            IntPtr bytesWritten;
            int written = WinAPI.WriteProcessMemory(procHandle, allocMemAddress, bytes, (uint)bytes.Length, out bytesWritten);
            if ((int)bytesWritten != bytes.Length)
            {
                Logger.Log.WriteError("Injector", "Inject", "Could not write to target process: " + Marshal.GetLastWin32Error().ToString(), rtxtLog);
                WinAPI.VirtualFreeEx(procHandle, allocMemAddress, (IntPtr)0, (uint)(WinAPI.AllocationType.Release));
                WinAPI.CloseHandle(procHandle);
                return false;
            }
            Logger.Log.Write("Injector", "Inject", "Wrote path to DLL to allocated memory in the game", Logger.ELogType.Info, rtxtLog);

            IntPtr ipThread = WinAPI.CreateRemoteThread(procHandle, (IntPtr)null, (IntPtr)0, loadLibraryAddr, allocMemAddress, 0, (IntPtr)null);
            if (ipThread == (IntPtr)0)
            {
                Logger.Log.WriteError("Injector", "Inject", "Could not create remote thread in target process: " + Marshal.GetLastWin32Error().ToString(), rtxtLog);
                WinAPI.VirtualFreeEx(procHandle, allocMemAddress, (IntPtr)0, (uint)(WinAPI.AllocationType.Release));
                WinAPI.CloseHandle(procHandle);
                return false;
            }
            Logger.Log.Write("Injector", "Inject", "Created remote thread in game process @ LoadLibraryA with pointer to path to DLL string as parameter (thID: " + ipThread.ToString() +")", Logger.ELogType.Info, rtxtLog);

            // this will wait on the thread that just calls LoadLibrary
            // that thread will exit immediately after LoadLibrary returns (regardless of what the return value is)
            // i think the thread's exit code is actually set to the result of LoadLibrary... strange.
            // either way, this ensures the LoadLibrary call finishes before we free the memory holding our pathToDLL
            uint waitValue = WinAPI.WaitForSingleObject(ipThread, WinAPI.INFINITE);
            if (waitValue == WinAPI.WAIT_OBJECT_0)
            {
                uint exitCode = 50;
                if (!WinAPI.GetExitCodeThread(ipThread, out exitCode))
                {
                    throw new Exception("Could not get exit code from remote thread: " + Marshal.GetLastWin32Error().ToString());
                }
                if (exitCode == 0)
                {
                    throw new Exception("Could not load bootstrapper into target process. No further details available.");
                }
            }

            WinAPI.VirtualFreeEx(procHandle, allocMemAddress, (IntPtr)0, (uint)(WinAPI.AllocationType.Release));
            WinAPI.CloseHandle(ipThread);
            WinAPI.CloseHandle(procHandle);
            return true;
        }
    }
}
