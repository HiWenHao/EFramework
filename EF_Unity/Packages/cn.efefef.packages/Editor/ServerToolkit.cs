/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-20 14:44:04
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-20 14:44:04
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using UnityEditor;

namespace EasyFramework.Edit.Packages
{
    /// <summary>
    /// 框架远端服务工具箱
    /// </summary>
    public static class ServerToolkit
    {
        /// <summary>
        /// 获取请求链接地址
        /// </summary>
        /// <param name="platform">远端类型</param>
        public static string GetFrameworkPath(ServerType platform)
        {
            return platform switch
            {
                ServerType.GitHub => "https://github.com/HiWenHao/EFramework",
                ServerType.Gitee  => "https://gitee.com/AlvinCN/EFramework",
                ServerType.Local => "file://",
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        /// 获取框架作者名
        /// </summary>
        /// <param name="platform">远端类型</param>
        public static string GetFrameworkOwner(ServerType platform)
        {
            return platform switch
            {
                ServerType.GitHub => "HiWenHao",
                ServerType.Gitee  => "AlvinCN",
                ServerType.Local => "Alvin",
                _ => throw new NotSupportedException()
            };
        }
        
        public static string GetToken(ServerType platform)
        {
            return EditorPrefs.GetString(UnityEngine.Application.productName + GetKey(platform), "");
        }

        public static void SetToken(ServerType platform,  string token)
        {
            EditorPrefs.SetString(UnityEngine.Application.productName + GetKey(platform), token);
        }

        /// <summary>
        /// 获取请求链接地址
        /// </summary>
        /// <param name="platform">远端类型</param>
        /// <param name="owner">仓库拥有者名</param>
        /// <param name="repo">框架名</param>
        /// <param name="branch">分支名</param>
        /// <param name="filePath">位于项目下的具体位置</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static string GetRawUrl(ServerType platform, string owner, string repo, string branch, string filePath)
        {
            string encodedPath = Uri.EscapeDataString(filePath);
            return platform switch
            {
                ServerType.GitHub => $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{encodedPath}",
                ServerType.Gitee  => $"https://gitee.com/{owner}/{repo}/raw/{branch}/{encodedPath}",
                _ => throw new NotSupportedException()
            };
        }

        private static string GetKey(ServerType platform)
        {
            return platform switch
            {
                ServerType.GitHub => "EFPackagesGitHub",
                ServerType.Gitee => "EFPackagesGitee",
                ServerType.Local => "Local",
                _ => "Local"
            };
        }
    }
}
