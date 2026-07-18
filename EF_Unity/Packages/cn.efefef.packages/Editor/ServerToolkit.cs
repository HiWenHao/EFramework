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
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Packages
{
    /// <summary>
    /// 框架远端服务工具箱
    /// </summary>
    public static class ServerToolkit
    {
        /// <summary> 本地工作状态路径（ProjectSettings 下，自动管理） </summary>
        private static string ConfigPath => Path.Combine(Application.dataPath, "../ProjectSettings/EFPackageCache.json");

        /// <summary> 官方包目录路径（Editor Resources 下，应提交到 Git） </summary>
        public static string CatalogPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages/cn.efefef.packages/Editor Resources/EFPackageCache.json"));

        /// <summary> 远端目录相对路径（在仓库中的位置） </summary>
        public const string RemoteCatalogRepoPath = "EF_Unity/Packages/cn.efefef.packages/Editor Resources/EFPackageCache.json";

        /// <summary>
        /// 获取包配置数据
        /// </summary>
        public static PackageConfig GetPackageConfig()
        {
            if (!File.Exists(ConfigPath))
                return null;

            string json = File.ReadAllText(ConfigPath);
            var packageConfig = JsonUtility.FromJson<PackageConfig>(json);
            return packageConfig;
        }

        /// <summary>
        /// 保存包配置数据
        /// </summary>
        public static void SavePackageConfig(PackageConfig packageConfig)
        {
            string json = packageConfig.ToJson();
            File.WriteAllText(ConfigPath,  json);
        }
        
        public static void SavePackageConfig(string packageConfig)
        {
            File.WriteAllText(ConfigPath,  packageConfig);
        }

        #region 官方包目录 (Catalog) — 提交到 Git 的文件

        /// <summary>
        /// 判断当前项目是否为 EFramework 源码项目（而非使用者项目）
        /// 检测标记：存在 EFramework.sln / EasyFramework-Unity.xmind / .github 目录
        /// 同时支持标准布局（sln 与 Assets 同级）和 EF 布局（sln 在上级目录）
        /// </summary>
        public static bool IsFrameworkProject
        {
            get
            {
                string dataPathParent = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string grandParent = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
                
                // 检查两个层级
                string[] candidates = { dataPathParent, grandParent };
                foreach (string dir in candidates)
                {
                    if (File.Exists(Path.Combine(dir, "EFramework.sln"))
                        || File.Exists(Path.Combine(dir, "EasyFramework-Unity.xmind"))
                        || Directory.Exists(Path.Combine(dir, ".github")))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 加载本地官方包目录
        /// </summary>
        public static EFPackageCatalog LoadCatalog()
        {
            if (!File.Exists(CatalogPath))
                return null;
            return EFPackageCatalog.FromJson(File.ReadAllText(CatalogPath));
        }

        /// <summary>
        /// 保存官方包目录
        /// </summary>
        public static void SaveCatalog(EFPackageCatalog catalog)
        {
            string dir = Path.GetDirectoryName(CatalogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(CatalogPath, catalog.ToJson());
            
            D.Emphasize($"[EF.Packages] 包目录已保存: {CatalogPath}");
            D.Emphasize($"[EF.Packages] 请将此文件提交到 Git 仓库，开发者即可通过\"更新全部信息\"拉取最新目录。");
        }

        /// <summary>
        /// 从本地扫描所有 EF 包，生成官方目录
        /// </summary>
        public static EFPackageCatalog GenerateCatalogFromLocalPackages()
        {
            var catalog = new EFPackageCatalog
            {
                generatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            foreach (var packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (!packageInfo.name.Contains("cn.efefef."))
                    continue;

                catalog.packages.Add(new EFPackageCatalogEntry
                {
                    name = packageInfo.name,
                    displayName = string.IsNullOrEmpty(packageInfo.displayName)
                        ? packageInfo.name : packageInfo.displayName,
                    description = packageInfo.description,
                    version = packageInfo.version,
                });
            }
            
            D.Emphasize($"[EF.Packages] 已扫描 {catalog.packages.Count} 个 EF 包");
            return catalog;
        }

        #endregion
        
        /// <summary>
        /// 获取请求链接地址
        /// </summary>
        /// <param name="platform">远端类型</param>
        public static string GetFrameworkPath(ServerType platform)
        {
            return platform switch
            {
                ServerType.GitHub => "https://github.com/HiWenHao/EFramework",
                ServerType.Gitee => "https://gitee.com/AlvinCN/EFramework",
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
                ServerType.Gitee => "AlvinCN",
                ServerType.Local => "Alvin",
                _ => throw new NotSupportedException()
            };
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
                ServerType.Gitee => $"https://gitee.com/{owner}/{repo}/raw/{branch}/{encodedPath}",
                _ => throw new NotSupportedException()
            };
        }
        
        public static string GetToken(ServerType platform)
        {
            return EditorPrefs.GetString(Application.productName + GetKey(platform), "");
        }

        public static void SetToken(ServerType platform, string token)
        {
            EditorPrefs.SetString(UnityEngine.Application.productName + GetKey(platform), token);
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