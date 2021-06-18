using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.CompilerServices;
using NinMods.Hooking.LowLevel;
using NinMods.Hooking.NativeImports;

namespace NinMods.Hooking
{
    public static class NativeStubHooker
    {
        public class StubInstance
        {
            /* Hook Target Info */
            internal IntPtr TargetAddr;
            internal MethodInfo TargetMethodInfo;
            /* Stub Info */
            internal IntPtr TrampAddr;
            internal IntPtr StubAddr;
            internal int StubSize;
            internal IntPtr Stub_NumHooksPtr;
            internal IntPtr Stub_TrampAddrPtr;
            internal IntPtr Stub_Start_Call_List;
            internal IntPtr Stub_End_Call_List;
            internal int TotalHooksAllocated;
            /* User Variables */
            private Delegate oFunc;
            private bool hasReturnType = false;
            internal List<IntPtr> hookAddresses = new List<IntPtr>();

            internal StubInstance(MethodInfo targetMethod, IntPtr targetMethodAddr, Delegate openDel, bool returnsVoid)
            {
                TargetMethodInfo = targetMethod;
                TargetAddr = targetMethodAddr;
                oFunc = openDel;
                hasReturnType = !returnsVoid;
            }

            public TResult CallOriginal<TResult>(params object[] args)
            {
                TResult defaultTResult = default(TResult);
#if PROFILING
                System.DateTime before = System.DateTime.Now;
#endif
                // TO-DO:
                // figure out why MinHook's FreezeThreads causes the game to freeze despite having a billion checks for CurrentThreadID
                if (MinHook.DisableHook(TargetAddr, true) != MinHook.MH_STATUS.MH_OK)
                {
                    Logger.Log.Write("NinMods.NativeStubHooker", "CallOriginal", "Could not disable hook");
                    return defaultTResult;
                }
#if PROFILING
                System.DateTime after = System.DateTime.Now;
                System.TimeSpan span = after - before;
                Logger.Log.Write("[NativeStubHooker] Executed LL_DisableHook in " + span.TotalMilliseconds.ToString() + " ms");
                before = DateTime.Now;
#endif
                object result = null;
                if (hasReturnType)
                    result = oFunc.DynamicInvoke(args);
                else
                    oFunc.DynamicInvoke(args);
#if PROFILING
                after = DateTime.Now;
                span = after - before;
                Logger.Log.Write("[NativeStubHooker] Invoked original function in " + span.TotalMilliseconds.ToString() + " ms");
                before = DateTime.Now;
#endif
                if (MinHook.EnableHook(TargetAddr, true) != MinHook.MH_STATUS.MH_OK)
                {
                    Logger.Log.Write("NinMods.NativeStubHooker", "CallOriginal", "Could not re-enable hook");
                    return defaultTResult;
                }
#if PROFILING
                after = DateTime.Now;
                span = after - before;
                Logger.Log.Write("[NativeStubHooker] Executed LL_EnableHook in " + span.TotalMilliseconds.ToString() + " ms");
#endif
                if (hasReturnType)
                    return (TResult)result;
                else
                    return defaultTResult;
            }
        }

        // NOTE [3 FEB 21]:
        // stubs aren't actually implemented, i got lazy.
        // the stubs are only there to support multiple hooks on a single target method, anyway.
        // so if you're only hooking a method once it doesn't matter.
        private static List<IntPtr> g_StubHeaps = new List<IntPtr>();
        private static byte[] stubCodeArr = new byte[] { 0x41, 0x5b, 0x4d, 0x89, 0x22, 0x4d, 0x89, 0x6a, 0x08, 0x4d, 0x89, 0x5a, 0x10, 0x49, 0x89, 0x4a, 0x18, 0x49, 0x89, 0x52, 0x20, 0x4d, 0x89, 0x42, 0x28, 0x4d, 0x89, 0x4a, 0x30, 0xf3, 0x41, 0x0f, 0x7f, 0x42, 0x38, 0xf3, 0x41, 0x0f, 0x7f, 0x4a, 0x48, 0xf3, 0x41, 0x0f, 0x7f, 0x52, 0x58, 0xf3, 0x41, 0x0f, 0x7f, 0x5a, 0x68, 0x49, 0xc7, 0xc4, 0x00, 0x00, 0x00, 0x00, 0x4d, 0x89, 0xd5, 0xeb, 0x03, 0x49, 0xff, 0xc4, 0x4d, 0x3b, 0x65, 0x78, 0x7d, 0x39, 0x49, 0x8b, 0x4d, 0x18, 0x49, 0x8b, 0x55, 0x20, 0x4d, 0x8b, 0x45, 0x28, 0x4d, 0x8b, 0x4d, 0x30, 0xf3, 0x41, 0x0f, 0x6f, 0x45, 0x38, 0xf3, 0x41, 0x0f, 0x6f, 0x4d, 0x48, 0xf3, 0x41, 0x0f, 0x6f, 0x55, 0x58, 0xf3, 0x41, 0x0f, 0x6f, 0x5d, 0x68, 0x4c, 0x89, 0xe8, 0x48, 0x05, 0x88, 0x00, 0x00, 0x00, 0x4a, 0x8b, 0x04, 0xe0, 0xff, 0xd0, 0xeb, 0xbe, 0x4d, 0x89, 0xea, 0x4d, 0x8b, 0x22, 0x4d, 0x8b, 0x6a, 0x08, 0x4d, 0x8b, 0x5a, 0x10, 0x41, 0x53, 0xc3, 0xcc, 0xcc };
        private static byte[] trampCodeArr = new byte[] { 0x49, 0x8B, 0x4A, 0x18, 0x49, 0x8B, 0x52, 0x20, 0x4D, 0x8B, 0x42, 0x28, 0x4D, 0x8B, 0x4A, 0x30, 0xF3, 0x41, 0x0F, 0x6F, 0x42, 0x38, 0xF3, 0x41, 0x0F, 0x6F, 0x4A, 0x48, 0xF3, 0x41, 0x0F, 0x6F, 0x52, 0x58, 0xF3, 0x41, 0x0F, 0x6F, 0x5A, 0x68, 0x49, 0x8B, 0x82, 0x80, 0x00, 0x00, 0x00, 0xFF, 0xE0, 0xC3 };

        private static List<StubInstance> m_AllStubs = new List<StubInstance>();
        private static Dictionary<MethodInfo, StubInstance> m_StubsByTargetMethod = new Dictionary<MethodInfo, StubInstance>();

        public static void Uninitialize()
        {
            foreach (IntPtr stubBlock in g_StubHeaps)
            {
                if (!NativeImport.VirtualFree(stubBlock, IntPtr.Zero, (FreeType.Decommit | FreeType.Release)))
                {
                    Logger.Log.Write("NinMods.NativeStubHooker", "Uninitialize", "Could not free memory associated with a stub");
                }
            }
        }

        unsafe private static bool UpdateStub64(StubInstance stubEntry, IntPtr hookAddr)
        {
            if ((*((long*)stubEntry.Stub_NumHooksPtr.ToPointer()) + 1) > stubEntry.TotalHooksAllocated)
            {
                Logger.Log.Write("NinMods.NativeStubHooker", "UpdateStub64", "Exceeded 255 hooks for this target. Reallocation is not currently supported\n");
                return false;
                // NOTE: 
                // must freeze threads. minhook freezes and unfreezes threads each operation.
                // must relocate thread IPs if they are executing within old stub range.
                // steps:
                // 1. Unhook via MinHook
                // 2. freeze all threads
                // 3. copy all hooks to local vector<T>
                // 4. free old stub via VirtualFree
                // 5. call CreateStubWithHooks w/ new size specified, and copy of old hooks as vector<T>
                // 6. hook via MinHook
                // 7. unfreeze all threads
            }
            ((long*)stubEntry.Stub_Start_Call_List.ToPointer())[*((long*)stubEntry.Stub_NumHooksPtr.ToPointer())] = hookAddr.ToInt64();
            *((long*)stubEntry.Stub_NumHooksPtr.ToPointer()) = (*((long*)stubEntry.Stub_NumHooksPtr.ToPointer()) + 1);
            Logger.Log.Write("NinMods.NativeStubHooker", "UpdateStub64", "Updated stub to call an additional hookAddr [" + hookAddr.ToString());
            return true;
        }

        unsafe private static bool CreateStubWithHooks64(StubInstance stubEntry, List<IntPtr> hooks)
        {
            const int StubCodeSize = 160;
            const int TrampCodeSize = 60;
            const int DataSectionSize = 136;

            const int OffsetToHookCount = 120;

            int bytesForHooks = ((hooks.Count() > 255 ? hooks.Count : 255) * 8);
            int numBytes = StubCodeSize + TrampCodeSize + DataSectionSize + bytesForHooks;
            List<byte> stubBytes = new List<byte>(numBytes);

            IntPtr stubBlock = NativeImport.VirtualAlloc(IntPtr.Zero, (IntPtr)numBytes, (AllocationType.Reserve | AllocationType.Commit), AllocationProtect.PAGE_EXECUTE_READWRITE);
            if ((stubBlock == null) || (stubBlock == IntPtr.Zero))
            {
                Logger.Log.Write("NinMods.NativeStubHooker", "CreateStubWithHooks64", "Could not allocate memory block for stub");
                return false;
            }
            g_StubHeaps.Add(stubBlock);

            IntPtr addrOfDataSection = (stubBlock + StubCodeSize) + TrampCodeSize;
            long addrOfDataSectionAsLong = addrOfDataSection.ToInt64();
            Logger.Log.Write("NinMods.NativeStubHooker", "CreateStubWithHooks64", "Allocated memory for stub @ " + stubBlock.ToString("X") +
                "\n--Data section starts @ " + addrOfDataSection.ToString("X") + " and ends at " + (addrOfDataSection + (numBytes - 160)).ToString("X") +
                "\n--Loop counter should be @ " + (addrOfDataSection + (numBytes - 160 - bytesForHooks)).ToString("X"));

            stubBytes.AddRange(new byte[]{ 0x49, 0xba});
            // have to reverse byte order here
            for (int byteIndex = 0; byteIndex < 8; byteIndex++)
            {
                stubBytes.Add((byte)(addrOfDataSectionAsLong >> (byteIndex * 8)));
            }
            // this is the stub code after the movabs, <AddrOfDataSection> instruction
            stubBytes.AddRange(stubCodeArr);
            // now we write the trampoline code (this will invoke the MinHook trampoline function)
            // movabs r10, <AddrOfDataSection>
            stubBytes.AddRange(new byte[] { 0x49, 0xba });
            for (int byteIndex = 0; byteIndex < 8; byteIndex++)
            {
                stubBytes.Add((byte)(addrOfDataSectionAsLong >> (byteIndex * 8)));
            }
            // this is the tramp code after the movabs, <AddrOfDataSection> instruction
            stubBytes.AddRange(trampCodeArr);

            // now we fill in the data section
            // the hooks are stored at the end, after all other data, so we need to zero out those parts to get to the hooks part
            for (int index = 0; index < OffsetToHookCount; index++)
            {
                stubBytes.Add(0x0);
            }
            // first we need to fill in the # of hooks (used in the for-loop conditional in stub code)
            // we extend this to 64 bit because the cmp instruction is encoded w/ REX.W set, so it expects 64 bits.
            long hookCount = (long)hooks.Count;
            for (int byteIndex = 0; byteIndex < 8; byteIndex++)
            {
                stubBytes.Add((byte)(hookCount >> (byteIndex * 8)));
            }
            // next comes the trampoline address, which we'll zero for now
            for (int index = 0; index < 8; index++)
            {
                stubBytes.Add(0x0);
            }
            // now we can fill out the hooks "array" (we allocated space for 255 or hooks->size(), but if hooks->size < 255 we don't need to zero-out those unfilled slots)
            for (int index = 0; index < hooks.Count; index++)
            {
                // address bytes
                long hookAddr = hooks[index].ToInt64();
                for (int byteIndex = 0; byteIndex < 8; byteIndex++)
                {
                    stubBytes.Add((byte)(hookAddr >> (byteIndex * 8)));
                }
            }
            byte[] stubBytesArray = stubBytes.ToArray();
            fixed (byte* stubBytesPtr = stubBytesArray)
            {
                NativeImport.memcpy(stubBlock.ToPointer(), stubBytesPtr, (IntPtr)numBytes);
            }
            NativeImport.FlushInstructionCache(System.Diagnostics.Process.GetCurrentProcess().Handle, stubBlock, (IntPtr)numBytes);
            Logger.Log.Write("NinMods.NativeStubHooker", "CreateStubWithHooks64", "Filled out stub block with stub bytes, hopefully");

            stubEntry.StubAddr = stubBlock;
            stubEntry.StubSize = numBytes;
            stubEntry.Stub_NumHooksPtr = addrOfDataSection +120;
            stubEntry.Stub_TrampAddrPtr = addrOfDataSection +128;
            stubEntry.Stub_Start_Call_List = addrOfDataSection +136;
            stubEntry.Stub_End_Call_List = stubBlock + numBytes;
            stubEntry.TotalHooksAllocated = 255;
            return true;
        }

        unsafe public static StubInstance HookMethod(MethodInfo targetMethod, MethodInfo hookMethod, Type openDelType, out int hookIndex, out IntPtr oFuncAddr)
        {
            hookIndex = -1;
            oFuncAddr = IntPtr.Zero;
            RuntimeMethodHandle runtimeMethodHandle = targetMethod.MethodHandle;
            if (runtimeMethodHandle == null)
            {
                Logger.Log.Write("NinMods.NativeStubHooker", "HookMethod", "Could not get RuntimeMethodHandle for '" + targetMethod.DeclaringType.FullName + "::" + targetMethod.Name + "'");
                return null;
            }
            IntPtr targetMethodAddr = runtimeMethodHandle.GetFunctionPointer();
            runtimeMethodHandle = hookMethod.MethodHandle;
            if (runtimeMethodHandle == null)
            {
                Logger.Log.Write("NinMods.NativeStubHooker", "HookMethod", "Could not get RuntimeMethodHandle for '" + hookMethod.DeclaringType.FullName + "::" + hookMethod.Name + "'");
                return null;
            }
            IntPtr hookMethodAddr = runtimeMethodHandle.GetFunctionPointer();
            Delegate openDelegate = Delegate.CreateDelegate(openDelType, null, targetMethod);
            StubInstance stub;
            if (!m_StubsByTargetMethod.TryGetValue(targetMethod, out stub))
            {
                stub = new StubInstance(targetMethod, targetMethodAddr, openDelegate, (targetMethod.ReturnType == typeof(void)));
                stub.hookAddresses.Add(hookMethodAddr);
                // TO-DO:
                // 1. Create Stub
                //CreateStubWithHooks64(stub, new List<IntPtr>() { hookMethodAddr });
                // 2. Use MinHook to detour targetMethod to newly created stub
                MinHook.MH_STATUS mhStatus = MinHook.CreateHook(targetMethodAddr, hookMethodAddr, out stub.TrampAddr);
                if (mhStatus != MinHook.MH_STATUS.MH_OK)
                {
                    Logger.Log.Write("NinMods.NativeStubHooker", "HookMethod", "Could not create hook via MinHook (" + mhStatus.ToString() + ")");
                    return null;
                }
                //*((long*)stub.Stub_TrampAddrPtr.ToPointer()) = stub.TrampAddr.ToInt64();
                mhStatus = MinHook.EnableHook(targetMethodAddr);
                if (mhStatus != MinHook.MH_STATUS.MH_OK)
                {
                    Logger.Log.Write("NinMods.NativeStubHooker", "HookMethod", "Could not enable hook via MinHook (" + mhStatus.ToString() + ")");
                    return null;
                }
                m_StubsByTargetMethod.Add(targetMethod, stub);
            }
            else
            {
                if (!stub.hookAddresses.Contains(hookMethodAddr))
                {
                    // this target method has already been hooked, so update the stub
                    if (!UpdateStub64(stub, hookMethodAddr))
                    {
                        Logger.Log.Write("NinMods.NativeStubHooker", "HookMethod", "Could not update stub for hook");
                        return null;
                    }
                    stub.hookAddresses.Add(hookMethodAddr);
                }
                else
                {
                    Logger.Log.Write("NinMods.NativeStubHooker", "HookMethod", "'" + hookMethod.Name + "' is already registered for '" + targetMethod.Name + "', skipping.");
                }
            }
            return stub;
        }
    }
}