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
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using UnityEngine;

namespace EasyFramework.Systems.Pool
{
    /// <summary>
    /// 池系统，负责初始化、获取和回收所有类型的池对象，并驱动空闲超时销毁。
    /// <para>The pool system is responsible for initializing, obtaining and recycling all types of pool objects,
    /// and driving the automatic destruction of idle objects after a certain timeout period.</para>
    /// </summary>
    [Manager(Order = -1000)]
    public class PoolSystem : MonoSingleton<PoolSystem>, ISingleton, IUpdate
    {
        /// <summary>
        /// 开启输出日志
        /// <para>Enable output logging</para>
        /// </summary>
        public bool OpenDebug { get; private set; }

        /// <summary>
        /// 清理计时器
        /// <para>Cleanup tick</para>
        /// </summary>
        public float CleanupTick { get; set; } = 5;

        public bool IsPaused { get; private set; }
        
        private float _gameObjectPoolCleanupTick;
        private Dictionary<Type, object> _objectPools;
        private Dictionary<GameObject, GameObjectPool> _gameObjectPools;

        void ISingleton.Init()
        {
            _objectPools = new Dictionary<Type, object>();
            _gameObjectPools = new Dictionary<GameObject, GameObjectPool>();
        }

        // 驱动所有 GameObjectPool 的闲置超时清理
        void IUpdate.Update(float elapse, float realElapse)
        {
            if ((_gameObjectPoolCleanupTick += elapse) < CleanupTick)
                return;
            
            _gameObjectPoolCleanupTick = 0;
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
            {
                pool.Clear();
            }
            foreach (object poolObj in _objectPools.Values)
            {
                if (poolObj is IClearablePool clearable)
                    clearable.Clear();
            }
            
            _gameObjectPools.Clear();
            _objectPools.Clear();
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
        public void CreateGameObjectPool(GameObject prefab, Transform parent, int initial, int max, float idleTimeout = -1f)
        {
            if (_gameObjectPools.ContainsKey(prefab))
            {
                Warning($"Pool for prefab {prefab.name} already exists.");
                return;
            }
            var pool = new GameObjectPool(prefab, initial, max, parent, idleTimeout) { OpenDebug = OpenDebug };
            _gameObjectPools[prefab] = pool;
        }

        /// <summary>
        /// 销毁一个可挂载对象池
        /// <para>Destroy a mountable object pool</para>
        /// </summary>
        /// <param name="prefab">对象池预制件<para>The GameObject Pool Prefab</para></param>
        /// <returns>是否销毁成功
        /// <para>The Destroy succeed</para></returns>
        public bool DestroyGameObjectPool(GameObject prefab)
        {
            if (_gameObjectPools.TryGetValue(prefab, out var pool))
                pool.Clear();
            return _gameObjectPools.Remove(prefab);
        }
        
        /// <summary>
        /// 从对应池中获取一个 GameObject 实例
        /// <para>Obtain an instance of GameObject from the corresponding pool</para>
        /// </summary>
        /// <param name="prefab">对象池预制件
        /// <para>The GameObject Pool Prefab</para></param>
        /// <returns>激活的游戏对象
        /// <para>Activated game object</para></returns>
        public GameObject Spawn(GameObject prefab)
        {
            if (TryGetPool(prefab, out var pool))
                return pool.Get();

            D.Error($"Pool for prefab {prefab.name} not created. Please call CreateGameObjectPool first.");
            return null;
        }
        
        /// <summary>
        /// 从对应池中获取一个 GameObject 实例
        /// <para>Obtain an instance of GameObject from the corresponding pool</para>
        /// </summary>
        /// <param name="prefab">对象池预制件
        /// <para>The GameObject Pool Prefab</para></param>
        /// <param name="pos">世界位置</param>
        /// <param name="rot">旋转角度</param>
        /// <returns>激活的游戏对象
        /// <para>Activated game object</para></returns>
        public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            var go = Spawn(prefab);
            if (go != null) 
                go.transform.SetPositionAndRotation(pos, rot);
            return go;
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
        
        #region 异步预热（UniTask）

        /// <summary>
        /// 异步预热 GameObject 池（分帧创建，避免卡顿）。基于 UniTask，支持进度报告和取消。
        /// <para>Asynchronously preheat GameObject pool (create objects across frames to avoid stuttering). Based on UniTask, supports progress reporting and cancellation.</para>
        /// </summary>
        /// <param name="prefab">对象池预制件</param>
        /// <param name="totalCount">要创建的总数量</param>
        /// <param name="perFrame">每帧创建的数量（建议 1~10）</param>
        /// <param name="progress">进度回调（已创建数量，总数量）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>UniTask，等待预热完成</returns>
        public async UniTask WarmupGameObjectPoolAsync(GameObject prefab, int totalCount, int perFrame = 5,
            IProgress<(int current, int total)> progress = null, CancellationToken cancellationToken = default)
        {
            if (!TryGetPool(prefab, out var pool))
            {
                D.Error($"Pool for prefab {prefab.name} not found.");
                return;
            }

            if (totalCount <= 0) return;
            if (perFrame <= 0) perFrame = 1;

            int created = 0;
            while (created < totalCount)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 如果池已被销毁，提前退出
                if (!pool.IsAlive)
                {
                    Warning($"Pool for {prefab.name} was disposed during warmup. Warmup stopped.");
                    return;
                }

                int toCreate = Mathf.Min(perFrame, totalCount - created);
                for (int i = 0; i < toCreate; i++)
                {
                    pool.CreateOneAndPush();
                }
                created += toCreate;
                progress?.Report((created, totalCount));

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        /// <summary>
        /// 异步预热 GameObject 池（分帧创建，避免卡顿）。基于 UniTask，支持进度报告和取消。
        /// <para>Asynchronously preheat GameObject pool (create objects across frames to avoid stuttering). Based on UniTask, supports progress reporting and cancellation.</para>
        /// </summary>
        /// <param name="prefab">对象池预制件</param>
        /// <param name="totalCount">要创建的总数量</param>
        /// <param name="perFrame">每帧创建的数量（建议 1~10）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>UniTask，等待预热完成</returns>
        public UniTask WarmupGameObjectPoolAsync(GameObject prefab, int totalCount, int perFrame = 5, CancellationToken cancellationToken = default)
        {
            return WarmupGameObjectPoolAsync(prefab, totalCount, perFrame, null, cancellationToken);
        }

        #endregion

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
            if (null == _gameObjectPools)
                return;
            
            foreach (var pool in _gameObjectPools.Values)
            {
                pool.OpenDebug = OpenDebug;
            }
        }
        
        #endregion
        
        #region 私有函数
        
        private bool TryGetPool(GameObject prefab, out GameObjectPool pool)
        {
            return _gameObjectPools.TryGetValue(prefab, out pool);
        }
        
        private void Warning(string msg)
        {
            if (OpenDebug)
                D.Warning(msg);
        }

        #endregion
    }
}