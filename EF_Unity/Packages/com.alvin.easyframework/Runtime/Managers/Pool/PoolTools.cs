/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-09 11:25:29
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-09 11:25:29
 * ScriptVersion: 0.1
 * ===============================================
*/

using System;
using UnityEngine;

namespace EasyFramework.Managers.Pool
{
	/// <summary>
	/// 对象池相关工具类
	/// </summary>
    public static class PoolTools
    {
        // 常见的Unity内置组件命名空间
        static string[] _builtInNamespaces =
        {
            "UnityEngine.",
            "UnityEngine.UI.",
            "UnityEngine.AI.",
            "UnityEngine.Animations.",
            "UnityEngine.Audio.",
            "UnityEngine.Networking."
        };
        
        /// <summary>
        /// 检查类型是否是可挂载的MonoBehaviour脚本
        /// </summary>
        public static bool IsMountableMonoType(Type type)
        {
            if (type == null)
                return false;

            if (!type.IsClass)
                return false;

            if (!typeof(MonoBehaviour).IsAssignableFrom(type))
                return false;

            if (type.IsAbstract)
                return false;

            if (IsUnityBuiltInComponent(type))
                return false;

            if (type.IsGenericTypeDefinition || type.ContainsGenericParameters)
                return false;

            if (HasDisallowMultipleComponentAttribute(type) &&
                UnityEngine.Object.FindObjectsOfType(type).Length > 0)
                return false;

            return true;
        }

        /// <summary>
        /// 检查是否为Unity内置组件
        /// </summary>
        private static bool IsUnityBuiltInComponent(Type type)
        {
            string typeNamespace = type.Namespace ?? "";
            foreach (var ns in _builtInNamespaces)
            {
                if (typeNamespace.StartsWith(ns))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 检查是否有DisallowMultipleComponent属性
        /// </summary>
        private static bool HasDisallowMultipleComponentAttribute(Type type)
        {
            return Attribute.IsDefined(type, typeof(DisallowMultipleComponent));
        }
    }
}
