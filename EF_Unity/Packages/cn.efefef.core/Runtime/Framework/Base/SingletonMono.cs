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

                // QuitGames 是完整卸载（宿主一并销毁）；只有显式 Initialize 之后才允许创建单例。
                if (!EFC.CanCreateSingleton)
                {
                    D.Error(EFC.LogTag + "框架未运行，拒绝创建 MonoSingleton " + typeof(T).Name +
                            "。请先调用 EFC.Initialize()。");
                    return default;
                }

                // IgnoreAutoRegister 不进注册表：本地缓存保证单实例，语义对齐 EFC.GetOrCreate<T>()
                if (Attribute.IsDefined(typeof(T), typeof(IgnoreAutoRegisterAttribute)))
                    return IgnoredCache.Value;

                // 依赖解析可能回环到本 T（此时尚未写表）：fail-fast 而非无限递归
                if (CreationGuard.Creating)
                    throw new InvalidOperationException(
                        $"{EFC.LogTag} Re-entrant creation of {typeof(T).Name}. Check for circular dependencies.");

                CreationGuard.Creating = true;
                try
                {
                    var instance = CreateInstance(out bool createdNew);
                    // 以注册表为唯一真源：Init 失败回滚后 instance 已被判定不可用，不得返回
                    if (EFC.TryGetSingleton(out T registered))
                        return registered;

                    // 回滚路径未销毁对象，此处由创建方清理刚生成的 GameObject，避免场景残留。
                    // 仅销毁本类新建的对象；复用场景中预置对象时保留原物。
                    if (createdNew && instance != null && instance.gameObject != null)
                    {
                        instance.gameObject.SetActive(false);
                        Destroy(instance.gameObject);
                    }

                    D.Error(EFC.LogTag + "MonoSingleton " + typeof(T).Name + " 创建后未能注册，已返回 null。");
                    return null;
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
            private static int _generation = -1;
            private static Lazy<T> _lazy;

            internal static T Value
            {
                get
                {
                    // 关闭域重载时按会话代数重建，避免拿到上一 Play 会话的 fake-null 实例
                    if (_generation != EFC.Generation)
                    {
                        _lazy = new Lazy<T>(() => CreateInstance(out _));
                        _generation = EFC.Generation;
                    }

                    return _lazy.Value;
                }
            }
        }

        private static T CreateInstance(out bool createdNew)
        {
            createdNew = false;
            T instance = null;
            var existing = FindObjectsOfType<T>();
            for (int i = 0; i < existing.Length; i++)
            {
                T candidate = existing[i];
                // 跳过已销毁或已失活的实例：Destroy 延迟到帧末，期间仍可能被 FindObjectsOfType 找到
                if (candidate == null || !candidate.gameObject.activeInHierarchy)
                {
                    if (candidate != null)
                        Destroy(candidate.gameObject);
                    continue;
                }

                if (instance == null)
                    instance = candidate;
                else
                    Destroy(candidate.gameObject);
            }

            if (instance == null)
            {
                instance = new GameObject().AddComponent<T>();
                createdNew = true;
            }

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
                ? EFC.Manager
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