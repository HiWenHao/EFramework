/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 09:57:33
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-10 09:57:33
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// Unity对象池基类
    /// </summary>
    public abstract class UnityPoolBase<T> : MonoBehaviour, IPool<T> where T : class
    {
        #region Public Property - 公开属性

        public string PoolName => Config.poolName;

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 可用数量
        /// </summary>
        public int AvailableCount { get; protected set; }

        /// <summary>
        /// 激活数量
        /// </summary>
        public int ActiveCount { get; protected set; }

        /// <summary>
        /// 总生成数量
        /// </summary>
        public int TotalCreated { get; protected set; }

        /// <summary>
        /// 总回收数量
        /// </summary>
        public int TotalRecycled { get; protected set; }

        /// <summary>
        /// 峰值数量
        /// </summary>
        public int PeakActiveCount { get; protected set; }

        #endregion

        #region Public Event - 公开事件

        public event EventHandler<PoolEventArgs> OnObjectCreated;
        public event EventHandler<PoolEventArgs> OnObjectGet;
        public event EventHandler<PoolEventArgs> OnObjectRecycle;
        public event EventHandler<PoolEventArgs> OnObjectDestroyed;
        public event EventHandler<PoolEventArgs> OnPoolCleared;
        public event EventHandler<PoolEventArgs> OnPoolDestroyed;

        #endregion

        #region Protected Field - 受保护字段

        /// <summary>
        /// 当前池配置
        /// </summary>
        protected PoolConfig Config = PoolConfig.Default;

        /// <summary>
        /// 对象工厂
        /// </summary>
        protected IObjectFactory<T> ObjectFactory;

        /// <summary>
        /// 全部对象
        /// </summary>
        protected HashSet<T> AllObjects;

        /// <summary>
        /// 可用对象
        /// </summary>
        protected Stack<T> AvailableObjects;

        /// <summary>
        /// 待使用对象
        /// </summary>
        protected Dictionary<T, DateTime> IdleObjects;

        /// <summary>
        /// 已激活对象
        /// </summary>
        protected Dictionary<T, DateTime> ActiveObjects;

        /// <summary>
        /// 最后一次清理时间
        /// </summary>
        protected DateTime LastCleanupTime;

        #endregion

        #region Public Function - 公共函数

        public virtual void Initialize(T prefab, PoolConfig config)
        {
            if (IsInitialized)
            {
                if (Config.enableShowDebugInfo)
                    D.Warning($"Cannot change factory after pool [ {Config.poolName} ]  is initialized...");
                return;
            }

            if (null == ObjectFactory)
            {
                if (Config.enableShowDebugInfo)
                    D.Error($"Object pool [ {Config.poolName} ]  has no factory!...");

                return;
            }

            Config = config;

            AllObjects = new HashSet<T>();
            AvailableObjects = new Stack<T>();
            IdleObjects = new Dictionary<T, DateTime>();
            ActiveObjects = new Dictionary<T, DateTime>();


            if (Config is { lazyLoading: false, preloadCount: > 0 })
            {
                PreloadObjects(Config.preloadCount);
            }

            IsInitialized = true;
            LastCleanupTime = DateTime.Now;

            if (Config is { enableAutoCleanup: true, cleanupInterval: > 0 })
            {
                EF.StartCoroutines(AutoCleanupRoutine());
            }
        }

        public void Dispose()
        {
            if (!IsInitialized)
                return;
            IsInitialized = false;

            ClearAll();

            AllObjects = null;
            ActiveObjects = null;
            lock (AvailableObjects)
            {
                IdleObjects = null;
                AvailableObjects = null;
            }

            ObjectFactory = null;
            OnPoolDestroyed?.Invoke(this, PoolEventArgs.Create(Config.poolName, PoolEventType.PoolDestroyed));

            EF.StopCoroutines(AutoCleanupRoutine());
        }

        public virtual void ClearAll()
        {
            if (!IsInitialized)
                return;

            lock (AvailableObjects)
            {
                // 清理可用对象
                while (AvailableObjects.Count > 0)
                    DestroyObject(AvailableObjects.Pop());

                AvailableObjects.Clear();
                IdleObjects.Clear();
                AvailableCount = 0;
            }

            foreach (var obj in ActiveObjects.Keys)
            {
                // 这里只是从追踪中移除，不实际销毁正在使用的对象
                ActiveObjects.Remove(obj);
                AllObjects.Remove(obj);
            }

            ActiveCount = 0;
            ActiveObjects.Clear();
            AllObjects.Clear();

            // 触发事件
            OnPoolCleared?.Invoke(this, PoolEventArgs.Create(Config.poolName, PoolEventType.PoolCleared));

            if (Config.enableShowDebugInfo)
                D.Log($"Pool [ {Config.poolName} ]  cleared...");
        }

        public virtual T Get()
        {
            if (!IsInitialized)
            {
                if (Config.enableShowDebugInfo)
                    D.Warning($"Object pool [ {Config.poolName} ]  is not initialized...");
                return null;
            }

            T obj = null;

            lock (AvailableObjects)
            {
                if (AvailableObjects.Count > 0)
                {
                    obj = AvailableObjects.Pop();
                    IdleObjects.Remove(obj);
                    AvailableCount--;
                }
            }

            if (obj == null)
            {
                if (TotalCreated >= Config.maxPoolSize)
                {
                    if (Config.enableShowDebugInfo)
                        D.Warning($"Pool [ {Config.poolName} ]  reached max capacity ({Config.maxPoolSize})...");
                    return null;
                }

                obj = CreateNewObject();
                if (obj == null)
                {
                    if (Config.enableShowDebugInfo)
                        D.Error($"Failed to create object in pool [ {Config.poolName} ] ...");
                    return null;
                }
            }

            // 激活对象
            if (ActivateObject(obj))
            {
                ActiveCount++;
                ActiveObjects[obj] = DateTime.Now;

                // 触发事件
                OnObjectGet?.Invoke(this, PoolEventArgs.Create(Config.poolName, PoolEventType.Get, obj));

                if (Config.enableShowDebugInfo)
                    D.Log($"Object spawned from pool [ {Config.poolName} ] : {GetObjectName(obj)}...");

                return obj;
            }

            return null;
        }

        public virtual bool Recycle(T obj)
        {
            if (obj == null)
            {
                if (Config.enableShowDebugInfo)
                    D.Warning($"Attempted to recycle null object to pool [ {Config.poolName} ] ...");
                return false;
            }

            if (!IsInitialized)
            {
                if (Config.enableShowDebugInfo)
                    D.Warning($"Object pool [ {Config.poolName} ]  is not initialized...");
                return false;
            }

            if (!AllObjects.Contains(obj))
            {
                if (Config.enableShowDebugInfo)
                    D.Warning($"Object does not belong to pool [ {Config.poolName} ] : {GetObjectName(obj)}...");
                return false;
            }

            if (Config.enableReuseCheck)
            {
                lock (AvailableObjects)
                {
                    if (AvailableObjects.Contains(obj) || IdleObjects.ContainsKey(obj))
                    {
                        if (Config.enableShowDebugInfo)
                            D.Error($"Object already in pool [ {Config.poolName} ] : {GetObjectName(obj)}...");

                        return false;
                    }
                }
            }

            if (AvailableCount >= Config.maxPoolSize)
            {
                if (Config.enableShowDebugInfo)
                    D.Warning($"Pool [ {Config.poolName} ]  is full, cannot recycle object...");

                DestroyObject(obj);
                return false;
            }

            // 停用对象
            if (!DeactivateObject(obj))
                return false;

            // 更新统计
            ActiveCount--;
            ActiveObjects.Remove(obj);

            // 放回池中
            lock (AvailableObjects)
            {
                AvailableObjects.Push(obj);
                IdleObjects[obj] = DateTime.Now;
                AvailableCount++;
            }

            TotalRecycled++;

            // 触发事件
            OnObjectRecycle?.Invoke(this, PoolEventArgs.Create(Config.poolName, PoolEventType.Recycle, obj));

            if (Config.enableShowDebugInfo)
                D.Log($"Object recycled to pool [ {Config.poolName} ] : {GetObjectName(obj)}...");

            return true;
        }

        public virtual PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                PoolName = Config.poolName,
                AvailableCount = AvailableCount,
                ActiveCount = ActiveCount,
                TotalCreated = TotalCreated,
                TotalRecycled = TotalRecycled,
                PeakActiveCount = PeakActiveCount,
                AverageHoldTime = CalculateAverageHoldTime(),
                IsInitialized = IsInitialized
            };
        }

        public virtual void SetFactory(IObjectFactory<T> objectFactory)
        {
            if (IsInitialized)
            {
                if (Config.enableShowDebugInfo)
                    D.Warning($"Cannot change factory after pool [ {Config.poolName} ]  is initialized...");
                return;
            }

            ObjectFactory = objectFactory;
        }

        /// <summary>
        /// 清理空闲对象
        /// </summary>
        public virtual void CleanupIdleObjects()
        {
            if (!IsInitialized || Config.maxIdleTime <= 0)
                return;

            lock (AvailableObjects)
            {
                var objectsToRemove = new List<T>();
                var now = DateTime.Now;

                foreach (var kvp in IdleObjects)
                {
                    var idleTime = now - kvp.Value;
                    if (idleTime.TotalSeconds > Config.maxIdleTime)
                    {
                        objectsToRemove.Add(kvp.Key);
                    }
                }

                foreach (var obj in objectsToRemove)
                {
                    if (AvailableObjects.Contains(obj))
                    {
                        var tempStack = new Stack<T>();
                        while (AvailableObjects.Count > 0)
                        {
                            var item = AvailableObjects.Pop();
                            if (!item.Equals(obj))
                            {
                                tempStack.Push(item);
                            }
                        }

                        while (tempStack.Count > 0)
                        {
                            AvailableObjects.Push(tempStack.Pop());
                        }

                        IdleObjects.Remove(obj);
                        AllObjects.Remove(obj);
                        DestroyObject(obj);
                        AvailableCount--;
                        TotalCreated--;
                    }
                }

                if (objectsToRemove.Count > 0 && Config.enableShowDebugInfo)
                {
                    D.Log($"Cleaned up {objectsToRemove.Count} idle objects from pool [ {Config.poolName} ] ...");
                }
            }
        }

        #endregion

        #region Protected function - 受保护函数

        /// <summary>
        /// 创建新对象
        /// </summary>
        /// <returns>新创建的对象</returns>
        protected virtual T CreateNewObject()
        {
            if (ObjectFactory == null)
            {
                if (Config.enableShowDebugInfo)
                    D.Error($"No factory set for pool [ {Config.poolName} ] ...");
                return null;
            }

            var obj = ObjectFactory.Create();
            if (obj == null)
            {
                if (Config.enableShowDebugInfo)
                    D.Error($"Factory failed to create object for pool [ {Config.poolName} ] ...");
                return null;
            }

            AllObjects.Add(obj);
            TotalCreated++;

            // 触发事件
            OnObjectCreated?.Invoke(this, PoolEventArgs.Create(Config.poolName, PoolEventType.Created, obj));

            if (Config.enableShowDebugInfo)
                D.Log($"Object created in pool [ {Config.poolName} ] : {GetObjectName(obj)}...");

            return obj;
        }

        /// <summary>
        /// 销毁对象
        /// </summary>
        /// <param name="obj">被销毁的对象</param>
        protected virtual void DestroyObject(T obj)
        {
            if (obj == null)
                return;

            ObjectFactory?.Dispose(obj);

            AllObjects.Remove(obj);
            IdleObjects.Remove(obj);
            ActiveObjects.Remove(obj);

            // 触发事件
            OnObjectDestroyed?.Invoke(this, PoolEventArgs.Create(Config.poolName, PoolEventType.Destroyed, obj));

            if (Config.enableShowDebugInfo)
                D.Log($"Object destroyed in pool [ {Config.poolName} ] : {GetObjectName(obj)}...");
        }

        /// <summary>
        /// 激活对象
        /// </summary>
        /// <param name="obj">需要被激活的对象</param>
        /// <returns></returns>
        protected virtual bool ActivateObject(T obj)
        {
            if (obj == null)
                return false;

            ObjectFactory?.ResetState(obj);

            if (obj is GameObject gameObj)
            {
                gameObj.SetActive(true);
                return true;
            }

            if (obj is Component component)
            {
                component.gameObject.SetActive(true);

                if (component is IPoolable poolable)
                    poolable.OnGet();

                return true;
            }

            return false;
        }

        /// <summary>
        /// 停用对象
        /// </summary>
        /// <param name="obj">需要被停用的对象</param>
        /// <returns>是否停用成功</returns>
        protected virtual bool DeactivateObject(T obj)
        {
            if (obj == null)
                return false;

            if (obj is GameObject gameObj)
            {
                gameObj.SetActive(false);
                return true;
            }

            if (obj is Component component)
            {
                // 调用 IPoolable 接口
                if (component is IPoolable poolable)
                    poolable.OnRecycle();

                component.gameObject.SetActive(false);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取对象名称
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>对象名称</returns>
        protected virtual string GetObjectName(T obj)
        {
            if (obj is GameObject gameObj)
                return gameObj.name;
            if (obj is Component component)
                return component.name;
            return obj.ToString();
        }

        /// <summary>
        /// 预加载对象
        /// </summary>
        /// <param name="count">预加载数量</param>
        protected void PreloadObjects(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = CreateNewObject();
                if (obj == null)
                {
                    if (Config.enableShowDebugInfo)
                        D.Warning(
                            "The object creation failed. Please check if the quantity has reached the limit, or there is a problem with the object factory...");
                    return;
                }

                DeactivateObject(obj);
                lock (AvailableObjects)
                {
                    AvailableObjects.Push(obj);
                    IdleObjects[obj] = DateTime.Now;
                    AvailableCount++;
                }
            }
        }

        /// <summary>
        /// 自动清理协程
        /// </summary>
        protected virtual System.Collections.IEnumerator AutoCleanupRoutine()
        {
            while (IsInitialized && Config.enableAutoCleanup)
            {
                yield return new WaitForSeconds(Config.cleanupInterval);
                CleanupIdleObjects();
            }
        }

        /// <summary>
        /// 计算平均持有时间
        /// </summary>
        protected TimeSpan CalculateAverageHoldTime()
        {
            if (ActiveObjects.Count == 0)
                return TimeSpan.Zero;

            var now = DateTime.Now;
            var totalTime = TimeSpan.Zero;

            foreach (var spawnTime in ActiveObjects.Values)
            {
                totalTime += now - spawnTime;
            }

            return TimeSpan.FromSeconds(totalTime.TotalSeconds / ActiveObjects.Count);
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        protected virtual void UpdateStatistics()
        {
            // 可以在这里添加自定义统计逻辑
        }

        #endregion
    }
}