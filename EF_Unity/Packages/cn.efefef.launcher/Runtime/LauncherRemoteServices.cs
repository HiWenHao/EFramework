/*
 * ================================================
 * Describe:        This script is used to resolve the remote asset urls.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 15:58:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 15:58:00
 * ScriptVersion:   0.1
 * ================================================
 */

using YooAsset;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 远端资源地址查询服务。独立实现，不复用 EF.YooAsset（那个在热更层）
    /// <para>Remote URL resolver. Deliberately standalone — EF.YooAsset lives in the hot update layer.</para>
    /// </summary>
    internal sealed class LauncherRemoteServices : IRemoteServices
    {
        private readonly string _mainHost;
        private readonly string _fallbackHost;

        public LauncherRemoteServices(string mainHost, string fallbackHost)
        {
            _mainHost = (mainHost ?? string.Empty).TrimEnd('/');
            _fallbackHost = string.IsNullOrWhiteSpace(fallbackHost)
                ? _mainHost
                : fallbackHost.TrimEnd('/');
        }

        string IRemoteServices.GetRemoteMainURL(string fileName) => $"{_mainHost}/{fileName}";

        string IRemoteServices.GetRemoteFallbackURL(string fileName) => $"{_fallbackHost}/{fileName}";
    }
}