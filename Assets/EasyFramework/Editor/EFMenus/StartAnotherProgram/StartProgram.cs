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
using XHTools;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Start another program.打开外部程序
    /// </summary>
    public class StartProgram
    {
        [MenuItem("EFTools/ThirdPartyAssets/Excel To Byte File", false, 2000)]
        private static void StartETB()
        {
            string _path = Application.dataPath.Substring(0, Application.dataPath.IndexOf("Assets"));            
            StartEXE(_path + "ExcelToByteFIle", "ExcelToByteFile.exe");
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
        [MenuItem("EFTools/About Us/Join QQ group", false, 10000)]
        private static void JoinQQGroup()
        {
            Application.OpenURL("https://jq.qq.com/?_wv=1027&k=4GvMJd6w");
        }
        [MenuItem("EFTools/About Us/Open Git Page", false, 10001)]
        private static void OpenGitPage()
        {
            Application.OpenURL("https://github.com/HiWenHao/EFramework`");
        }
        #endregion
    }
}
