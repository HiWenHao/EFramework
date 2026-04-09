/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-02 16:12:15
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-02 16:12:15
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using EasyFramework.Managers.Pool;
using UnityEngine;

namespace EasyFramework.Managers
{
    /// <summary>
    /// Pooled manager.
    /// <para>对象池管理器</para>
    /// </summary>
    public class PoolManager : MonoSingleton<PoolManager>, IManager
    {
        public static event EventHandler<PoolEventArgs> OnPoolCreated;
        public static event EventHandler<PoolEventArgs> OnPoolDestroyed;
        public static event EventHandler<PoolEventArgs> OnGlobalStatisticsUpdated;

        private bool _enableGlobalStatistics = true;
        private float _statisticsUpdateInterval = 5f;
        private float _lastStatisticsUpdateTime;

        private const int MaxSize = 1000;

        private Dictionary<Type, ObjectPool> _objectPools;
        private List<PoolStatistics> _globalStatistics;
        private Dictionary<object, string> _objectToPool;
        private Dictionary<string, IPool<GameObject>> _unityObjectPools;

        void ISingleton.Init()
        {
            _objectPools = new Dictionary<Type, ObjectPool>();
            _globalStatistics = new List<PoolStatistics>();
            _objectToPool = new Dictionary<object, string>();
            _unityObjectPools = new Dictionary<string, IPool<GameObject>>();
        }

        void ISingleton.Quit()
        {
            DisposeAll();
            _objectPools = null;
        }

        #region Mono挂载相关对象池

        /// <summary>
        /// 获取池
        /// </summary>
        public IPool<T> GetPool<T>(string poolName) where T : class
        {
            if (_unityObjectPools.TryGetValue(poolName, out var pool))
            {
                return pool as IPool<T>;
            }

            D.Error($"Pool '{poolName}' not found");
            return null;
        }

        /// <summary>
        /// 销毁池
        /// </summary>
        /// <param name="poolName">池名称</param>
        /// <returns>是否销毁成功</returns>
        public bool DestroyPool(string poolName)
        {
            if (!_unityObjectPools.TryGetValue(poolName, out var pool))
            {
                D.Warning($"Pool '{poolName}' not found");
                return false;
            }

            pool.Dispose();
            if (pool is MonoBehaviour monoPool)
                Destroy(monoPool.gameObject);

            _unityObjectPools.Remove(poolName);
            OnPoolDestroyed?.Invoke(this, PoolEventArgs.Create(poolName, PoolEventType.Destroyed));
            return true;
        }

        /// <summary>
        /// 创建 GameObject 池
        /// </summary>
        public UnityGameObjectPool CreateGameObjectPools(GameObject prefab, PoolConfig config)
        {
            if (_unityObjectPools.TryGetValue(config.poolName, out var objectPool))
            {
                Debug.LogWarning($"Pool '{config.poolName}' already exists");
                return objectPool as UnityGameObjectPool;
            }

            GameObject poolObject = new GameObject(config.poolName);
            poolObject.transform.SetParent(transform);

            UnityGameObjectPool pool = poolObject.AddComponent<UnityGameObjectPool>();

            pool.Initialize(prefab, config);

            if (pool is IPool<GameObject> iPool)
            {
                _unityObjectPools.Add(config.poolName, iPool);
                D.Warning("Pool '" + config.poolName + "' created");
            }

            // 触发事件
            OnPoolCreated?.Invoke(this, PoolEventArgs.Create(config.poolName, PoolEventType.Created));

            if (config.enableShowDebugInfo)
                Debug.Log($"GameObject pool '{config.poolName}' created");

            return pool;
        }

        /// <summary>
        /// 从指定池中获取对象
        /// </summary>
        public T GetFromPool<T>(string poolName) where T : class
        {
            var pool = GetPool<T>(poolName);
            if (pool == null)
            {
                D.Error($"Pool '{poolName}' not found or type mismatch");
                return null;
            }

            T obj = pool.Get();
            if (obj != null)
                _objectToPool[obj] = poolName;

            return obj;
        }

        /// <summary>
        /// 回收对象到对应的池
        /// </summary>
        public bool RecycleToPool<T>(T obj) where T : class
        {
            if (obj == null)
            {
                Debug.LogWarning("Attempted to recycle null object");
                return false;
            }

            if (!_objectToPool.TryGetValue(obj, out var poolName))
            {
                Debug.LogWarning($"Object does not belong to any managed pool: {obj}");
                return false;
            }

            var pool = GetPool<T>(poolName);
            if (pool == null)
            {
                Debug.LogError($"Pool '{poolName}' not found for object: {obj}");
                return false;
            }

            bool result = pool.Recycle(obj);
            if (result)
            {
                _objectToPool.Remove(obj);
            }

            return result;
        }

        #endregion


        #region --------------------------------------------------------------------------------

        /// <summary>
        /// 获取一个非挂载类型对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>对象</returns>
        public T Get<T>() where T : class
        {
            Type type = typeof(T);

            // if (PoolTools.IsMountableMonoType(type))
            // {
            //     if (!_unityObjectPools.ContainsKey(type))
            //         _unityObjectPools.Add(type, new UnityGameObjectPool(new GameObject(), transform, PoolConfig.Default));
            //     
            //     return _unityObjectPools[type].Get() as T;
            // }

            if (!_objectPools.ContainsKey(type))
                _objectPools.Add(type, new ObjectPool(MaxSize));

            return _objectPools[type].Get<T>();
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="item">元素</param>
        /// <typeparam name="T">元素类型</typeparam>
        public void Recycle<T>(T item) where T : class
        {
            Type type = item.GetType();

            if (PoolTools.IsMountableMonoType(type))
            {
                // if (!_unityObjectPools.ContainsKey(type))
                //     _unityObjectPools.Add(type, new UnityObjectPool(new GameObject(), transform, PoolConfig.Default));
                //
                // _unityObjectPools[type].Recycle(item as GameObject);
                return;
            }


            if (!_objectPools.ContainsKey(type))
            {
                D.Error($"Type of [ {type.FullName} ] recycle object pool doesn't exist");
                return;
            }

            _objectPools[type].Recycle(item);
        }

        /// <summary>
        /// 释放 <typeparamref name="T"/> 类型对象池
        /// </summary>
        /// <typeparam name="T">相关类型</typeparam>
        public void DisposeOf<T>() where T : class
        {
            Type type = typeof(T);
            if (PoolTools.IsMountableMonoType(type))
            {
                _unityObjectPools[type.Name].ClearAll();
                return;
            }

            _objectPools[type].Dispose();
        }

        /// <summary>
        /// 释放全部对象池
        /// </summary>
        public void DisposeAll()
        {
            foreach (var objectPool in _objectPools.Values)
            {
                objectPool.Dispose();
            }

            foreach (var unityPool in _unityObjectPools.Values)
            {
                unityPool.ClearAll();
            }

            _objectPools.Clear();
            _unityObjectPools.Clear();
        }

        #endregion


        #region 统计信息

        /// <summary>
        /// 获取所有池的统计信息
        /// </summary>
        public List<PoolStatistics> GetAllPoolStatistics()
        {
            var stats = new List<PoolStatistics>();

            foreach (var pool in _unityObjectPools.Values)
            {
                stats.Add(pool.GetStatistics());
            }

            return stats;
        }

        /// <summary>
        /// 获取全局统计信息
        /// </summary>
        public GlobalPoolStatistics GetGlobalStatistics()
        {
            var allStats = GetAllPoolStatistics();

            return new GlobalPoolStatistics
            {
                //TotalPools = allStats.Count,
                //TotalAvailable = allStats.Sum(s => s.AvailableCount),
                //TotalActive = allStats.Sum(s => s.ActiveCount),
                //TotalCreated = allStats.Sum(s => s.TotalCreated),
                //PeakActive = allStats.Sum(s => s.PeakActiveCount),
                //LastUpdateTime = DateTime.Now
            };
        }

        /// <summary>
        /// 打印所有池的统计信息
        /// </summary>
        public void PrintAllStatistics()
        {
            var stats = GetAllPoolStatistics();

            Debug.Log("=== Object Pool Statistics ===");
            foreach (var stat in stats)
            {
                Debug.Log(stat.ToString());
            }

            var global = GetGlobalStatistics();
            Debug.Log($"=== Global Statistics ===");
            Debug.Log($"Total Pools: {global.TotalPools}");
            Debug.Log($"Total Available: {global.TotalAvailable}");
            Debug.Log($"Total Active: {global.TotalActive}");
            Debug.Log($"Total Created: {global.TotalCreated}");
            Debug.Log($"Peak Active: {global.PeakActive}");
        }

        protected virtual void Updates()
        {
            if (_enableGlobalStatistics &&
                UnityEngine.Time.time - _lastStatisticsUpdateTime > _statisticsUpdateInterval)
            {
                _globalStatistics.AddRange(GetAllPoolStatistics());
                _lastStatisticsUpdateTime = UnityEngine.Time.time;

                // 触发事件
                OnGlobalStatisticsUpdated?.Invoke(this,
                    PoolEventArgs.Create("Global", PoolEventType.StatisticsUpdated, GetGlobalStatistics()));
            }
        }

        #endregion
    }
}