/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 09:59:22
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-10 09:59:22
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using UnityEngine;

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// 池配置
    /// </summary>
    [Serializable]
    public struct PoolConfig
    {
        /// <summary>
        /// 池名称
        /// </summary>
        [Header("基础设置")] [Tooltip("池名称")] public string poolName;

        /// <summary>
        /// 预加载数量
        /// </summary>
        [Tooltip("预加载数量")] public int preloadCount;

        /// <summary>
        /// 池最大容量
        /// </summary>
        [Tooltip("池最大容量")] public int maxPoolSize;

        /// <summary>
        /// 是否启用自动清理
        /// </summary>
        [Tooltip("是否启用自动清理")] public bool enableAutoCleanup;

        /// <summary>
        /// 自动清理间隔
        /// </summary>
        [Tooltip("自动清理间隔（秒）")] public float cleanupInterval;

        /// <summary>
        /// 空闲对象最大存活时间（秒）
        /// </summary>
        [Tooltip("空闲对象最大存活时间（秒）")] public float maxIdleTime;

        /// <summary>
        /// 是否启用惰性加载
        /// </summary>
        [Header("性能优化")] [Tooltip("是否启用惰性加载")] public bool lazyLoading;

        /// <summary>
        /// 是否启用对象复用检测
        /// </summary>
        [Tooltip("是否启用对象复用检测")] public bool enableReuseCheck;

        /// <summary>
        /// 是否在编辑器中显示调试信息
        /// </summary>
        [Tooltip("是否在编辑器中显示调试信息")] public bool enableShowDebugInfo;

        /// <summary>
        /// 默认配置
        /// </summary>
        public static PoolConfig Default => new PoolConfig
        {
            poolName = "DefaultPool",
            preloadCount = 5,
            maxPoolSize = 100,
            enableAutoCleanup = false,
            cleanupInterval = 60f,
            maxIdleTime = 300f,
            lazyLoading = true,
            enableReuseCheck = true,
            enableShowDebugInfo = false
        };
    }
}