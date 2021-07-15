using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NinMods.Utilities;
using NinMods.Hooking;
using System.Runtime.CompilerServices;

namespace NinMods
{
    public static class HookingUtils
    {
        public static MethodInfo GetMethodInfo(string qualifiedTypeName, string methodName, bool useTypeCache = false)
        {
            MethodInfo targetMethodInfo = null;
            if (useTypeCache)
            {
                List<MethodInfo> targetMethodInfos = TypeCache.GetMethodInfoMatchingName(qualifiedTypeName, methodName);
                if ((targetMethodInfos == null) || (targetMethodInfos.Count == 0))
                {
                    Logger.Log.Write("Could not find '" + qualifiedTypeName + "'");
                    return null;
                }
                if (targetMethodInfos.Count > 1)
                {
                    Logger.Log.Write("Found multiple matches for '" + qualifiedTypeName + "::" + methodName + "', defaulting to first one");
                }
                targetMethodInfo = targetMethodInfos[0];
            }
            else
            {
                Type targetType = Type.GetType(qualifiedTypeName);
                if (targetType == null)
                {
                    Logger.Log.Write("Could not find '" + qualifiedTypeName + "'");
                    return null;
                }
                targetMethodInfo = targetType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
                if (targetMethodInfo == null)
                {
                    Logger.Log.Write("Could not find '" + qualifiedTypeName + "::" + methodName + "'");
                }
            }
            return targetMethodInfo;
        }

        public static ManagedHooker.HookEntry InstallHook_Managed(string qualifiedTargetTypeName, string targetMethodName, string qualifiedHookTypeName, string hookMethodName, Type openDelType)
        {
            MethodInfo targetMethod = GetMethodInfo(qualifiedTargetTypeName, targetMethodName, false);
            MethodInfo hookMethod = GetMethodInfo(qualifiedHookTypeName, hookMethodName, false);
            if ((targetMethod == null) || (hookMethod == null))
            {
                return null;
            }
            try
            {
                RuntimeHelpers.PrepareMethod(hookMethod.MethodHandle);
                Logger.Log.Write($"Prepared hook method '{hookMethodName}");
                RuntimeHelpers.PrepareMethod(targetMethod.MethodHandle);
                Logger.Log.Write($"Prepared target method '{targetMethodName}");
            }
            catch(BadImageFormatException bife)
            {
                // assume this means the target method has some kind of encryption
                // run the target type's static cctor to see if that'll decrypt it, then try again
                throw bife;
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return null;
        }

        public static NativeStubHooker.StubInstance InstallHook_Native(string qualifiedTargetTypeName, string targetMethodName, string qualifiedHookTypeName, string hookMethodName, Type openDelType, out int hookIndex, out IntPtr oFuncAddr)
        {
            hookIndex = -1;
            oFuncAddr = IntPtr.Zero;
            MethodInfo targetMethod = GetMethodInfo(qualifiedTargetTypeName, targetMethodName, true);
            MethodInfo hookMethod = GetMethodInfo(qualifiedHookTypeName, hookMethodName, false);
            if ((targetMethod == null) || (hookMethod == null))
                return null;
            NativeStubHooker.StubInstance stubInstance = NativeStubHooker.HookMethod(targetMethod, hookMethod, openDelType, out hookIndex, out oFuncAddr);
            return stubInstance;
        }

        public static NativeStubHooker.StubInstance InstallHook_Native(MethodInfo targetMethod, MethodInfo hookMethod, Type openDelType, out int hookIndex, out IntPtr oFuncAddr)
        {
            NativeStubHooker.StubInstance stubInstance = NativeStubHooker.HookMethod(targetMethod, hookMethod, openDelType, out hookIndex, out oFuncAddr);
            return stubInstance;
        }
    }
}