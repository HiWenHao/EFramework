/* 
 * ================================================
 * Describe:      This script is used to start another program.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-28 14:20:57
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-28 14:20:57
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;
using UnityEditor;
using UnityEngine;
using EasyFramework;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Start another program.打开外部程序
    /// </summary>
    public class StartProgram
    {
        [MenuItem("EFTools/Utility/Excel To Byte File", false, 99999)]
        private static void StartETB()
        {
            StartEXE(Application.dataPath + "/../../Tools/ExcelToByteFIle", "ExcelToByteFile.exe");
        }

        /// <summary>
        /// Start another program.打开外部程序
        /// </summary>
        /// <param name="exePath">EXE所在绝对路径，不带名称</param>
        /// <param name="exeName">exe名称，带后缀</param>
        static void StartEXE(string exePath, string exeName)
        {
            try
            {
                System.Diagnostics.Process myprocess = new System.Diagnostics.Process();
                myprocess.StartInfo = new System.Diagnostics.ProcessStartInfo(exePath + "/" + exeName)
                {
                    WorkingDirectory = exePath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                myprocess.Start();
            }
            catch (Exception ex)
            {
                D.Warning("加载exe失败," + ex.Message);
            }
        }

        #region Open URL
        [MenuItem("EFTools/About Us/Join QQ group", false, 100000)]
        private static void JoinQQGroup()
        {
            Application.OpenURL("https://jq.qq.com/?_wv=1027&k=4GvMJd6w");
        }
        [MenuItem("EFTools/About Us/Open git page", false, 100001)]
        private static void OpenGitPage()
        {
            Application.OpenURL("https://github.com/HiWenHao/EFramework");
        }
        [MenuItem("EFTools/About Us/Report an iuess", false, 100002)]
        private static void ReportAnIuess()
        {
            Application.OpenURL("https://github.com/HiWenHao/EFramework/issues");
        }
        #endregion
    }
}
