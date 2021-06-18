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
    }
}
