/*
 * ================================================
 * Describe:      This script is interface for managing a pool of objects of a certain type.
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-02 16:41:01
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-02 16:41:01
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// 对象池接口
    /// </summary>
    public interface IPool<T> where T : class
    {
        /// <summary>
        /// 初始化完成
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 池名
        /// </summary>
        string PoolName { get; }
        
        /// <summary>
        /// 当对象创建
        /// </summary>
        event EventHandler<PoolEventArgs> OnObjectCreated;

        /// <summary>
        /// 当对象被获取
        /// </summary>
        event EventHandler<PoolEventArgs> OnObjectGet;

        /// <summary>
        /// 当对象被回收
        /// </summary>
        event EventHandler<PoolEventArgs> OnObjectRecycle;


        /// <summary>
        /// 当对象被销毁
        /// </summary>
        event EventHandler<PoolEventArgs> OnObjectDestroyed;

        /// <summary>
        /// 当对象被清除
        /// </summary>
        event EventHandler<PoolEventArgs> OnPoolCleared;

        /// <summary>
        /// 当对象池被销毁
        /// </summary>
        event EventHandler<PoolEventArgs> OnPoolDestroyed;
        
        /// <summary>
        /// 初始化对象池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="config">池配置文件</param>
        void Initialize(T prefab, PoolConfig config);

        /// <summary>
        /// 释放并且销毁当前对象池
        /// </summary>
        void Dispose();

        /// <summary>
        /// 清理池中所有对象
        /// </summary>
        void ClearAll();

        /// <summary>
        /// 设置对象工厂
        /// </summary>
        /// <param name="factory">对象工厂</param>
        void SetFactory(IObjectFactory<T> factory);

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>被获取的对象</returns>
        T Get();

        /// <summary>
        /// 回收对象到池中
        /// </summary>
        /// <param name="obj">被回收对象</param>
        /// <returns>回收是否成功</returns>
        bool Recycle(T obj);

        /// <summary>
        /// 获取池统计信息
        /// </summary>
        /// <returns>池统计信息</returns>
        PoolStatistics GetStatistics();
    }
}
