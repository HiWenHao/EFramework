/*
 * ================================================
 * Describe:      This script is used to start another program.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-28 14:20:57
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-07-08 14:21:00
 * ScriptVersion: 0.3
 * ===============================================
 */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Start another program.打开外部程序
    /// <para>菜单层（Menu Layer）只负责描述菜单入口与参数，真正的进程启动全部委派给
    /// <see cref="ProcessToolkit"/>——跨平台脚本包裹、输出捕获、非阻塞等待等逻辑只有一份实现，
    /// 避免在此处重复造一个更脆弱的轮子。</para>
    /// </summary>
    internal static class StartProgramToolkit
    {
        #region EXE or Bat or Sh

        [MenuItem(MenuItemToolkit.Utility + "📊 Excel To Byte File", false, MenuItemToolkit.UtilityPriority + 1)]
        private static void StartETB()
        {
            string toolDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Tools/ExcelToByteFIle"));

            var result = ProcessToolkit.RunCaptured("ExcelToByteFile.exe", toolDir);
            if (result.ExitCode != 0)
            {
                D.Error($"[ StartProgramToolkit ] ExcelToByteFile 执行失败，退出码 {result.ExitCode}。\n{result.Error}\n{result.Output}");
                return;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #endregion

        #region Open URL

        [MenuItem(MenuItemToolkit.About + "🐧 Join QQ group", false, MenuItemToolkit.AboutPriority + 1)]
        private static void JoinQQGroup()
        {
            Application.OpenURL("https://jq.qq.com/?_wv=1027&k=4GvMJd6w");
        }

        [MenuItem(MenuItemToolkit.About + "📃 Open git page", false, MenuItemToolkit.AboutPriority + 2)]
        private static void OpenGitPage()
        {
            Application.OpenURL("https://github.com/HiWenHao/EFramework");
        }

        [MenuItem(MenuItemToolkit.About + "🪲 Report an issue", false, MenuItemToolkit.AboutPriority + 3)]
        private static void ReportAnIssue()
        {
            Application.OpenURL("https://github.com/HiWenHao/EFramework/issues");
        }

        #endregion
    }
}