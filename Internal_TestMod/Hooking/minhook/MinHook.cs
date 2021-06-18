using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;

using NinMods.Hooking.NativeImports;
using NinMods.Hooking.Utilities;

namespace NinMods.Hooking.LowLevel
{
    public class MinHook
    {
        public enum MH_STATUS
        {
            // Unknown error. Should not be returned.
            MH_UNKNOWN = -1,

            // Successful.
            MH_OK = 0,

            // MinHook is already initialized.
            MH_ERROR_ALREADY_INITIALIZED,

            // MinHook is not initialized yet, or already uninitialized.
            MH_ERROR_NOT_INITIALIZED,

            // The hook for the specified target function is already created.
            MH_ERROR_ALREADY_CREATED,

            // The hook for the specified target function is not created yet.
            MH_ERROR_NOT_CREATED,

            // The hook for the specified target function is already enabled.
            MH_ERROR_ENABLED,

            // The hook for the specified target function is not enabled yet, or already
            // disabled.
            MH_ERROR_DISABLED,

            // The specified pointer is invalid. It points the address of non-allocated
            // and/or non-executable region.
            MH_ERROR_NOT_EXECUTABLE,

            // The specified target function cannot be hooked.
            MH_ERROR_UNSUPPORTED_FUNCTION,

            // Failed to allocate memory.
            MH_ERROR_MEMORY_ALLOC,

            // Failed to change the memory protection.
            MH_ERROR_MEMORY_PROTECT,

            // The specified module is not loaded.
            MH_ERROR_MODULE_NOT_FOUND,

            // The specified function is not found.
            MH_ERROR_FUNCTION_NOT_FOUND
        };

        class HookEntry
        {
            public IntPtr targetFunc, detourFunc, trampFunc;
            public bool patchAbove = false;
            public bool isEnabled = false;

            public uint nIPs = 0;
            // target func
            public byte[] oldIPs = new byte[8];
            // trampoline func
            public byte[] newIPs = new byte[8];

            // Original prologue of the target function.
            public byte[] backup = new byte[8];
        }

        static List<HookEntry> g_Hooks = new List<HookEntry>();

        static object g_Lock = 0;

        static readonly bool Is64Bit = (IntPtr.Size == 8);
        static readonly bool IsMono = (System.Type.GetType("Mono.Runtime") != null);

        static object g_ThreadIDLock = 0;
        static List<int> g_ThreadIDs = new List<int>();

        public static void NotifyThreadChange(int threadID, bool attached)
        {
            Logger.Log.Write("Minhook", "NotifyThreadChange", "Saw thread#" + threadID.ToString() + (attached ? " attach" : " detach"));
            lock (g_ThreadIDLock)
            {
                if (threadID == AppDomain.GetCurrentThreadId())
                {
                    if (g_ThreadIDs.Contains(threadID))
                        g_ThreadIDs.Remove(threadID);
                    return;
                }

                if (g_ThreadIDs.Count == 0)
                {
                    g_ThreadIDs = EnumerateThreads_NtProcessManager();
                }
                else
                {
                    if ((attached) && (!g_ThreadIDs.Contains(threadID)))
                        g_ThreadIDs.Add(threadID);
                    else if ((!attached) && (g_ThreadIDs.Contains(threadID)))
                        g_ThreadIDs.Remove(threadID);
                }
            }
        }

        /// <summary>
        /// Frees unmanaged allocated memory used by MinHook
        /// </summary>
        /// <returns></returns>
        public static bool Uninitialize()
        {
            // TO-DO:
            // proper error handling
            lock (g_Lock)
            {
                DisableAllHooks();
                MemoryBuffer.UninitializeBuffer();
            }
            return true;
        }

        static HookEntry FindHook(IntPtr targetFunc)
        {
            foreach (HookEntry hook in g_Hooks)
                if (hook.targetFunc == targetFunc)
                    return hook;
            return null;
        }

        //-------------------------------------------------------------------------
        static unsafe ulong FindOldIP(HookEntry pHook, ulong ip)
        {
            uint i;

            if (pHook.patchAbove && ip == ((ulong)pHook.targetFunc - (ulong)sizeof(JMP_CALL_REL)))
                return (ulong)pHook.targetFunc;

            for (i = 0; i < pHook.nIPs; ++i)
            {
                if (ip == ((ulong)pHook.trampFunc + pHook.newIPs[i]))
                    return (ulong)pHook.targetFunc + pHook.oldIPs[i];
            }

            if (Is64Bit)
            {
                // Check relay function.
                if (ip == (ulong)pHook.detourFunc)
                    return (ulong)pHook.targetFunc;
            }

            return 0;
        }

        //-------------------------------------------------------------------------
        static unsafe ulong FindNewIP(HookEntry pHook, ulong ip)
        {
            uint i;
            for (i = 0; i < pHook.nIPs; ++i)
            {
                if (ip == ((ulong)pHook.targetFunc + pHook.oldIPs[i]))
                    return (ulong)pHook.trampFunc + pHook.newIPs[i];
            }

            return 0;
        }

        static unsafe void ProcessThreadIPs64(IntPtr hThread, HookEntry targetHook, bool enable, bool allHooks = false)
        {
            // If the thread suspended in the overwritten area,
            // move IP to the proper address.
            if ((!allHooks) && (targetHook.isEnabled == enable))
                return;
            CONTEXT64 c = new CONTEXT64();
            c.ContextFlags = (uint)CONTEXT_FLAGS.CONTEXT_CONTROL;

            if (!NativeImport.GetThreadContext64(hThread, ref c))
                return;
            ulong* pIP = &c.Rip;

            ulong ip = 0;
            if (!allHooks)
            {
                if (enable)
                    ip = FindNewIP(targetHook, *pIP);
                else
                    ip = FindOldIP(targetHook, *pIP);
                if (ip != 0)
                {
                    *pIP = (ulong)ip;
                    NativeImport.SetThreadContext64(hThread, ref c);
                }
            }
            else
            {
                foreach (HookEntry hook in g_Hooks)
                {
                    if (hook.isEnabled == enable)
                        continue;
                    if (enable)
                        ip = FindNewIP(hook, *pIP);
                    else
                        ip = FindOldIP(hook, *pIP);
                    if (ip != 0)
                    {
                        *pIP = (ulong)ip;
                        NativeImport.SetThreadContext64(hThread, ref c);
                    }
                }
            }
        }

        //-------------------------------------------------------------------------
        static unsafe void ProcessThreadIPs32(IntPtr hThread, HookEntry targetHook, bool enable, bool allHooks = false)
        {
            // If the thread suspended in the overwritten area,
            // move IP to the proper address.
            if ((!allHooks) && (targetHook.isEnabled == enable))
                return;
            CONTEXT c = new CONTEXT();
            c.ContextFlags = (uint)CONTEXT_FLAGS.CONTEXT_CONTROL;

            if (!NativeImport.GetThreadContext32(hThread, ref c))
                return;
            uint* pIP = &c.Eip;

            ulong ip = 0;
            if (!allHooks)
            {
                if (enable)
                    ip = FindNewIP(targetHook, *pIP);
                else
                    ip = FindOldIP(targetHook, *pIP);
                if (ip != 0)
                {
                    *pIP = (uint)ip;
                    NativeImport.SetThreadContext32(hThread, ref c);
                }
            }
            else
            {
                foreach (HookEntry hook in g_Hooks)
                {
                    if (hook.isEnabled == enable)
                        continue;
                    if (enable)
                        ip = FindNewIP(hook, *pIP);
                    else
                        ip = FindOldIP(hook, *pIP);
                    if (ip != 0)
                    {
                        *pIP = (uint)ip;
                        NativeImport.SetThreadContext32(hThread, ref c);
                    }
                }
            }
        }

        // credits: http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/whidbey/netfxsp/ndp/fx/src/Services/Monitoring/system/Diagnosticts/ProcessManager@cs/2/ProcessManager@cs
        public static unsafe List<int> EnumerateThreads_NtProcessManager()
        {
#if VERBOSE_PROFILING
            System.DateTime before = System.DateTime.Now;
#endif
            List<int> threadIDs = new List<int>();
            NtStatus status;
            int processID = Process.GetCurrentProcess().Id, curThreadID = AppDomain.GetCurrentThreadId();
            uint dataLength = 0x10000;

            GCHandle bufferHandle = new GCHandle();

            // Query the system processes. If the call fails because of a length mismatch, recreate a bigger buffer and try again.
            try
            {
                do
                {
                    Byte[] buffer = new Byte[dataLength];
                    bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                    status = NativeImport.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessInformation, bufferHandle.AddrOfPinnedObject(), dataLength, out dataLength);
                    if (status == NtStatus.InfoLengthMismatch)
                    {
                        // The length of the buffer was not sufficient. Expand the buffer before retrying.
                        if (bufferHandle.IsAllocated) bufferHandle.Free();
                        dataLength *= 2;
                    }
                }
                while (status == NtStatus.InfoLengthMismatch);
                if (status == NtStatus.Success)
                {
                    long totalOffset = 0;
                    IntPtr bufferAddr = bufferHandle.AddrOfPinnedObject();
                    IntPtr currentPtr;
                    SystemProcessInformation pi = null;
                    SystemThreadInformation ti = null;
                    do
                    {
                        currentPtr = (IntPtr)((long)bufferAddr + totalOffset);
                        pi = new SystemProcessInformation();
                        Marshal.PtrToStructure(currentPtr, pi);

                        //Logger.Log.Write("Saw process w/ ID " + pi.UniqueProcessId.ToInt64().ToString());

                        if (pi.UniqueProcessId.ToInt32() == processID)
                        {
                            //Logger.Log.Write("Found current process, # of threads: " + pi.NumberOfThreads.ToString());

                            currentPtr = (IntPtr)((long)currentPtr + Marshal.SizeOf(pi));
                            for (int threadIndex = 0; threadIndex < pi.NumberOfThreads; threadIndex++)
                            {
                                ti = new SystemThreadInformation();
                                Marshal.PtrToStructure(currentPtr, ti);

                                int threadID = (int)ti.UniqueThread;
                                if (threadID != curThreadID)
                                    threadIDs.Add(threadID);
                                //Logger.Log.Write("Saw thread ID: " + threadID.ToString());

                                currentPtr = (IntPtr)((long)currentPtr + Marshal.SizeOf(ti));
                            }
                        }

                        totalOffset += pi.NextEntryOffset;
                    }
                    while ((pi != null) && (pi.NextEntryOffset != 0));
                }
            }
            finally
            {
                if (bufferHandle.IsAllocated) bufferHandle.Free();
            }
#if VERBOSE_PROFILING
            System.DateTime after = System.DateTime.Now;
            System.TimeSpan span = after - before;
            Logger.Log.Write("[MinHook][NtProcessManager] Enumerated threads in " + span.TotalMilliseconds.ToString() + " ms");
#endif
            return threadIDs;
        }

        //TO-DO:
        // credits: evolution536 via https://www.unknowncheats.me/forum/1490125-post5.html?s=90961d05f5d0c963c2e63eb37cdf3f7f
        public static unsafe List<int> EnumerateThreads_CrySearch()
        {
#if VERBOSE_PROFILING
            System.DateTime before = System.DateTime.Now;
#endif
            List<int> threadIDs = new List<int>();
            NtStatus status;
            int processID = Process.GetCurrentProcess().Id, curThreadID = AppDomain.GetCurrentThreadId();
            uint dataLength = 0x10000;
            IntPtr procInfoBuffer;

            // Query the system processes. If the call fails because of a length mismatch, recreate a bigger buffer and try again.
            do
            {
                procInfoBuffer = NativeImport.VirtualAlloc(IntPtr.Zero, (IntPtr)dataLength, AllocationType.Commit, AllocationProtect.PAGE_READWRITE);
                status = NativeImport.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessInformation, procInfoBuffer, dataLength, out dataLength);
                if (status == NtStatus.InfoLengthMismatch)
                {
                    // The length of the buffer was not sufficient. Expand the buffer before retrying.
                    NativeImport.VirtualFree(procInfoBuffer, IntPtr.Zero, FreeType.Release);
                    dataLength *= 2;
                }
            }
            while (status == NtStatus.InfoLengthMismatch);

            if (status == NtStatus.Success)
            {
                //Logger.Log.Write("Successfully retrieved SystemProcessInformation (buffer addr: " + procInfoBuffer.ToString("X") + ")(size: " + dataLength.ToString() + "), enumerating processes now");
                long totalOffset = 0;
                IntPtr currentPtr;
                s_SystemProcessInformation curProc;
                do
                {
                    currentPtr = (IntPtr)((long)procInfoBuffer + totalOffset);
                    curProc = *((s_SystemProcessInformation*)currentPtr.ToPointer());

                    int curProcID = curProc.UniqueProcessId.ToInt32();
                    //Logger.Log.Write("Saw process w/ ID " + curProcID.ToString());
                    // Iterate processes until the correct one is found.
                    if (curProcID == processID)
                    {
                        //Logger.Log.Write("Found current process, # of threads: " + curProc.NumberOfThreads.ToString());
                        // Iterate threads of process.
                        currentPtr = (IntPtr)((long)currentPtr + sizeof(s_SystemProcessInformation));
                        s_SystemThreadInformation curThread;
                        for (int threadIndex = 0; threadIndex < curProc.NumberOfThreads; threadIndex++)
                        {
                            curThread = *((s_SystemThreadInformation*)currentPtr.ToPointer());

                            int threadID = (int)curThread.UniqueThread;
                            if (threadID != curThreadID)
                                threadIDs.Add(threadID);

                            //Logger.Log.Write("Saw thread ID: " + threadID.ToString());
                            //newEntry.IsSuspended = curThread->ThreadInfo.WaitReason == Suspended;
                            currentPtr = (IntPtr)((long)currentPtr + sizeof(s_SystemThreadInformation));
                        }
                    }

                    totalOffset += curProc.NextEntryOffset;
                }
                while (curProc.NextEntryOffset != 0);
            }

            // Free heap allocated process information.
            NativeImport.VirtualFree(procInfoBuffer, IntPtr.Zero, NativeImports.FreeType.Release);
#if VERBOSE_PROFILING
            System.DateTime after = System.DateTime.Now;
            System.TimeSpan span = after - before;
            Logger.Log.Write("[MinHook][CrySearch] Enumerated threads in " + span.TotalMilliseconds.ToString() + " ms");
#endif

            return threadIDs;
        }

        static unsafe List<int> EnumerateThreads()
        {
#if VERBOSE_PROFILING
            System.DateTime before = System.DateTime.Now;
#endif
            List<int> retVal = new List<int>();
            Process curProc = Process.GetCurrentProcess();
            curProc.Refresh();
            uint curProcID = (uint)curProc.Id, curThreadID = (uint)AppDomain.GetCurrentThreadId();
            IntPtr hSnapshot = NativeImport.CreateToolhelp32Snapshot(SnapshotFlags.Thread, 0);
            // is there no way to check if hSnapshot is valid? if the call fails it returns ((HANDLE)(LONG_PTR)-1)
            // but i don't think that can be cast to IntPtr, lol.
            // oh well!
            THREADENTRY32 te = new NativeImports.THREADENTRY32();
            te.dwSize = (uint)sizeof(THREADENTRY32);
            if (NativeImport.Thread32First(hSnapshot, ref te))
            {
                do
                {
                    if ((te.th32OwnerProcessID == curProcID) && (te.th32ThreadID != curThreadID))
                    {
                        retVal.Add((int)te.th32ThreadID);
                    }
                    te.dwSize = (uint)sizeof(THREADENTRY32);
                } while (NativeImport.Thread32Next(hSnapshot, out te));
            }
            NativeImport.CloseHandle(hSnapshot);
#if VERBOSE_PROFILING
            System.DateTime after = System.DateTime.Now;
            System.TimeSpan span = after - before;
            Logger.Log.Write("[MinHook] Enumerated threads in " + span.TotalMilliseconds.ToString() + " ms");
#endif

            return retVal;
        }

        /// <summary>
        /// .NET CLR only. Do not use when working with mono.
        /// </summary>
        /// <param name="processHook"></param>
        /// <param name="enableHook"></param>
        /// <param name="processAllHooks"></param>
        static void Freeze(HookEntry processHook, bool enableHook, bool processAllHooks = false)
        {
#if VERBOSE_PROFILING
            System.DateTime before = System.DateTime.Now;
#endif
            Process curProc = Process.GetCurrentProcess();
            curProc.Refresh();

            foreach (ProcessThread pT in curProc.Threads)
            {
                // ignore the obsolete warning
                // AppDomain.GetCurrentThreadId returns the OS thread
                // multiple System.Threading.Thread (or managed threads) can share a single OS thread
                // which is why microsoft marks this function as obsolete - to mark a clear boundary between managed and OS threads
                // but we want the OS thread anyway, so this obsolete message doesn't apply to us
                if (pT.Id == AppDomain.GetCurrentThreadId())
                    continue;
                IntPtr pOpenThread = NativeImport.OpenThread((ThreadAccess.SUSPEND_RESUME | ThreadAccess.GET_CONTEXT | ThreadAccess.SET_CONTEXT | ThreadAccess.QUERY_INFORMATION), false, pT.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    // thread does not exist
                    int lastError = Marshal.GetLastWin32Error();
                    throw new Exception("Failed to open thread");
                }
                int retVal = NativeImport.SuspendThread(pOpenThread);
                if (retVal == -1)
                {
                    NativeImport.CloseHandle(pOpenThread);
                    throw new Exception("Failed to suspend thread");
                }
                if (Is64Bit)
                    ProcessThreadIPs64(pOpenThread, processHook, enableHook, processAllHooks);
                else
                    ProcessThreadIPs32(pOpenThread, processHook, enableHook, processAllHooks);
                NativeImport.CloseHandle(pOpenThread);
            }
#if VERBOSE_PROFILING
            System.DateTime after = System.DateTime.Now;
            System.TimeSpan span = after - before;
            Logger.Log.Write("[MinHook] Froze threads in " + span.TotalMilliseconds.ToString() + " ms");
#endif
        }

        /// <summary>
        /// Mono CLR only.
        /// </summary>
        /// <param name="threadIDs">Collection of thread IDs for the process, excluding the current thread.</param>
        /// <param name="processHook"></param>
        /// <param name="enableHook"></param>
        /// <param name="processAllHooks"></param>
        static void Freeze(List<int> threadIDs, HookEntry processHook, bool enableHook, bool processAllHooks = false)
        {
#if VERBOSE_PROFILING
            System.DateTime before = System.DateTime.Now;
#endif
            lock (g_ThreadIDLock)
            {
                int curThreadID = AppDomain.GetCurrentThreadId();
                foreach (int threadID in threadIDs)
                {
                    if (threadID == curThreadID) continue;
                    IntPtr pOpenThread = NativeImport.OpenThread((ThreadAccess.SUSPEND_RESUME | ThreadAccess.GET_CONTEXT | ThreadAccess.SET_CONTEXT | ThreadAccess.QUERY_INFORMATION), false, threadID);
                    if (pOpenThread == IntPtr.Zero)
                    {
                        if (g_ThreadIDs.Contains(threadID))
                            g_ThreadIDs.Remove(threadID);
                        // thread does not exist
                        int lastError = Marshal.GetLastWin32Error();
                        throw new Exception("Failed to open thread#" + threadID.ToString() + " (err#" + lastError.ToString() + ") for suspension");
                    }
                    int retVal = NativeImport.SuspendThread(pOpenThread);
                    if (retVal == -1)
                    {
                        if (g_ThreadIDs.Contains(threadID))
                            g_ThreadIDs.Remove(threadID);
                        NativeImport.CloseHandle(pOpenThread);
                        throw new Exception("Failed to suspend thread#" + threadID.ToString());
                    }
                    if (Is64Bit)
                        ProcessThreadIPs64(pOpenThread, processHook, enableHook, processAllHooks);
                    else
                        ProcessThreadIPs32(pOpenThread, processHook, enableHook, processAllHooks);
                    NativeImport.CloseHandle(pOpenThread);
                }
            }
#if VERBOSE_PROFILING
            System.DateTime after = System.DateTime.Now;
            System.TimeSpan span = after - before;
            Logger.Log.Write("[MinHook] Froze threads in " + span.TotalMilliseconds.ToString() + " ms");
#endif
        }

        /// <summary>
        /// .NET CLR only. Do not use when working with mono.
        /// </summary>
        static void UnFreeze()
        {
#if VERBOSE_PROFILING
            System.DateTime before = System.DateTime.Now;
#endif
            Process curProc = Process.GetCurrentProcess();
            curProc.Refresh();

            foreach (ProcessThread pT in curProc.Threads)
            {
                // ignore the obsolete warning
                // AppDomain.GetCurrentThreadId returns the OS thread
                // multiple System.Threading.Thread (or managed threads) can share a single OS thread
                // which is why microsoft marks this function as obsolete - to mark a clear boundary between managed and OS threads
                // but we want the OS thread anyway, so this obsolete message doesn't apply to us
                if (pT.Id == AppDomain.GetCurrentThreadId())
                    continue;
                IntPtr pOpenThread = NativeImport.OpenThread((ThreadAccess.SUSPEND_RESUME | ThreadAccess.GET_CONTEXT | ThreadAccess.SET_CONTEXT | ThreadAccess.QUERY_INFORMATION), false, pT.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    // thread does not exist
                    int lastError = Marshal.GetLastWin32Error();
                    throw new Exception("Failed to open thread");
                }
                int retVal = NativeImport.ResumeThread(pOpenThread);
                if (retVal == -1)
                {
                    NativeImport.CloseHandle(pOpenThread);
                    throw new Exception("Failed to suspend thread");
                }
                NativeImport.CloseHandle(pOpenThread);
            }
#if VERBOSE_PROFILING
            System.DateTime after = System.DateTime.Now;
            System.TimeSpan span = after - before;
            Logger.Log.Write("[MinHook] Unfroze threads in " + span.TotalMilliseconds.ToString() + " ms");
#endif
        }

        /// <summary>
        /// Mono CLR only.
        /// </summary>
        /// <param name="threadIDs">Collection of thread IDs for the process, excluding the current thread.</param>
        static void UnFreeze(List<int> threadIDs)
        {
#if VERBOSE_PROFILING
            System.DateTime before = System.DateTime.Now;
#endif
            lock (g_ThreadIDLock)
            {
                int curThreadID = AppDomain.GetCurrentThreadId();
                foreach (int threadID in threadIDs)
                {
                    if (threadID == curThreadID) continue;
                    IntPtr pOpenThread = NativeImport.OpenThread((ThreadAccess.SUSPEND_RESUME | ThreadAccess.GET_CONTEXT | ThreadAccess.SET_CONTEXT | ThreadAccess.QUERY_INFORMATION), false, threadID);
                    if (pOpenThread == IntPtr.Zero)
                    {
                        if (g_ThreadIDs.Contains(threadID))
                            g_ThreadIDs.Remove(threadID);
                        // thread does not exist
                        int lastError = Marshal.GetLastWin32Error();
                        throw new Exception("Failed to open thread#" + threadID.ToString() + " (err#" + lastError.ToString() + ") for resume");
                    }
                    int retVal = NativeImport.ResumeThread(pOpenThread);
                    if (retVal == -1)
                    {
                        if (g_ThreadIDs.Contains(threadID))
                            g_ThreadIDs.Remove(threadID);
                        NativeImport.CloseHandle(pOpenThread);
                        throw new Exception("Failed to resume thread#" + threadID.ToString());
                    }
                    NativeImport.CloseHandle(pOpenThread);
                }
            }
#if VERBOSE_PROFILING
            System.DateTime after = System.DateTime.Now;
            System.TimeSpan span = after - before;
            Logger.Log.Write("[MinHook] Unfroze threads in " + span.TotalMilliseconds.ToString() + " ms");
#endif
        }

        /// <summary>
        /// Creates a new hook for target function.
        /// The hook is created but left in a disabled state. Call EnableHook to enable the hook.
        /// </summary>
        /// <param name="origType"></param>
        /// <param name="origFuncName"></param>
        /// <param name="hookType"></param>
        /// <param name="hookFuncName"></param>
        /// <returns></returns>
        public static MH_STATUS CreateHook(IntPtr targetAddr, IntPtr hookAddr, out IntPtr tramp)
        {
            MH_STATUS status = MH_STATUS.MH_OK;
            tramp = IntPtr.Zero;
            Logger.Log.Write("MinHook", "CreateHook", $"Creating hook {targetAddr.ToString("X2")} -> {hookAddr.ToString("X2")}");
            // C#'s lock is the same as MinHook's spin lock right?
            // should work regardless, but might cause a temporary freeze in rare cases
            lock (g_Lock)
            {
                if (FindHook(targetAddr) == null)
                {
                    unsafe
                    {
                        void* pBuffer = MemoryBuffer.AllocateBuffer((void*)targetAddr);
                        if (pBuffer != null)
                        {
                            TRAMPOLINE_S trampoline = new TRAMPOLINE_S();
                            trampoline.pTarget = targetAddr;
                            trampoline.pDetour = hookAddr;
                            trampoline.pTrampoline = (IntPtr)pBuffer;
                            bool hasTrampoline = false;
                            if (Is64Bit)
                                hasTrampoline = Trampoline.CreateTrampolineFunction64(&trampoline);
                            else
                                hasTrampoline = Trampoline.CreateTrampolineFunction32(&trampoline);
                            if (hasTrampoline)
                            {
                                // TO-DO:
                                // assign trampoline to oFuncPtr (so the original function can be called from within the hook)
                                HookEntry newHook = new HookEntry();
                                newHook.targetFunc = trampoline.pTarget;
                                if (Is64Bit)
                                    newHook.detourFunc = trampoline.pRelay;
                                else
                                    newHook.detourFunc = trampoline.pDetour;
                                newHook.trampFunc = trampoline.pTrampoline;
                                newHook.patchAbove = trampoline.patchAbove;
                                newHook.isEnabled = false;
                                newHook.nIPs = trampoline.nIP;
                                // TRANSLATION NOTE:
                                // hardcoded 8
                                Marshal.Copy((IntPtr)trampoline.oldIPs, newHook.oldIPs, 0, 8);
                                Marshal.Copy((IntPtr)trampoline.newIPs, newHook.newIPs, 0, 8);

                                // Back up the target function.

                                if (trampoline.patchAbove)
                                {
                                    fixed (byte* backupPtr = newHook.backup)
                                    {
                                        NativeImport.memcpy(
                                        backupPtr,
                                        (byte*)targetAddr - sizeof(JMP_CALL_REL),
                                        (IntPtr)(sizeof(JMP_CALL_REL) + sizeof(JMP_REL_SHORT)));
                                    }
                                }
                                else
                                {
                                    fixed (byte* backupPtr = newHook.backup)
                                    {
                                        NativeImport.memcpy(backupPtr, (byte*)targetAddr, (IntPtr)sizeof(JMP_CALL_REL));
                                    }
                                }
                                tramp = newHook.trampFunc;
                                g_Hooks.Add(newHook);
                            }
                            else
                            {
                                status = MH_STATUS.MH_ERROR_UNSUPPORTED_FUNCTION;
                            }
                            if (status != MH_STATUS.MH_OK)
                                MemoryBuffer.FreeBuffer(pBuffer);
                        }
                        else
                        {
                            status = MH_STATUS.MH_ERROR_MEMORY_ALLOC;
                        }
                    }
                }
                else
                {
                    status = MH_STATUS.MH_ERROR_ALREADY_CREATED;
                }
            }
            return status;
        }

        /// <summary>
        /// Enables an already created hook.
        /// Does *not* create the hook if it does not already exist.
        /// </summary>
        /// <param name="targetAddr"></param>
        /// <param name="wantToCrash">Skips thread handling when overwriting target method code. This can lead to a crash (immediately or at any point in the future due to corruption).</param>
        /// <returns></returns>
        public static MH_STATUS EnableHook(IntPtr targetAddr, bool wantToCrash = false)
        {
            MH_STATUS status = MH_STATUS.MH_OK;
            HookEntry hook = FindHook(targetAddr);
            if (hook != null)
            {
                status = _EnableHook(hook, true, false, wantToCrash);
            }
            else
            {
                status = MH_STATUS.MH_ERROR_NOT_CREATED;
            }

            return status;
        }

        /// <summary>
        /// Disables a hook but does not destroy it. Can be enabled later.
        /// </summary>
        /// <param name="targetAddr"></param>
        /// /// <param name="wantToCrash">Skips thread handling when overwriting target method code. This can lead to a crash (immediately or at any point in the future due to corruption).</param>
        /// <returns></returns>
        public static MH_STATUS DisableHook(IntPtr targetAddr, bool wantToCrash = false)
        {
            MH_STATUS status = MH_STATUS.MH_OK;
            HookEntry hook = FindHook(targetAddr);
            if (hook != null)
            {
                status = _EnableHook(hook, false, false, wantToCrash);
            }
            else
            {
                status = MH_STATUS.MH_ERROR_NOT_CREATED;
            }

            return status;
        }

        /// <summary>
        /// Disables all hooks, but does not destroy them. They can be re-enabled at any point.
        /// </summary>
        /// <returns></returns>
        public static MH_STATUS DisableAllHooks()
        {
            return _EnableHook(null, false, true);
        }

        static MH_STATUS _EnableHook(HookEntry targetHook, bool enable, bool allHooks = false, bool wantToCrash = false)
        {
            MH_STATUS status = MH_STATUS.MH_OK;
            lock (g_Lock)
            {
                try
                {
                    if ((IsMono) && (!wantToCrash) && (g_ThreadIDs.Count == 0))
                        g_ThreadIDs = EnumerateThreads_NtProcessManager();
                    if (allHooks)
                    {
                        if (!wantToCrash)
                        {
                            if (IsMono)
                                Freeze(g_ThreadIDs, null, enable, true);
                            /*
                            else
                                Freeze(null, enable, true);
                            */
                        }
                        foreach (HookEntry hook in g_Hooks)
                        {
                            if (hook.isEnabled != enable)
                            {
                                status = EnableHookLL(hook, enable);
                                if (status != MH_STATUS.MH_OK)
                                    break;
                            }
                        }
                        if (!wantToCrash)
                        {
                            if (IsMono)
                                UnFreeze(g_ThreadIDs);
                            /*
                            else
                                UnFreeze();
                            */
                        }
                    }
                    else if (targetHook.isEnabled != enable)
                    {
                        if (!wantToCrash)
                        {
                            if (IsMono)
                                Freeze(g_ThreadIDs, targetHook, enable);
                            /*
                            else
                                Freeze(targetHook, enable);
                            */
                        }
                        status = EnableHookLL(targetHook, enable);
                        if (!wantToCrash)
                        {
                            if (IsMono)
                                UnFreeze(g_ThreadIDs);
                            /*
                            else
                                UnFreeze();
                            */
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.WriteException("Minhook", "_EnableHook", ex);
                    return MH_STATUS.MH_UNKNOWN;
                }
            }
            return status;
        }

        //-------------------------------------------------------------------------
        static unsafe MH_STATUS EnableHookLL(HookEntry pHook, bool enable)
        {
#if VERBOSE_PROFILING
            System.DateTime before = System.DateTime.Now;
#endif
            AllocationProtect oldProtect;
            uint patchSize = (uint)sizeof(JMP_CALL_REL);
            byte* pPatchTarget = (byte*)pHook.targetFunc;

            if (pHook.patchAbove)
            {
                pPatchTarget -= sizeof(JMP_CALL_REL);
                patchSize += (uint)sizeof(JMP_REL_SHORT);
            }

            if (!NativeImport.VirtualProtect((IntPtr)pPatchTarget, (IntPtr)patchSize, AllocationProtect.PAGE_EXECUTE_READWRITE, out oldProtect))
                return MH_STATUS.MH_ERROR_MEMORY_PROTECT;

            if (enable)
            {
                JMP_CALL_REL* pJmp = (JMP_CALL_REL*)pPatchTarget;
                pJmp->opcode = 0xE9;
                pJmp->operand = (uint)((byte*)pHook.detourFunc - (pPatchTarget + sizeof(JMP_CALL_REL)));

                if (pHook.patchAbove)
                {
                    JMP_REL_SHORT* pShortJmp = (JMP_REL_SHORT*)pHook.targetFunc;
                    pShortJmp->opcode = 0xEB;
                    pShortJmp->operand = (byte)(0 - (sizeof(JMP_REL_SHORT) + sizeof(JMP_CALL_REL)));
                }
            }
            else
            {
                fixed (byte* backup = pHook.backup)
                {
                    if (pHook.patchAbove)
                        NativeImport.memcpy(pPatchTarget, backup, (IntPtr)(sizeof(JMP_CALL_REL) + sizeof(JMP_REL_SHORT)));
                    else
                        NativeImport.memcpy(pPatchTarget, backup, (IntPtr)sizeof(JMP_CALL_REL));
                }
            }

            NativeImport.VirtualProtect((IntPtr)pPatchTarget, (IntPtr)patchSize, oldProtect, out oldProtect);

            // Just-in-case measure.
            NativeImport.FlushInstructionCache(Process.GetCurrentProcess().Handle, (IntPtr)pPatchTarget, (IntPtr)patchSize);

            pHook.isEnabled = enable;
#if VERBOSE_PROFILING
            System.DateTime after = System.DateTime.Now;
            System.TimeSpan span = after - before;
            Logger.Log.Write("[MinHook] " + (enable ? "Enabled" : "Disabled") + " hook in " + span.TotalMilliseconds.ToString() + " ms");
#endif
            return MH_STATUS.MH_OK;
        }

        /// <summary>
        /// Permanently destroy all hooks to target function
        /// Cleans up unmanaged memory
        /// </summary>
        /// <param name="origType"></param>
        /// <param name="origFuncName"></param>
        /// <param name="hookType"></param>
        /// <param name="hookFuncName"></param>
        /// <returns></returns>
        public static MH_STATUS DestroyHook(IntPtr targetAddr)
        {
            MH_STATUS status = MH_STATUS.MH_OK;

            lock (g_Lock)
            {
                HookEntry hook = FindHook(targetAddr);
                if (hook != null)
                {
                    if (hook.isEnabled)
                    {
                        if ((IsMono) && (g_ThreadIDs.Count == 0))
                        {
                            g_ThreadIDs = EnumerateThreads_NtProcessManager();
                            Freeze(g_ThreadIDs, hook, false);
                        }
                        else
                            Freeze(hook, false);
                        _EnableHook(hook, false);
                        if (IsMono)
                            UnFreeze(g_ThreadIDs);
                        else
                            UnFreeze();
                    }
                    if (status == MH_STATUS.MH_OK)
                    {
                        unsafe
                        {
                            MemoryBuffer.FreeBuffer((void*)hook.trampFunc);
                        }
                        g_Hooks.Remove(hook);
                    }
                }
                else
                {
                    status = MH_STATUS.MH_ERROR_NOT_CREATED;
                }
            }
            return status;
        }
    }
}