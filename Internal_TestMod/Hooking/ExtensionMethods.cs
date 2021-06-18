using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NinMods.Hooking
{
    public static class ExtensionMethods
    {
        private static HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(sbyte),
            typeof(byte),
            typeof(bool),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(float),
            typeof(double)
        };

        public static bool IsNumericType(this Type t)
        {
            return NumericTypes.Contains(t);
        }
    }
}
