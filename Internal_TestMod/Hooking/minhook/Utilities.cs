using System;
using System.Collections.Generic;
using System.Text;
//using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NinMods.Hooking.Utilities
{
    public class Utility
    {
        public static MethodInfo GetMethodByName(Type type, string name)
        {
            MethodInfo[] typeMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in typeMethods)
            {
                if (method.Name == name)
                {
                    return method;
                }
            }
            return null;
        }

        public static IntPtr GetMethodAddrByName(Type type, string name, out MethodInfo methodInfo)
        {
            MethodInfo[] typeMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in typeMethods)
            {
                if (method.Name == name)
                {
                    methodInfo = method;
                    try
                    {
                        RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);
                    }
                    catch (BadImageFormatException bife)
                    {
                        // silently log the error, but continue execution after. this doesn't necessarily indicate failure.
                        Logger.Log.Write("NinMods.Hooking.Utilities.Utility", "GetMethodAddrByName", $"BadImageFormatException when preparing target method '{name}' via RuntimeHelpers.", Logger.ELogType.Error);
                    }
                    return methodInfo.MethodHandle.GetFunctionPointer();
                }
            }
            methodInfo = null;
            return IntPtr.Zero;
        }
    }
}
