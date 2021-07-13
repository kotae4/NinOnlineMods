using NinMods.Hooking.NativeImports;
using NinMods.Hooking.Utilities;
using NinMods.Hooking.LowLevel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace NinMods.Hooking
{
    public static class ManagedHooker
    {
        private const uint HOOK_SIZE_X64 = 12;
        private const uint HOOK_SIZE_X86 = 7;


        enum EChainedHookerStatus
        {
            Failed,
            New,
            Existing
        };

        public class HookEntry
        {
            /// <summary>
            /// This is set to false each time the game calls the original function.
            /// Then it's set to true when any hook calls the trampoline.
            /// </summary>
            public bool HasOriginalFunctionBeenCalled = false;

            // fuck, i forgot i wanted to associate priority with each delegate (hook)
            // it'd take a little too much work to convert this to a dictionary
            // and access modifiers are already a big issue so i don't want to write an Add function
            // so i'm just going to have two separate lists and pray i can keep track of them
            internal List<int> _PRIV_HookPriorities = new List<int>();
            // this HAS to be public for the dynamic method to call it. raw IL still has to pass the module checks.
            // and i really don't want to write the IL to call it via reflection
            public List<Delegate> _PRIV_Hooks = new List<Delegate>();
            public Delegate _PRIV_ChainDynFunc = null;
            internal IntPtr _PRIV_pChainedHooker = IntPtr.Zero;
            internal Delegate _PRIV_pTrampoline = null;

            /// <summary>
            /// Calls the original function without resulting in infinite recursion.
            /// </summary>
            /// <returns>The declared return type of the original function, you must pass this type in TRet</returns>
            public object CallOriginalFunction(Type retType, params object[] args)
            {
                // making this generic isn't completely necessary, but i think it might avoid boxing in certain cases?
                // or maybe making it generic results in boxing all the time. oh well. don't care to check.
                // the important part is just setting this bool.
                HasOriginalFunctionBeenCalled = true;
                // special handling for void return types, just in case the runtime doesn't handle it
                if (retType != typeof(void))
                    return _PRIV_pTrampoline.DynamicInvoke(args);
                _PRIV_pTrampoline.DynamicInvoke(args);
                return null;
            }
        }

        public static Dictionary<string, HookEntry> HookedDict = new Dictionary<string, HookEntry>();

        static RuntimeMethodHandle GetMethodRuntimeHandle(MethodBase method)
        {
            // TO-DO:
            // .NET versions > 3.x don't allow you to access the MethodHandle of a DynamicMethod
            // Instead, you have to use reflection to access the private '_method' field to get the IntPtr
            // Seems mono doesn't have this limitation
            RuntimeMethodHandle handle;
            try
            {
                if (Environment.Version.Major == 4)
                {
                    var getMethodDescriptorInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (getMethodDescriptorInfo == null)
                    {
                        Logger.Log.Write("Could not get 'GetMethodDescriptor' of DynamicMethod type");
                        throw new Exception("Could not get 'GetMethodDescriptor' of DynamicMethod type");
                    }
                    else
                        handle = (RuntimeMethodHandle)getMethodDescriptorInfo.Invoke(method, null);
                }
                else
                {
                    var fieldInfo = typeof(DynamicMethod).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo == null)
                    {
                        Logger.Log.Write("Could not get 'm_method' of DynamicMethod type");
                        throw new Exception("Could not get 'm_method' of DynamicMethod type");
                    }
                    else
                        handle = (RuntimeMethodHandle)fieldInfo.GetValue(method);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.WriteException(ex);
                throw ex;
            }
            /*
            var fieldInfo = typeof(Delegate).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
            handle = (RuntimeMethodHandle)fieldInfo.GetValue(method);
            */
            return handle;
        }

        private static EChainedHookerStatus GetOrCreateChainedHooker(string chainName, MethodInfo targetMethodInfo, Type closedDelegateType, out HookEntry hookEntry)
        {
            if (HookedDict.TryGetValue(chainName, out hookEntry))
            {
                Logger.Log.Write("Retrieved existing hook chain");
                return EChainedHookerStatus.Existing;
            }
            Logger.Log.Write("Creating new hook chain");
            hookEntry = null;

            ParameterInfo[] args = targetMethodInfo.GetParameters();
            Type[] argTypes;
            if (!targetMethodInfo.IsStatic)
            {
                Logger.Log.Write("Target method is not static. Including 'this' parameter in signature.");
                argTypes = new Type[args.Length + 1];
                argTypes[0] = targetMethodInfo.DeclaringType;
                for (int i = 0; i < args.Length; i++)
                    argTypes[i + 1] = args[i].ParameterType;
            }
            else
            {
                Logger.Log.Write("Target method is static. Not using 'this' parameter.");
                argTypes = new Type[args.Length];
                for (int i = 0; i < args.Length; i++)
                    argTypes[i] = args[i].ParameterType;
            }



            DynamicMethod iterateChainedHooksDynFunc = new DynamicMethod(
                "chain_" + chainName,
                targetMethodInfo.ReturnType ?? typeof(void),
                argTypes,
                targetMethodInfo.DeclaringType,
                false
                );

            Logger.Log.Write("Instantiated blank DynamicMethod, filling now...");

            #region Boilerplate Type Definitions
            Type thisType = typeof(ManagedHooker);
            FieldInfo dictField = thisType.GetField("HookedDict", BindingFlags.Static | BindingFlags.Public);

            Type hookInfoType = typeof(HookEntry);
            FieldInfo hookInfoHooksField = hookInfoType.GetField("_PRIV_Hooks", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (hookInfoHooksField != null)
            {
                Logger.Log.Write($"hookInfoHooksField: {hookInfoHooksField}");
            }
            else
            {
                Logger.Log.Write("hookInfoHooksField failed!!!");
                return EChainedHookerStatus.Failed;
            }

            FieldInfo hookInfoHasCalledField = hookInfoType.GetField("HasOriginalFunctionBeenCalled", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (hookInfoHasCalledField != null)
            {
                Logger.Log.Write($"hookInfoHasCalledField: {hookInfoHasCalledField}");
            }
            else
            {
                Logger.Log.Write("hookInfoHasCalledField failed!!!");
                return EChainedHookerStatus.Failed;
            }

            Type dictType = ManagedHooker.HookedDict.GetType();
            /*
            Type[] genericArgs = dictType.GetGenericArguments();
            foreach (Type gT in genericArgs)
                logger.Log("    " + gT.Name + " [" + gT.ToString() + "]");
            */
            MethodInfo dictGetFieldMethod = dictType.GetMethod("get_Item");
            /*
            MethodInfo[] methods = dictType.GetMethods();
            foreach (MethodInfo method in methods)
                logger.Log("    " + method.Name + " " + method.ToString());
            */
            if (dictGetFieldMethod != null)
            {
                Logger.Log.Write($"DictGetFieldMethod: {dictGetFieldMethod} is generic? {dictGetFieldMethod.ContainsGenericParameters}");
            }
            else
            {
                Logger.Log.Write("DictGetFieldMethod failed!!!");
                return EChainedHookerStatus.Failed;
            }



            Type listType = typeof(List<Delegate>);
            MethodInfo listGetEnumeratorMethod = listType.GetMethod("GetEnumerator");
            if (listGetEnumeratorMethod != null)
            {
                Logger.Log.Write($"listGetEnumeratorMethod: {listGetEnumeratorMethod}");
            }
            else
            {
                Logger.Log.Write("listGetEnumeratorMethod failed!!!");
                return EChainedHookerStatus.Failed;
            }

            Type listEnumeratorType = typeof(List<Delegate>.Enumerator);
            MethodInfo listEnumeratorGetCurrentMethod = listEnumeratorType.GetMethod("get_Current");
            if (listEnumeratorGetCurrentMethod != null)
            {
                Logger.Log.Write($"listEnumeratorGetCurrentMethod: {listEnumeratorGetCurrentMethod}");
            }
            else
            {
                Logger.Log.Write("listEnumeratorGetCurrentMethod failed!!!");
                return EChainedHookerStatus.Failed;
            }
            MethodInfo listEnumeratorMoveNextMethod = listEnumeratorType.GetMethod("MoveNext");
            if (listEnumeratorMoveNextMethod != null)
            {
                Logger.Log.Write($"listEnumeratorMoveNextMethod: " + listEnumeratorMoveNextMethod.ToString());
            }
            else
            {
                Logger.Log.Write("listEnumeratorMoveNextMethod failed!!!");
                return EChainedHookerStatus.Failed;
            }
            MethodInfo listEnumeratorDispose = listEnumeratorType.GetMethod("Dispose");
            if (listEnumeratorDispose != null)
            {
                Logger.Log.Write($"listEnumeratorDispose: {listEnumeratorDispose}");
            }
            else
            {
                Logger.Log.Write("listEnumeratorDispose failed!!!");
                return EChainedHookerStatus.Failed;
            }

            Type delegateType = typeof(Delegate);
            MethodInfo delegateDynamicInvokeMethod = delegateType.GetMethod("DynamicInvoke");
            if (delegateDynamicInvokeMethod != null)
            {
                Logger.Log.Write($"delegateDynamicInvokeMethod: {delegateDynamicInvokeMethod}");
            }
            else
            {
                Logger.Log.Write("delegateDynamicInvokeMethod failed!!!");
                return EChainedHookerStatus.Failed;
            }
            #endregion

            ILGenerator il = iterateChainedHooksDynFunc.GetILGenerator();
            Label moveNextLabel = il.DefineLabel();
            Label processNextLabel = il.DefineLabel();
            //Label exitLabel = il.DefineLabel();
            // NOTE:
            // the way the JIT compiler handles the try{} finally{} block is pretty clever

            il.DeclareLocal(typeof(HookEntry));
            il.DeclareLocal(typeof(List<Delegate>.Enumerator));
            il.DeclareLocal(typeof(Delegate));
            if (targetMethodInfo.ReturnType != typeof(void))
            {
                il.DeclareLocal(typeof(Object));

                if (targetMethodInfo.ReturnType.IsNumericType())
                {
                    // return type is a bool, int, float, double, byte, etc
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Box, typeof(Int32));
                }
                else if (targetMethodInfo.ReturnType.IsValueType)
                {
                    // return type is a struct
                    il.Emit(OpCodes.Ldloca_S, 3);
                    il.Emit(OpCodes.Initobj, targetMethodInfo.ReturnType);
                    il.Emit(OpCodes.Box, targetMethodInfo.ReturnType);
                }
                else
                {
                    // return type is a reference
                    il.Emit(OpCodes.Ldnull);
                }
                // Store the null ClassReference at local variable index#0 (this will be the return value)
                il.Emit(OpCodes.Stloc_3);
            }

            il.Emit(OpCodes.Ldsfld, dictField);

            il.Emit(OpCodes.Ldstr, chainName);
            il.Emit(OpCodes.Callvirt, dictGetFieldMethod);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stfld, hookInfoHasCalledField);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldfld, hookInfoHooksField);
            il.Emit(OpCodes.Callvirt, listGetEnumeratorMethod);

            // Store the List<Delegate>.Enumerator at local variable index#0
            il.Emit(OpCodes.Stloc_1);

            Label exceptionBlock = il.BeginExceptionBlock();

            il.Emit(OpCodes.Br_S, moveNextLabel);

            il.MarkLabel(processNextLabel);
            // Push the List<Delegate>.Enumerator local variable onto the stack
            il.Emit(OpCodes.Ldloca_S, (byte)1);
            il.Emit(OpCodes.Call, listEnumeratorGetCurrentMethod);
            // Store the Delegate at local variable index#1
            il.Emit(OpCodes.Stloc_2);

            // PROCESS DYNAMIC ARGUMENTS
            // Push the Delegate local variable onto the stack
            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Ldc_I4, argTypes.Length);
            il.Emit(OpCodes.Newarr, typeof(Object));
            Logger.Log.Write($"There are {argTypes.Length} args.");
            for (int index = 0; index < argTypes.Length; index++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, index);
                il.Emit(OpCodes.Ldarg, (short)index);
                if (argTypes[index].IsValueType)
                {
                    il.Emit(OpCodes.Box, argTypes[index]);
                }
                il.Emit(OpCodes.Stelem_Ref);
                Logger.Log.Write($"Emitted arg {argTypes[index].Name}");
            }
            // END PROCESS DYNAMIC ARGUMENTS
            il.Emit(OpCodes.Callvirt, delegateDynamicInvokeMethod);
            // Store the returned ClassReference at local variable index#0, or discard it if the return type is void
            if (targetMethodInfo.ReturnType == typeof(void))
                il.Emit(OpCodes.Pop);
            else
                il.Emit(OpCodes.Stloc_3);

            il.MarkLabel(moveNextLabel);

            // Push the List<Delegate>.Enumerator local variable onto the stack
            il.Emit(OpCodes.Ldloca_S, (byte)1);
            il.Emit(OpCodes.Call, listEnumeratorMoveNextMethod);
            il.Emit(OpCodes.Brtrue_S, processNextLabel);
            il.Emit(OpCodes.Leave_S, exceptionBlock);

            il.BeginFinallyBlock();

            // Push the List<Delegate>.Enumerator local variable onto the stack
            il.Emit(OpCodes.Ldloca_S, (byte)1);
            il.Emit(OpCodes.Constrained, listEnumeratorType);
            il.Emit(OpCodes.Callvirt, listEnumeratorDispose);
            il.Emit(OpCodes.Endfinally);

            //il.MarkLabel(exitLabel);

            il.EndExceptionBlock();


            if (targetMethodInfo.ReturnType != typeof(void))
            {
                il.Emit(OpCodes.Ldloc_3);
                if ((targetMethodInfo.ReturnType.IsNumericType()) || (targetMethodInfo.ReturnType.IsValueType))
                {
                    // return type is a bool, int, float, double, byte, etc
                    // OR
                    // return type is a struct
                    il.Emit(OpCodes.Unbox_Any, targetMethodInfo.ReturnType);
                }
                else
                {
                    // return type is a reference
                    il.Emit(OpCodes.Castclass, targetMethodInfo.ReturnType);
                }
            }
            il.Emit(OpCodes.Ret);


            Logger.Log.Write("Finalizing chain hook dynMethod by creating delegate");
            Delegate finalizedDynMethod = iterateChainedHooksDynFunc.CreateDelegate(closedDelegateType);

            // DESIGN FLAWS:
            // 1. It's up to each hook to call the original function (through the trampoline function)
            //      * There's no way for hook#2 to know if hook#1 already called the original function, or to share the results of the call, or anything
            //      * A huge optimization would be to add handling for calling the trampoline into this dyn func, or to just pass it on to the next hook

            // TO-DO:
            // .NET versions > 3.x don't allow you to access the MethodHandle of a DynamicMethod
            // Instead, you have to use reflection to access the private '_method' field to get the IntPtr
            // Seems mono doesn't have this limitation
            hookEntry = new HookEntry();
            hookEntry._PRIV_pChainedHooker = GetMethodRuntimeHandle(iterateChainedHooksDynFunc.GetBaseDefinition()).GetFunctionPointer();
            //hookEntry._PRIV_pChainedHooker = finalizedDynMethod.Method.MethodHandle.GetFunctionPointer();
            hookEntry._PRIV_ChainDynFunc = finalizedDynMethod;
            HookedDict.Add(chainName, hookEntry);

            return EChainedHookerStatus.New;
        }

        public static HookEntry HookMethod<TDelOpen>(Type targetType, string targetMethod, Type hookType, string hookMethod, int hookPriority)
        {
            MethodInfo targetMethodInfo;
            IntPtr targetMethodAddr = Utility.GetMethodAddrByName(targetType, targetMethod, out targetMethodInfo);
            if (targetMethodAddr == IntPtr.Zero)
            {
                // this will never happen, unfortunately. a pre-JIT'd method will still return a valid address, it's just pointing to a thunk rather than a function.
                // and determining whether it's pre-JIT or post-JIT is quite hard
                throw new Exception($"Could not get address for target method '{targetMethod}'");
            }
            Logger.Log.Write($"Target method '{targetMethod}' is at address {targetMethodAddr.ToString("X2")}");
            MethodInfo hookMethodInfo;
            IntPtr hookMethodAddr = Utility.GetMethodAddrByName(hookType, hookMethod, out hookMethodInfo);
            if (hookMethodAddr == IntPtr.Zero)
            {
                // this will never happen, unfortunately. a pre-JIT'd method will still return a valid address, it's just pointing to a thunk rather than a function.
                // and determining whether it's pre-JIT or post-JIT is quite hard
                throw new Exception($"Could not get address for hook method '{targetMethod}'");
            }
            Delegate hookDel = hookMethodInfo.CreateDelegate(typeof(TDelOpen));
            if (hookDel == null)
            {
                throw new Exception($"Could not create delegate for hook method '{targetMethod}'");
            }
            Logger.Log.Write($"Hook method '{hookMethod}' is at address {hookMethodAddr.ToString("X2")}, created delegate {hookDel}");

            string dynName = $"{targetMethodInfo.Name}_{targetMethod.GetHashCode()}";
            DynamicMethod jmpToTrampDynFunc;
            MinHook.MH_STATUS mhStatus;

            // closed around 'null', effectively an open delegate since parameter list will be used and not the 'target' property.
            // this allows us to work with both instance and static methods
            Delegate closedDelegate = Delegate.CreateDelegate(typeof(TDelOpen), null, targetMethodInfo);
            Type closedDelegateType = closedDelegate.GetType();

            // 1. Get or set up the chained hooker. Most of the magic is in this step.
            HookEntry hookEntry = new HookEntry();
            EChainedHookerStatus chainedHookStatus = GetOrCreateChainedHooker(dynName, targetMethodInfo, closedDelegateType, out hookEntry);
            if (chainedHookStatus == EChainedHookerStatus.Failed)
            {
                Logger.Log.Write($"Error retrieving chained hooker. (Target function: {dynName})");
                return null;
            }
            else if (chainedHookStatus == EChainedHookerStatus.New)
            {
                // 2. If this is the first time the target is being hooked, set up an invokable trampoline
                // Create dummy dynamic method (this step is copied from MonoMod project on github, credits to github user '0x0ade')
                // we will then write our jmp bytes to this method's addr and store its invocable delegate for it to later be called from within the hook.
                // so we must match the signature of the target method (what we're jmp'ing to)
                // and have a bare minimum compilable body (the actual body won't be executed since we're writing our jmp bytes over it)
                Logger.Log.Write("Creating dummy dynMethod for trampoline hack");
                ParameterInfo[] args = targetMethodInfo.GetParameters();
                Type[] argTypes;
                if (!targetMethodInfo.IsStatic)
                {
                    argTypes = new Type[args.Length + 1];
                    argTypes[0] = targetMethodInfo.DeclaringType;
                    for (int i = 0; i < args.Length; i++)
                        argTypes[i + 1] = args[i].ParameterType;
                }
                else
                {
                    argTypes = new Type[args.Length];
                    for (int i = 0; i < args.Length; i++)
                        argTypes[i] = args[i].ParameterType;
                }
                jmpToTrampDynFunc = new DynamicMethod(
                    "tramp_" + dynName,
                    targetMethodInfo.ReturnType ?? typeof(void),
                    argTypes,
                    targetMethodInfo.DeclaringType,
                    false
                    );
                ILGenerator il = jmpToTrampDynFunc.GetILGenerator();
                // i don't remember what problem this solved.
                // maybe just making sure it's long enough for the jmp that we write later?
                for (int i = 0; i < 10; i++)
                {
                    il.Emit(OpCodes.Nop);
                }
                // this is the bare minimum code needed for a method body in C#.
                if (jmpToTrampDynFunc.ReturnType != typeof(void))
                {
                    // initialize default return type
                    il.DeclareLocal(jmpToTrampDynFunc.ReturnType);
                    il.Emit(OpCodes.Ldloca_S, (sbyte)0);
                    il.Emit(OpCodes.Initobj, jmpToTrampDynFunc.ReturnType);
                    il.Emit(OpCodes.Ldloc_0);
                }
                il.Emit(OpCodes.Ret);

                // 3. Detour target function using minhook, get trampoline address out from minhook
                // We detour to the chained hooker address, which then services each hook
                Logger.Log.Write("Creating hook to chain via MinHook");
                IntPtr tramp;
                mhStatus = MinHook.CreateHook(targetMethodAddr, hookEntry._PRIV_pChainedHooker, out tramp);
                if (mhStatus != MinHook.MH_STATUS.MH_OK)
                {
                    Logger.Log.Write("Error creating hook: " + mhStatus.ToString());
                    return null;
                }

                // 4. Get JIT'd address of jmpToTrampDynFunc (DynamicMethod.CreateDelegate finalizes the dynamicmethod)
                Logger.Log.Write("Finalizing dummy dynMethod for trampoline hack by creating delegate");
                Delegate trampDynFuncDelegate = jmpToTrampDynFunc.CreateDelegate(closedDelegateType);
                IntPtr dynJITAddr = GetMethodRuntimeHandle(jmpToTrampDynFunc.GetBaseDefinition()).GetFunctionPointer();
                // 5. Write jmp to trampoline over JIT'd code of jmpToTrampDynFunc.
                // don't need to worry about freezing threads or correcting IPs since the code it's writing over isn't called by anything yet
                Logger.Log.Write("Writing jmp to native trampoline over dummy dynMethod");
                AllocationProtect oldProt;
                if (IntPtr.Size == 8)
                {
                    NativeImport.VirtualProtect(dynJITAddr, (IntPtr)HOOK_SIZE_X64, AllocationProtect.PAGE_EXECUTE_READWRITE, out oldProt);
                    unsafe
                    {
                        byte* ptr = (byte*)dynJITAddr;

                        // movabs rax, addr
                        // jmp rax
                        *(ptr) = 0x48;
                        *(ptr + 1) = 0xb8;
                        *(IntPtr*)(ptr + 2) = tramp;
                        *(ptr + 10) = 0xff;
                        *(ptr + 11) = 0xe0;
                    }
                    NativeImport.VirtualProtect(dynJITAddr, (IntPtr)HOOK_SIZE_X64, oldProt, out oldProt);
                }
                else
                {
                    NativeImport.VirtualProtect(dynJITAddr, (IntPtr)HOOK_SIZE_X86, AllocationProtect.PAGE_EXECUTE_READWRITE, out oldProt);
                    unsafe
                    {
                        byte* ptr = (byte*)dynJITAddr;

                        // mov eax, addr
                        // jmp eax
                        *(ptr) = 0xb8;
                        *(IntPtr*)(ptr + 1) = tramp;
                        *(ptr + 5) = 0xff;
                        *(ptr + 6) = 0xe0;
                    }
                    NativeImport.VirtualProtect(dynJITAddr, (IntPtr)HOOK_SIZE_X86, oldProt, out oldProt);
                }

                // 6. Store the invokable trampoline in the HookEntry (this is what's invoked in the CallOriginalFunction method from within each hook)
                hookEntry._PRIV_pTrampoline = trampDynFuncDelegate;
            }

            // 7. Finish setting up the hookEntry, adding the hook method and its priority to their respective lists
            // and, if necessary, re-sorting the lists
            hookEntry._PRIV_Hooks.Add(hookDel);
            hookEntry._PRIV_HookPriorities.Add(hookPriority);
            // if we're adding another hook to an already-seen target method, then we need to re-order the lists so that hook priorities are meaningful
            if (chainedHookStatus == EChainedHookerStatus.Existing)
            {
                // re-sort the hook priorities
                Logger.Log.Write("Sorting hook chain");
                if (hookEntry._PRIV_Hooks.Count != hookEntry._PRIV_HookPriorities.Count)
                {
                    Logger.Log.Write("The thing you knew would happen happened. You suck at programming. Nice error message btw good luck re-learning your own shit code and figuring out why this problem exists. This is your punishment.");
                    return null;
                }
                // insertion sort, hopefully keeping both lists in sync with each other
                int hookCount = hookEntry._PRIV_HookPriorities.Count - 1;
                int left = 0;
                for (int i = left, j = i; i < hookCount; j = ++i)
                {
                    Delegate hookOne = hookEntry._PRIV_Hooks[i + 1];
                    int priorityOne = hookEntry._PRIV_HookPriorities[i + 1];
                    int priorityTwo = hookEntry._PRIV_HookPriorities[j];
                    while (priorityOne < priorityTwo)
                    {
                        // swap
                        hookEntry._PRIV_HookPriorities[j + 1] = hookEntry._PRIV_HookPriorities[j];
                        hookEntry._PRIV_Hooks[j + 1] = hookEntry._PRIV_Hooks[j];
                        if (j-- == left)
                            break;
                        priorityTwo = hookEntry._PRIV_HookPriorities[j];
                    }
                    hookEntry._PRIV_HookPriorities[j + 1] = priorityOne;
                    hookEntry._PRIV_Hooks[j + 1] = hookOne;
                }
            }

            // 8. Enable the MinHook hook if this is the first time we've seen this target method
            if (chainedHookStatus == EChainedHookerStatus.New)
            {
                Logger.Log.Write("Enabling hook via MinHook");
                mhStatus = MinHook.EnableHook(targetMethodAddr);
                if (mhStatus != MinHook.MH_STATUS.MH_OK)
                {
                    Logger.Log.Write("Error enabling hook: " + mhStatus.ToString());
                    return null;
                }
            }
            // 9. Return the hook entry, which contains an invokable delegate of jmpToTrampDynFunc and some useful fields
            Logger.Log.Write("Successfully hooked managed function");
            return hookEntry;
        }
    }
}