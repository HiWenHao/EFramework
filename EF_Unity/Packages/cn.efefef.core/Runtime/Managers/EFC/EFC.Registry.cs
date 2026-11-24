/*
 * ================================================
 * Describe:      EFC 单例注册中心分部：注册/注销/查询、依赖解析、优先级与 Mono 反射适配。
 *                主机与生命周期见 EFC.cs，状态容器见 EFC.State.cs。
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-17 16:31:29
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-24
 * Version:       2.0
 * ===============================================
 */

using EasyFramework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed partial class EFC
{
    // 单例注册中心：所有写操作在主线程、锁内完成；用户回调在锁外执行
    private static class Registry
    {
        // 判重并写入注册表，随后的 Init 由调用方在锁外执行
        internal static bool Prepare(ISingleton item, out bool registered)
        {
            registered = false;
            if (item == null)
            {
                D.Error(LogTag + "Register 收到 null，已忽略。");
                return false;
            }

            if (!IsMainThread())
            {
                D.Error(LogTag + "Register 必须在主线程调用；工作线程请使用 EFC.RunOnMainThread。");
                return false;
            }

            // 必须在 CreateHost / EnsureDependencies 之前拒绝：否则依赖解析已创建 Unity 对象后才被拒，
            // 那些对象既不会进注册表也不会被 Quit/Destroy，成为无主残留
            lock (LockObj)
            {
                if (_quitting || !_inRunning)
                {
                    D.Error(LogTag + "框架未运行（请先调用 EFC.Initialize），拒绝注册：" + item.GetType().Name);
                    return false;
                }
            }

            Type type = item.GetType();
            if (Attribute.IsDefined(type, typeof(IgnoreAutoRegisterAttribute), true))
            {
                D.Error(LogTag + "IgnoreAutoRegister 类型不进入注册表，请使用 Instance 访问：" + type.Name);
                return false;
            }

            // 提前确保宿主与父节点有效，避免锁内访问 UnityEngine 对象
            CreateHost();

            if (!EnsureDependencies(type)) return false;

            bool isManager = Attribute.IsDefined(type, typeof(ManagerAttribute), false);
            int order = GetOrder(type);

            lock (LockObj)
            {
                if (_quitting)
                {
                    D.Error(LogTag + "退出过程中拒绝注册：" + type.Name);
                    return false;
                }

                if (!_inRunning)
                {
                    D.Error(LogTag + "框架未运行，注册被拒绝：" + type.Name);
                    return false;
                }

                if (EfcState.SingletonMap.ContainsKey(type)) return true;

                // Unity 对象 fake-null 校验必须在写表之前
                if (item is MonoBehaviour mono && mono == null)
                {
                    D.Error(LogTag + "待注册单例的 MonoBehaviour 已销毁：" + type.Name);
                    return false;
                }

                (isManager ? EfcState.Managers : EfcState.Singletons).Add(item);
                AddToPhases(item, order);
                EfcState.SingletonMap[type] = item;
                registered = true;
            }

            return true;
        }

        // Prepare 成功后的锁外步骤：SetParent + Init；失败时完整回滚
        internal static void Finish(ISingleton item, bool registered, bool isManager)
        {
            if (registered)
            {
                try
                {
                    if (item is MonoBehaviour mono)
                        mono.transform.SetParent(isManager ? Managers : Singleton, false);

                    // 先恢复驱动再 Init：软退出重入时 Init 内读取 IsRunning 应为 true，
                    // 与首次启动（Initialize 先置位）保持一致；恢复失败也不能阻断注册
                    try
                    {
                        ResumeDriver();
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                    }

                    item.Init();
                }
                catch (Exception e)
                {
                    LogException(e);
                    Rollback(item, item.GetType(), isManager);
                }
            }
        }

        // Init 失败的回滚路径：Quit 可能被二次抛出，必须逐项隔离
        private static void Rollback(ISingleton item, Type type, bool isManager)
        {
            lock (LockObj)
            {
                if (EfcState.SingletonMap.TryGetValue(type, out var current) && ReferenceEquals(current, item))
                {
                    EfcState.SingletonMap.Remove(type);
                    (isManager ? EfcState.Managers : EfcState.Singletons).Remove(item);
                    RemoveFromPhases(item);
                }
            }

            try
            {
                item.Quit();
            }
            catch (Exception e)
            {
                LogException(e);
            }

            // 不在此处销毁：Init 失败的可能是用户场景里预置的对象；
            // 是否销毁由创建方决定（MonoSingleton.Instance 会在注册失败时自行处理）
        }

        // 注销：先从注册表移除以保证幂等，再在锁外调用 Quit 与 Destroy
        internal static bool Unregister(ISingleton item, bool destroyGameObject)
        {
            if (item == null)
            {
                D.Error(LogTag + "Unregister 收到 null，已忽略。");
                return false;
            }

            if (!IsMainThread())
            {
                D.Error(LogTag + "Unregister 必须在主线程调用；工作线程请使用 EFC.RunOnMainThread。");
                return false;
            }

            Type type = item.GetType();
            bool isManager = Attribute.IsDefined(type, typeof(ManagerAttribute), false);

            lock (LockObj)
            {
                if (!EfcState.SingletonMap.TryGetValue(type, out var registered) || !ReferenceEquals(registered, item))
                    return false;

                EfcState.SingletonMap.Remove(type);
                (isManager ? EfcState.Managers : EfcState.Singletons).Remove(item);
                RemoveFromPhases(item);
            }

            try
            {
                item.Quit();
            }
            catch (Exception e)
            {
                LogException(e);
            }

            if (destroyGameObject && item is MonoBehaviour mono && mono != null && mono.gameObject != null)
            {
                // 先失活：Destroy 延迟到帧末，期间 FindObjectsOfType 仍可能复用该实例
                mono.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(mono.gameObject);
            }

            return true;
        }

        internal static bool TryGet<T>(out T singleton) where T : ISingleton
        {
            Type type = typeof(T);
            lock (LockObj)
            {
                if (EfcState.SingletonMap.TryGetValue(type, out var inst) && !IsDestroyed(inst))
                {
                    singleton = (T)inst;
                    return true;
                }

                // 兼容接口/基类查询：命中多个实现时拒绝返回，避免依赖 Dictionary 遍历顺序
                ISingleton match = null;
                foreach (var pair in EfcState.SingletonMap)
                {
                    if (pair.Key == type || !type.IsInstanceOfType(pair.Value) || IsDestroyed(pair.Value))
                        continue;

                    if (match != null)
                    {
                        D.Error(LogTag + "类型 " + type.Name + " 有多个单例实现，无法确定唯一实例：" +
                                match.GetType().Name + " / " + pair.Value.GetType().Name);
                        singleton = default;
                        return false;
                    }

                    match = pair.Value;
                }

                if (match != null)
                {
                    singleton = (T)match;
                    return true;
                }
            }

            singleton = default;
            return false;
        }

        internal static List<ISingleton> SnapshotAll()
        {
            lock (LockObj)
            {
                var snapshot = new List<ISingleton>(EfcState.Managers.Count + EfcState.Singletons.Count);
                snapshot.AddRange(EfcState.Managers);
                snapshot.AddRange(EfcState.Singletons);
                return snapshot;
            }
        }

        private static bool IsDestroyed(object instance) => instance is UnityEngine.Object unityObject && unityObject == null;

        internal static bool TryBeginCreation(Type type)
        {
            lock (LockObj)
            {
                if (EfcState.CreatingTypes.Contains(type)) return false;
                EfcState.CreatingTypes.Add(type);
                return true;
            }
        }

        internal static void EndCreation(Type type)
        {
            lock (LockObj)
            {
                EfcState.CreatingTypes.Remove(type);
            }
        }

        // 依赖解析失败必须向上传播，阻止残缺实例进入注册表
        internal static bool EnsureDependencies(Type managerType)
        {
            lock (LockObj)
            {
                if (EfcState.SingletonMap.ContainsKey(managerType)) return true;
            }

            lock (LockObj)
            {
                if (!EfcState.ResolvingSet.Add(managerType))
                {
                    D.Error(LogTag + "检测到循环依赖：" + managerType.Name);
                    return false;
                }
            }

            try
            {
                // inherit=true：派生类应继承基类声明的依赖
                object[] attributes = managerType.GetCustomAttributes(typeof(DependencyAttribute), true);
                for (int i = 0; i < attributes.Length; i++)
                {
                    if (!(attributes[i] is DependencyAttribute depAttr)) continue;
                    Type depType = depAttr.DependencyType;
                    if (depType == null || depType == managerType || !typeof(ISingleton).IsAssignableFrom(depType))
                        continue;

                    if (Attribute.IsDefined(depType, typeof(IgnoreAutoRegisterAttribute), true))
                    {
                        D.Error(LogTag + "依赖 " + depType.Name + " 标记了 IgnoreAutoRegister，不能作为 " + managerType.Name + " 的依赖。");
                        continue;
                    }

                    if (!EnsureDependencies(depType)) return false;
                    if (!CreateByType(depType)) return false;
                }

                return true;
            }
            finally
            {
                lock (LockObj)
                {
                    EfcState.ResolvingSet.Remove(managerType);
                }
            }
        }

        private static bool CreateByType(Type type)
        {
            if (type.IsValueType || !typeof(ISingleton).IsAssignableFrom(type))
            {
                D.Error(LogTag + "依赖类型必须实现 ISingleton 且为引用类型：" + type.Name);
                return false;
            }

            // 重复依赖或并发下已被注册：直接复用，避免创建无人认领的孤儿实例
            lock (LockObj)
            {
                if (EfcState.SingletonMap.ContainsKey(type)) return true;
            }

            if (typeof(MonoBehaviour).IsAssignableFrom(type))
                return GetOrCreateMonoSingleton(type);

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                D.Error(LogTag + "依赖类型必须有无参构造：" + type.Name);
                return false;
            }

            object created;
            try
            {
                // 直接实例化，避免对泛型方法做反射（方法重命名时静默失败）
                created = Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                LogException(e);
                return false;
            }

            return RegisterPlain((ISingleton)created);
        }

        // 普通 C# 单例的依赖创建路径：复用公开的注册流程
        private static bool RegisterPlain(ISingleton instance)
        {
            bool registered;
            if (!Prepare(instance, out registered)) return false;
            Finish(instance, registered, Attribute.IsDefined(instance.GetType(), typeof(ManagerAttribute), false));
            return true;
        }

        internal static object GetMonoSingletonInstance(Type type)
        {
            Func<object> getter;
            lock (LockObj)
            {
                if (!EfcState.MonoSingletonGetters.TryGetValue(type, out getter))
                {
                    PropertyInfo property = type.GetProperty(InstancePropertyName,
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (property == null || !property.CanRead || property.GetMethod == null)
                    {
                        D.Error(LogTag + "MonoBehaviour 单例缺少静态 Instance 属性：" + type.Name);
                        return null;
                    }

                    try
                    {
                        getter = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), property.GetMethod);
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        return null;
                    }

                    EfcState.MonoSingletonGetters[type] = getter;
                }
            }

            object instance;
            try
            {
                instance = getter();
            }
            catch (Exception e)
            {
                LogException(e);
                return null;
            }

            if (instance == null)
            {
                D.Error(LogTag + "静态 Instance 返回 null：" + type.Name);
                return null;
            }

            // IgnoreAutoRegister 类型按设计不进注册表，不应告警
            if (!Attribute.IsDefined(type, typeof(IgnoreAutoRegisterAttribute), true))
            {
                lock (LockObj)
                {
                    if (instance is ISingleton && !EfcState.SingletonMap.ContainsKey(type))
                        D.Warning(LogTag + "MonoSingleton " + type.Name + " 已创建但未注册，请确认 Instance 调用了 EFC.Register。");
                }
            }

            return instance;
        }

        // 依赖创建的成功判据必须是"已进入注册表"，而不是 getter 返回了非 null 对象
        private static bool GetOrCreateMonoSingleton(Type type)
        {
            if (GetMonoSingletonInstance(type) == null) return false;
            if (Attribute.IsDefined(type, typeof(IgnoreAutoRegisterAttribute), true)) return false;
            lock (LockObj)
            {
                return EfcState.SingletonMap.ContainsKey(type);
            }
        }
    }
}
