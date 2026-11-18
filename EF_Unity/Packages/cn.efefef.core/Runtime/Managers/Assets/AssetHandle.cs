/*
 * ================================================
 * Describe:      资源加载句柄 —— 一次加载的唯一释放凭证
 * Author:        Alvin5100
 * CreationTime:  2026-11-18 15:32:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-18 15:32:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Threading;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 资源加载句柄 —— 持有一次加载的引用，Dispose 即释放该引用。
    /// 幂等（多次 Dispose 无害）、并发安全（Interlocked）；资源必须经 <see cref="Asset"/> 访问。
    /// <para>Asset load handle — holds one reference to a load; disposing releases that reference.
    /// Idempotent, thread-safe; access the asset only via <see cref="Asset"/>.</para>
    /// </summary>
    /// <typeparam name="T">资源类型<para>Asset type</para></typeparam>
    public sealed class AssetHandle<T> : IDisposable where T : UnityEngine.Object
    {
        // 所属门面，Dispose 时回查释放
        private readonly AssetsManager _owner;

        // 资源包名（默认包为 null）
        private readonly string _packageName;

        // 资源路径
        private readonly string _path;

        // 持有的资源，Dispose 后置 null
        private T _asset;

        // 是否已释放：0=有效，1=已释放
        private int _disposed;

        internal AssetHandle(AssetsManager owner, string packageName, string path, T asset)
        {
            _owner = owner;
            _packageName = packageName;
            _path = path;
            _asset = asset;
        }

        /// <summary>
        /// 持有的资源；已释放返回 null
        /// <para>The held asset; null if disposed</para>
        /// </summary>
        public T Asset => Volatile.Read(ref _disposed) == 0 ? _asset : null;

        /// <summary>
        /// 句柄是否有效（未释放且资源非空）
        /// <para>Whether the handle is valid (not disposed and asset present)</para>
        /// </summary>
        public bool IsValid => Volatile.Read(ref _disposed) == 0 && _asset != null;

        /// <summary>
        /// 释放该句柄持有的引用；幂等且并发安全
        /// <para>Release the reference held by this handle; idempotent and thread-safe</para>
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            _asset = null;
            _owner.ReleaseInternal(_packageName, _path);
        }
    }
}