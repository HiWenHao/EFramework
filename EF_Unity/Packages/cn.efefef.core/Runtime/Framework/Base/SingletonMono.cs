/*
 * ================================================
 * Describe:        The class is monobehavior singleton base.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-23
 * ScriptVersion:   1.1
 * ===============================================
 */

using System;
using UnityEngine;

namespace EasyFramework
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>, ISingleton
    {
        /// <summary>
        /// 全局唯一实例。以 EFC 注册表为唯一真源：已注册则取，未注册则复用场景对象或新建。
        /// <para>Global instance. EFC registry is the single source of truth.</para>
        /// </summary>
        public static T Instance
        {
            get
            {
                if (EFC.TryGetSingleton(out T existing))
                    return existing;

                // IgnoreAutoRegister 不进注册表：本地缓存保证单实例，语义对齐 EFC.GetOrCreate<T>()
                if (Attribute.IsDefined(typeof(T), typeof(IgnoreAutoRegisterAttribute)))
                    return IgnoredCache.Value.Value;

                // 依赖解析可能回环到本 T（此时尚未写表）：fail-fast 而非无限递归
                if (CreationGuard.Creating)
                    throw new InvalidOperationException(
                        $"[EFC] Re-entrant creation of {typeof(T).Name}. Check for circular dependencies.");

                CreationGuard.Creating = true;
                try
                {
                    var instance = CreateInstance();
                    // Unity 对象创建只能在主线程；此处兜底回环/并发下已被注册的情形，以注册表为准
                    return EFC.TryGetSingleton(out T registered) ? registered : instance;
                }
                finally
                {
                    CreationGuard.Creating = false;
                }
            }
        }

        // 每类型独立；仅作同线程重入兜底（Unity 对象创建限主线程，跨线程靠注册表判重）
        private static class CreationGuard
        {
            [ThreadStatic] internal static bool Creating;
        }

        // IgnoreAutoRegister 的本地单实例缓存（不进 EFC 注册表）
        private static class IgnoredCache
        {
            internal static readonly Lazy<T> Value = new(CreateInstance);
        }

        private static T CreateInstance()
        {
            T instance;
            var existing = FindObjectsOfType<T>();
            if (existing.Length > 0)
            {
                for (int i = 1; i < existing.Length; i++)
                    Destroy(existing[i].gameObject);
                instance = existing[0];
            }
            else
                instance = new GameObject().AddComponent<T>();

            instance.name = $"--------------- [ {typeof(T).Name} ] ---------------";
            RegisterAndInit(instance);
            return instance;
        }

        private static void RegisterAndInit(T instance)
        {
            // IgnoreAutoRegister 不进注册表，生命周期自行管理
            if (Attribute.IsDefined(typeof(T), typeof(IgnoreAutoRegisterAttribute)))
                return;

            instance.transform.SetParent(Attribute.IsDefined(typeof(T), typeof(ManagerAttribute))
                ? EFC.Managers
                : EFC.Singleton);

            EFC.Register(instance);
        }

        protected virtual void OnDestroy()
        {
            if (!Attribute.IsDefined(typeof(T), typeof(IgnoreAutoRegisterAttribute)))
                EFC.Unregister((ISingleton)this, false);
        }
    }
}