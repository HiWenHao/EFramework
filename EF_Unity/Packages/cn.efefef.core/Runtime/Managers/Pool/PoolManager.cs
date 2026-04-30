/*
 * ================================================
 * Describe:      对象池管理器，统一管理 GameObjectPool 和 ObjectPool<T>。
 * Author:        Alvin5100(Wang)
 * CreationTime:  2026-04-30 14:23:19
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2026-04-30 14:23:19
 * ScriptVersion: 0.5
 * ===============================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// 池管理器，负责初始化、获取和回收所有类型的池对象。
    /// </summary>
    public class PoolManager : MonoSingleton<PoolManager>, ISingleton
    {
        private int _poolIdAutoIncrements;
        private Dictionary<Type, object> _objectPools;
        private Dictionary<int, GameObjectPool> _gameObjectPools;
        
        void ISingleton.Init()
        {
            _objectPools = new Dictionary<Type, object>();
            _gameObjectPools = new Dictionary<int, GameObjectPool>();
        }

        void ISingleton.Quit()
        {
            ClearAll();
        }
        
        /// <summary>
        /// 清除所有池
        /// </summary>
        public void ClearAll()
        {
            foreach (GameObjectPool pool in _gameObjectPools.Values)
                pool.Clear();
            foreach (object poolObj in _objectPools.Values)
            {
                if (poolObj is IClearablePool clearable)
                    clearable.Clear();
            }
        }

        #region GameObjectPool
        
        /// <summary>
        /// 创建一个可挂载对象池
        /// </summary>
        /// <param name="prefab">预制件</param>
        /// <param name="parent">父节点</param>
        /// <param name="initial">初始数量</param>
        /// <param name="max">最大数量</param>
        /// <param name="openDebug">开启日志</param>
        /// <returns>对象池ID</returns>
        public int CreateGameObjectPool(GameObject prefab, Transform parent, int initial, int max, bool openDebug)
        {
            _gameObjectPools[++_poolIdAutoIncrements] =
                new GameObjectPool(prefab, initial, max, parent, openDebug);
            
            return _poolIdAutoIncrements;
        }

        /// <summary>
        /// 销毁一个可挂载对象池
        /// </summary>
        /// <param name="poolId">对象池ID</param>
        /// <returns>是否销毁成功</returns>
        public bool DestroyGameObjectPool(int poolId)
        {
            if (_gameObjectPools.TryGetValue(poolId, out var pool))
                pool.Clear();
            return _gameObjectPools.Remove(poolId);
        }
        
        /// <summary>
        /// 从对应池中获取一个 GameObject 实例
        /// </summary>
        /// <param name="poolId">对象池ID</param>
        /// <returns>激活的游戏对象</returns>
        public GameObject Spawn(int poolId)
        {
            if (_gameObjectPools.TryGetValue(poolId, out var pool))
            {
                return pool.Get();
            }
            
            D.Error("Pool doesn't exist or was destroyed.");
            return null;
        }
        
        /// <summary>
        /// 从对应池中获取一个 GameObject 实例，并设置位置和旋转。
        /// </summary>
        /// <param name="poolId">对象池ID</param>
        /// <param name="pos">世界位置</param>
        /// <param name="rot">旋转角度</param>
        /// <returns>激活的游戏对象</returns>
        public GameObject Spawn(int poolId, Vector3 pos, Quaternion rot)
        {
            if (_gameObjectPools.TryGetValue(poolId, out var pool))
            {
                var go = pool.Get();
                go.transform.SetPositionAndRotation(pos, rot);
                return go;
            }
            
            D.Error("Pool doesn't exist or was destroyed.");
            return null;
        }

        /// <summary>
        /// 回收一个池化对象（推荐）
        /// <para>会判断自身归属者是否为 GameObjectPool， 不是则直接销毁</para>
        /// </summary>
        /// <param name="item">PooledObject 组件</param>
        public void Despawn(PooledObject item)
        {
            if (item == null) return;

            if (item.OwnerPool is GameObjectPool pool)
                pool.Recycle(item);
            else
                Destroy(item.gameObject);
        }

        /// <summary>
        /// 回收一个游戏对象（不推荐）
        /// <para>会从自身查找 PooledObject 组件， 没有则直接销毁</para>
        /// </summary>
        /// <param name="go">游戏对象</param>
        public void Despawn(GameObject go)
        {
            if (go == null) return;

            var item = go.GetComponent<PooledObject>();
            if (item != null)
                Despawn(item);
            else
                Destroy(go);
        }

        /// <summary>
        /// 输出所有 GameObjectPool 中泄漏的活动对象（需打开 debug）。
        /// </summary>
        public void DumpAllLeaks()
        {
            foreach (var p in _gameObjectPools.Values)
                p.DumpLeaks();
        }
        
        #endregion
        
        #region ObjectPool<T>

        /// <summary>
        /// 创建一个类型对象池
        /// </summary>
        /// <param name="max">最大容量</param>
        /// <param name="factory">创建函数</param>
        /// <param name="reset">重置方法</param>
        /// <typeparam name="T"></typeparam>
        public void CreateObjectPool<T>(int max, Func<T> factory, Action<T> reset) where T : class
        {
            Type type = typeof(T);
            if (_objectPools.ContainsKey(type))
            {
                D.Warning("Object pool already exists.");
                return;
            }
            
            var pool = new ObjectPool<T>(
                maxSize: max,
                factory: factory,
                reset: reset
            );

            _objectPools[type] = pool;
        }

        /// <summary>
        /// 销毁类型对象池
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool DestroyObjectPool<T>()
        {
            Type type = typeof(T);
            if (!_objectPools.TryGetValue(type, out var pool))
            {
                D.Warning("Object pool doesn't exist.");
                return false;
            }

            if (pool is IClearablePool clearable)
                clearable.Clear();
            return _objectPools.Remove(type);
        }
        
        /// <summary>
        /// 获取已注册的 ObjectPool(T)
        /// </summary>
        /// <typeparam name="T">池中对象类型</typeparam>
        /// <returns>对象池实例，不存在则返回 null</returns>
        public ObjectPool<T> GetObjectPool<T>() where T : class
        {
            var key = typeof(T);
            if (_objectPools.TryGetValue(key, out var pool))
                return pool as ObjectPool<T>;
            return null;
        }

        /// <summary>
        /// 从对应的 ObjectPool(T)  中取一个对象。
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>对象实例，若池不存在则返回 null</returns>
        public T GetFromPool<T>() where T : class
        {
            Type type = typeof(T);
            ObjectPool<T> pool = null;
            
            if (_objectPools.TryGetValue(type, out var poolObject))
                pool = poolObject as ObjectPool<T>;
            return pool?.Get();
        }

        /// <summary>
        /// 将对象归还到对应的 ObjectPool(T)
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="item">要归还的对象实例</param>
        public void ReturnToPool<T>(T item) where T : class
        {
            if (item == null) 
                return;
            
            if (!_objectPools.TryGetValue(typeof(T), out var poolObject))
                return;
            ObjectPool<T> pool = poolObject as ObjectPool<T>;
            pool?.Recycle(item);
        }

        #endregion

    }
}