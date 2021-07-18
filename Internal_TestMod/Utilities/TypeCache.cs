using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NinMods.Logging;

namespace NinMods.Utilities
{
    public static class TypeCache
    {
        static List<Type> m_AllTypes = new List<Type>();

        static void CacheAllTypesFromLoadedModules()
        {
            try
            {
                m_AllTypes.Clear();
                Logger.Log.Write("Re-populating cached types...");
                foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Logger.Log.Write("Saw loaded assembly '" + loadedAssembly.FullName + "'");
                    foreach (Module loadedModule in loadedAssembly.GetLoadedModules())
                    {
                        Logger.Log.Write("Saw loaded module '" + loadedModule.FullyQualifiedName + "'");
                        int prevCount = m_AllTypes.Count;
                        m_AllTypes.AddRange(loadedModule.GetTypes());
                        Logger.Log.Write("Cached " + (m_AllTypes.Count - prevCount).ToString() + " new types from module");
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log.WriteException(ex);
            }
            Logger.Log.Write("Done caching types (total: " + m_AllTypes.Count.ToString() + ")");
        }

        /// <summary>
        /// Searches for the type by name from all defined types in all loaded modules.
        /// </summary>
        /// <param name="typeName">The full type name excluding assembly information. eg; "IAmTesting.ModManager.ModManager"</param>
        /// <returns></returns>
        public static Type GetTypeByName(string typeName)
        {
            if (m_AllTypes.Count == 0)
            {
                CacheAllTypesFromLoadedModules();
            }
            // TO-DO:
            // optimize this
            foreach (Type type in m_AllTypes)
            {
                if (type.FullName == typeName)
                    return type;
            }
            return null;
        }

        /// <summary>
        /// Searches for the method existing on type by name from all defined types in all loaded modules.
        /// </summary>
        /// <param name="typeName">The full type name excluding assembly information. eg; "IAmTesting.ModManager.ModManager"</param>
        /// <param name="methodName">The name of the method. eg; just "DropItem" given the full path "RoR2.ChestBehavior.DropItem()"</param>
        /// <returns>A list of all matching methods. Take care to use the one you really want as parameters may be different.</returns>
        public static List<MethodInfo> GetMethodInfoMatchingName(string typeName, string methodName)
        {
            List<MethodInfo> matchingMethodsOnType = new List<MethodInfo>();
            Type targetType = NinMods.Utilities.TypeCache.GetTypeByName(typeName);
            if (targetType == null)
            {
                Logger.Log.Write("Could not find type '" + typeName + "'");
                return matchingMethodsOnType;
            }
            MethodInfo[] typeMethods = targetType.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in typeMethods)
            {
                Logger.Log.Write("Saw method '" + method.Name + "' on " + targetType.FullName);
                if (method.Name == methodName)
                    matchingMethodsOnType.Add(method);
            }
            return matchingMethodsOnType;
        }

        public static List<TMemberType> GetMemberInfoMatchingName<TMemberType>(string typeName, string memberName)
            where TMemberType : MemberInfo
        {
            List<TMemberType> matchingMembersOnType = new List<TMemberType>();
            Type targetType = NinMods.Utilities.TypeCache.GetTypeByName(typeName);
            if (targetType == null)
            {
                Logger.Log.Write("Could not find type '" + typeName + "'");
                return matchingMembersOnType;
            }
            MemberInfo[] typeMembers = targetType.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MemberInfo member in typeMembers)
            {
                Logger.Log.Write("Saw " + member.MemberType.ToString() + " '" + member.Name + "' on " + targetType.FullName);
                if (member.Name == memberName)
                    matchingMembersOnType.Add((TMemberType)member);
            }
            return matchingMembersOnType;
        }

        static TRetType GetValueOfField<TRetType>(object obj, FieldInfo field)
        {
            return (TRetType)field.GetValue(obj);
        }

        static TRetType GetValueOfProperty<TRetType>(object obj, PropertyInfo property, int index = 0)
        {
            TRetType value;
            if (property.GetIndexParameters().Length > 0)
            {
                value = (TRetType)property.GetValue(obj, new object[] { index });
            }
            else
            {
                value = (TRetType)property.GetValue(obj, null);
            }
            return value;
        }

        public static TRetType GetValue<TRetType, TMemberType>(object obj, TMemberType memberInfo, int index = 0)
            where TMemberType : MemberInfo
        {
            if (memberInfo is PropertyInfo)
            {
                return GetValueOfProperty<TRetType>(obj, (PropertyInfo)(MemberInfo)memberInfo, index);
            }
            else
            {
                return GetValueOfField<TRetType>(obj, (FieldInfo)(MemberInfo)memberInfo);
            }
        }

        static void SetValueOfField<TValType>(object obj, FieldInfo field, TValType value)
        {
            field.SetValue(obj, value);
        }

        static void SetValueOfProperty<TValType>(object obj, PropertyInfo property, TValType value, int index = 0)
        {
            if (property.GetIndexParameters().Length > 0)
            {
                property.SetValue(obj, value, new object[] { index });
            }
            else
            {
                property.SetValue(obj, value, null);
            }
        }

        public static void SetValue<TValType, TMemberType>(object obj, TMemberType memberInfo, TValType value, int index = 0)
            where TMemberType : MemberInfo
        {
            if (memberInfo is PropertyInfo)
            {
                SetValueOfProperty<TValType>(obj, (PropertyInfo)(MemberInfo)memberInfo, value, index);
            }
            else
            {
                SetValueOfField<TValType>(obj, (FieldInfo)(MemberInfo)memberInfo, value);
            }
        }

        public static TRetType CallMethod<TRetType>(object obj, MethodInfo methodInfo, object[] args)
        {
            return (TRetType)methodInfo.Invoke(obj, args);
        }
    }
}