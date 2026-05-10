/*
 * ================================================
 * Describe:        对象池系统，统一管理 GameObjectPool 和 ObjectPool<T>。
 * Author:          Alvin5100(Wang)
 * CreationTime:    2026-04-30 14:23:19
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-10
 * ScriptVersion:   0.2
 * ================================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Systems.Pool
{
    /// <summary>
    /// 池系统，负责初始化、获取和回收所有类型的池对象，并驱动空闲超时销毁。
    /// <para>The pool system is responsible for initializing, obtaining and recycling all types of pool objects,
    /// and driving the automatic destruction of idle objects after a certain timeout period.</para>
    /// </summary>
    public class PoolSystem : MonoSingleton<PoolSystem>, IManager, IUpdate
    {
        /// <summary>
        /// 开启输出日志
        /// <para>Enable output logging</para>
        /// </summary>
        public bool OpenDebug { get; private set; }
        
        private int _poolIdAutoIncrements;
        private Dictionary<Type, object> _objectPools;
        private Dictionary<int, GameObjectPool> _gameObjectPools;

        void ISingleton.Init()
        {
            _objectPools = new Dictionary<Type, object>();
            _gameObjectPools = new Dictionary<int, GameObjectPool>();
        }

        // 驱动所有 GameObjectPool 的闲置超时清理
        void IUpdate.Update(float elapse, float realElapse)
        {
            foreach (var pool in _gameObjectPools.Values)
            {
                pool.CleanupIdleObjects(Time.time);
            }
        }
        
        void ISingleton.Quit()
        {
            ClearAll();
        }
        
        /// <summary>
        /// 清除所有池
        /// <para>Clear all pools</para>
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
        /// <para>Create an object pool that can be mounted</para>
        /// </summary>
        /// <param name="prefab">预制件</param>
        /// <param name="parent">父节点</param>
        /// <param name="initial">初始数量</param>
        /// <param name="max">最大数量</param>
        /// <param name="idleTimeout">空闲超时销毁时间（秒），≤0 不启用
        /// <para>Idle timeout destruction time (seconds), ≤ 0 means not enabled</para></param>
        /// <returns>对象池ID  <para>The GameObject Pool ID</para></returns>
        public int CreateGameObjectPool(GameObject prefab, Transform parent, int initial, int max, float idleTimeout = -1f)
        {
            var pool = new GameObjectPool(prefab, initial, max, parent, idleTimeout)
            {
                OpenDebug = OpenDebug
            };
            
            _gameObjectPools[++_poolIdAutoIncrements] = pool;
            return _poolIdAutoIncrements;
        }

        /// <summary>
        /// 销毁一个可挂载对象池
        /// <para>Destroy a mountable object pool</para>
        /// </summary>
        /// <param name="poolId">对象池ID<para>The GameObject Pool ID</para></param>
        /// <returns>是否销毁成功
        /// <para>The Destroy succeed</para></returns>
        public bool DestroyGameObjectPool(int poolId)
        {
            if (_gameObjectPools.TryGetValue(poolId, out var pool))
                pool.Clear();
            return _gameObjectPools.Remove(poolId);
        }
        
        /// <summary>
        /// 从对应池中获取一个 GameObject 实例
        /// <para>Obtain an instance of GameObject from the corresponding pool</para>
        /// </summary>
        /// <param name="poolId">对象池ID
        /// <para>The GameObject Pool ID</para></param>
        /// <returns>激活的游戏对象
        /// <para>Activated game object</para></returns>
        public GameObject Spawn(int poolId)
        {
            if (_gameObjectPools.TryGetValue(poolId, out var pool))
                return pool.Get();

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
        /// 输出所有 GameObjectPool 中泄漏的活动对象（需打开 OpenDebug）。
        /// <para>Output all the active objects that have leaked in the GameObjectPool (this requires enabling debug mode).</para>
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
        /// <para>Create a type object pool</para>
        /// </summary>
        /// <param name="max">最大容量<para>Max Capacity</para></param>
        /// <param name="factory">创建函数<para>Create Function</para></param>
        /// <param name="reset">重置方法<para>Reset Action</para></param>
        /// <typeparam name="T">对象类型<para>The object type</para></typeparam>
        public void CreateObjectPool<T>(int max, Func<T> factory, Action<T> reset) where T : class
        {
            Type type = typeof(T);
            if (_objectPools.ContainsKey(type))
            {
                Warning("Object pool already exists.");
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
        /// <para>Destroy the type object pool</para>
        /// </summary>
        /// <typeparam name="T">对象类型<para>The object type</para></typeparam>
        /// <returns>是否销毁成功
        /// <para>The Destroy succeed</para></returns>
        public bool DestroyObjectPool<T>()
        {
            Type type = typeof(T);
            if (!_objectPools.TryGetValue(type, out var pool))
            {
                Warning("Object pool doesn't exist.");
                return false;
            }

            if (pool is IClearablePool clearable)
                clearable.Clear();
            return _objectPools.Remove(type);
        }
        
        /// <summary>
        /// 获取已注册的 T类型ObjectPool
        /// <para>Obtain the registered ObjectPool type of T </para>
        /// </summary>
        /// <typeparam name="T">池中对象类型
        /// <para>Object type in the pool</para></typeparam>
        /// <returns>对象池实例，不存在则返回 null
        /// <para>Object pool instance. If it does not exist, return null.</para></returns>
        public ObjectPool<T> GetObjectPool<T>() where T : class
        {
            var key = typeof(T);
            if (_objectPools.TryGetValue(key, out var pool))
                return pool as ObjectPool<T>;
            return null;
        }

        /// <summary>
        /// 从对应 T 类型的对象池中取一个对象。
        /// <para>Take an object from the object pool of the corresponding T type.</para>
        /// </summary>
        /// <typeparam name="T">对象类型<para>object type</para></typeparam>
        /// <returns>对象实例，若池不存在则返回 null
        /// <para>Object instance. If the pool does not exist, return null.</para></returns>
        public T GetFromPool<T>() where T : class
        {
            Type type = typeof(T);
            ObjectPool<T> pool = null;

            if (_objectPools.TryGetValue(type, out var poolObject))
                pool = poolObject as ObjectPool<T>;
            return pool?.Get();
        }

        /// <summary>
        /// 将T类型对象归还到对应的对象池中
        /// <para>Return the T type object to the corresponding object pool.</para>
        /// </summary>
        /// <typeparam name="T">对象类型<para>object type</para></typeparam>
        /// <param name="item">要归还的对象实例
        /// <para>The object instance to be returned</para></param>
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

        #region 公开函数
        
        /// <summary>
        /// 设置开启日志输出
        /// <para>Set to enable log output</para>
        /// </summary>
        /// <param name="openDebug">打开调试模式</param>
        public void SetOpenDebug(bool openDebug)
        {
            OpenDebug = openDebug;
            foreach (var pool in _gameObjectPools.Values)
            {
                pool.OpenDebug = OpenDebug;
            }
        }
        
        #endregion
        
        #region 私有函数
        
        private void Warning(string msg)
        {
            if (OpenDebug)
                D.Warning(msg);
        }

        #endregion
    }
}