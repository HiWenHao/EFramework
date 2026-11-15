/*
 * ================================================
 * Describe:      This script is used to boot the game through EF.Launcher.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-26 14:15:44
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-14 23:30:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using EasyFramework.Launcher;
using UnityEngine;

namespace EFExample
{
    /// <summary>
    /// 启动入口：只负责把控制权交给 EF.Launcher
    /// <para>Boot entry: hands control over to EF.Launcher.</para>
    /// </summary>
    /// <remarks>
    /// 本程序集（ExampleGame）属于 AOT 层，<b>不得引用任何 cn.efefef.* 运行时包</b>。
    /// 否则切到全量热更形态时 EF 会被剔除出包体，链接器会报
    /// <c>Failed to resolve assembly: 'EF.Core'</c>。
    /// </remarks>
    public class GameStart : MonoBehaviour
    {
        [Tooltip("启动配置，一般放在 Assets/Launcher/Resources/LauncherConfig.asset")]
        [SerializeField] private LauncherConfig _config;

        private void Start()
        {
            if (_config == null)
            {
                Debug.LogError("[GameStart] 未指定 LauncherConfig，启动中止。");
                return;
            }

            // 独立挂到一个常驻节点上，避免把本场景的物体一起 DontDestroyOnLoad
            var go = new GameObject("[Launcher]");
            DontDestroyOnLoad(go);

            var launcher = go.AddComponent<Launcher>();
            launcher.Configure(_config);
        }
    }
}
