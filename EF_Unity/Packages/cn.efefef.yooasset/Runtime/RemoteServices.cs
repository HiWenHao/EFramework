/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-05-29 15:03:22
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-29 15:03:22
 * ScriptVersion:   0.1
 * ================================================
 */

using YooAsset;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 远端资源地址查询服务类
    /// <para>Remote resource address query service class</para>
    /// </summary>
    internal class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;     // 默认主机服务器地址
        private readonly string _fallbackHostServer;    // 备用服务器地址

        /// <summary>
        /// 构造远端资源服务
        /// <para>Construct a remote resource service instance</para>
        /// </summary>
        /// <param name="defaultHostServer">默认主机服务器<para>Default host server URL</para></param>
        /// <param name="fallbackHostServer">备用服务器<para>Fallback host server URL</para></param>
        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer.TrimEnd('/');
            _fallbackHostServer = fallbackHostServer.TrimEnd('/');
        }

        /// <summary>
        /// 获取远端主资源地址
        /// <para>Get the main remote resource URL</para>
        /// </summary>
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }

        /// <summary>
        /// 获取远端备用资源地址
        /// <para>Get the fallback remote resource URL</para>
        /// </summary>
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }
}