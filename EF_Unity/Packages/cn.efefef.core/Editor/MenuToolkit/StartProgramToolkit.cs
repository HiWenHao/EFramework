/*
 * ================================================
 * Describe:      This script is used to start another program.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-28 14:20:57
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 15:42:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.MenuToolkit
{
    /// <summary>
    /// Start another program.打开外部程序
    /// </summary>
    internal static class StartProgramToolkit
    {
        #region EXE or Bat or Sh

        [MenuItem("EFTools/Utility/Excel To Byte File", false, 10000)]
        private static void StartETB()
        {
            StartProgram(Application.dataPath + "/../../Tools/ExcelToByteFIle", "ExcelToByteFile.exe");
        }

        [MenuItem("EFTools/Utility/Start Luabn", false, 10001)]
        private static void StartLuabn()
        {
            string assetsPath = $"{Application.dataPath}/Luban";

            string asmdefContent = File.ReadAllText($"{assetsPath}/Luban.asmdef");
            string asmdefMetaContent = File.ReadAllText($"{assetsPath}/Luban.asmdef.meta");

            StartProgram(Application.dataPath + "/../../Tools/LubanTools", "gen.bat", assetsPath);

            StreamWriter fs = File.CreateText($"{assetsPath}/Luban.asmdef");
            try
            {
                fs.Write(asmdefContent);
            }
            finally
            {
                fs.Dispose();
            }

            StreamWriter fsMeta = File.CreateText($"{assetsPath}/Luban.asmdef.meta");
            try
            {
                fsMeta.Write(asmdefMetaContent);
            }
            finally
            {
                fsMeta.Dispose();
            }

            D.Log("Luban run successfully.!!");
        }

        /// <summary>
        /// Start another program.打开外部程序
        /// </summary>
        /// <param name="programPath">运行文件所在绝对路径，不带名称</param>
        /// <param name="programName">运行文件名称，带后缀</param>
        /// <param name="arguments">启动运行文件时的参数</param>
        private static void StartProgram(string programPath, string programName, string arguments = "")
        {
            try
            {
                System.Diagnostics.Process myprocess = new System.Diagnostics.Process();
                myprocess.StartInfo =
                    new System.Diagnostics.ProcessStartInfo(programPath + "/" + programName, arguments)
                    {
                        WorkingDirectory = programPath,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                myprocess.Start();
            }
            catch (Exception ex)
            {
                D.Warning($"加载program失败,{ex.Message}");
            }
        }

        #endregion

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
