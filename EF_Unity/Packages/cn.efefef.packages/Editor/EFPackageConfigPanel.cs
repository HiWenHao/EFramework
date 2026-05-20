/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-19 17:04:50
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-19 17:04:50
 * ScriptVersion: 0.1
 * ===============================================
 */

#region UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using EasyFramework.Edit.Packages;
using EasyFramework.Edit.Windows;
using EasyFramework.Edit.Windows.ConfigPanel;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 管理项目相关包资产
    /// </summary>
    [EFConfigPanel(Priority = 10)]
    public class EFPackageConfigPanel : EFConfigPanelBase
    {
        private const string PackageFolderPath = ".git?path=/EF_Unity/Packages/";           // 远端Package文件夹地址
        private const string EFPackageCachePath = "Packages/cn.efefef.packages/Editor Resources/EFPackageCache.json";

        public override string Name => "EF" + LC.Combine(Lc.S, Lc.Package, Lc.Assets);

        private bool _allUnfold;
        private bool _hasProcess;
        private string _token;
        private string[] _createPackageTips;

        private ServerType _serverType;
        private Vector2 _scrollPosition;

        private Dictionary<string, bool> _packageFolder;

        private List<EFPackageInfo> _packages;
        
        private PackageConfig _config;
        private EFPackageInfo _newPackageInfo;

        private AddRequest _addRequest;
        private RemoveRequest _removeRequest;

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
            // if (EditorUtils.TimestampIsExceeded(_config.lastUpdateTimestamp, 180))
            //     GetLocalPackages();//FindCustomPackages();
        }

        public override void OnDestroy()
        {
            if (null == _config) return;
            ServerToolkit.SavePackageConfig(_config);
        }

        public override void LoadWindowData()
        {
            if (null != _config)
                return;

            _packageFolder = new Dictionary<string, bool>();

            _createPackageTips = new[]
            {
                LC.Combine(Lc.Package, Lc.Name) + "cn.efefef.{0}",
                LC.Combine(Lc.Package, Lc.Description),
                LC.Combine(Lc.Author, Lc.Name),
            };

            _allUnfold = true;

            _packages = new List<EFPackageInfo>();
            _config = ServerToolkit.GetPackageConfig() ?? new PackageConfig();

            foreach (EFPackageInfo packageInfo in _config.packagesInfo)
            {
                _packageFolder.Add(packageInfo.DisplayName, true);
            }
            _token = ServerToolkit.GetToken(_config.serverType);
        }

        public override void OnGUI()
        {
            #region GitToggle

            _config.serverType = (ServerType)EditorGUILayout.EnumPopup(
                $"{LC.Combine(Lc.Use)} {_config.serverType} {LC.Combine(Lc.Update)}", _config.serverType);
            if (_config.serverType != ServerType.Local)
            {
                if (_serverType != _config.serverType)
                {
                    _serverType = _config.serverType;
                    _token = ServerToolkit.GetToken(_config.serverType);
                }
                EditorGUILayout.BeginHorizontal();
                _token = EditorGUILayout.TextField($"{_config.serverType} Token", _token);
                if (GUILayout.Button(LC.Combine(Lc.Record), GUILayout.Width(80)))
                    ServerToolkit.SetToken(_config.serverType, _token);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(12.0f);

            #endregion

            #region Top Button

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(Lc.Update, Lc.All, Lc.Information), GUIUtils.Button(Color.green)))
            {
                if (EditorUtils.TimestampIsExceeded(_config.lastUpdateTimestamp, 180))
                {
                    GetLocalPackages();
                    UpdateAllFromGitOrGitee().Forget();
                }
                else
                    EditorUtility.DisplayDialog(LC.Combine(Lc.Tips),
                        $"{LC.Combine(Lc.Request, Lc.Too, Lc.Frequently)}, {LC.Combine(Lc.PleaseWaitMoment, Lc.TryAgain)}",
                        LC.Combine(Lc.Ok));
            }

            string created = LC.Combine(Lc.Create, Lc.New, Lc.Package);
            if (GUILayout.Button(created, GUIUtils.Button(Color.cyan)))
                CustomInputWindow.ShowWindow(created, _createPackageTips, CreatePackage);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(12.0f);

            #endregion

            #region Draw PackInfo

            if (GUILayout.Button(LC.Combine(Lc.All, Lc.Content, _allUnfold ? Lc.Fold : Lc.Unfold)))
                FoldAll();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUIUtils.ScrollViewBackground());
            foreach (EFPackageInfo packageInfo in _config.packagesInfo)
            {
                DrawSinglePackage(packageInfo);
            }
            EditorGUILayout.Space(12.0f);
            EditorGUILayout.EndScrollView();

            #endregion

        }

        private void FoldAll()
        {
            _allUnfold = !_allUnfold;
            var keys = new List<string>(_packageFolder.Keys);
            foreach (var key in keys)
            {
                _packageFolder[key] = _allUnfold;
            }
        }

        private void DrawSinglePackage(EFPackageInfo packageInfo)
        {
            EditorGUILayout.BeginVertical(GUIUtils.BackgroundStyle(), GUILayout.ExpandWidth(true));
            
            _packageFolder[packageInfo.DisplayName] = EditorGUILayout.BeginFoldoutHeaderGroup(
                _packageFolder[packageInfo.DisplayName], packageInfo.DisplayName, GUIUtils.Title());

            if (_packageFolder[packageInfo.DisplayName])
            {
                Lc versionType = CompareVersion(packageInfo.CurrentVersion, packageInfo.ServerVersion);

                EditorGUILayout.LabelField(packageInfo.Description, GUILayout.ExpandWidth(true));
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(LC.Combine(new[] { Lc.Current, Lc.Version }) + $":  {packageInfo.CurrentVersion}", GUIUtils.Text(),GUILayout.Width(150));
                string serverContents = versionType == Lc.Non ? LC.Combine(new[] { Lc.Not, Lc.Exist }) : packageInfo.ServerVersion;
                EditorGUILayout.LabelField(LC.Combine(new[] { Lc.Server, Lc.Version }) + $":  {serverContents}", GUIUtils.Text(),GUILayout.Width(150));
                DrawButton(versionType, packageInfo);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private Lc CompareVersion(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1))
                return Lc.Download;
            if (string.IsNullOrEmpty(v2))
                return Lc.Non;

            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            for (int i = 0; i < 3; i++)
            {
                if (parts1[i] == parts2[i])
                    continue;

                int n1 = int.Parse(parts1[i]);
                int n2 = int.Parse(parts2[i]);

                if (n1 < n2)
                    return Lc.Update;
                if (n1 > n2)
                    return Lc.Upload;
            }

            return Lc.Unload;
        }

        private void DrawButton(Lc versionType, EFPackageInfo packageInfo)
        {
            if (versionType == Lc.Upload && packageInfo.FromGit)
                return;

            Lc type = versionType == Lc.Non ? Lc.Unload : versionType;
            Color textColor = type switch
            {
                Lc.Download or Lc.Update => Color.green,
                Lc.Unload => GUIUtils.LightRed,
                _ => Color.white
            };

            if (!GUILayout.Button(LC.Combine(type), GUIUtils.Button(textColor), GUILayout.Width(140)))
                return;

            if (_hasProcess)
            {
                D.Warning(LC.Combine(Lc.Current, Lc.Have, Lc.One, Lc.Task, Lc.PleaseWaitMoment, Lc.TryAgain));
                return;
            }

            switch (versionType)
            {
                case Lc.Download:
                case Lc.Update:
                    CustomProgressWindow.ShowWindow(
                        LC.Combine(new[] { versionType, Lc.Package, Lc.Assets, Lc.PleaseWaitMoment }), null);
                    string path = ServerToolkit.GetFrameworkPath(_config.serverType) + PackageFolderPath;
                    _addRequest = Client.Add($"{path}{packageInfo.Name}");
                    EditorApplication.update += AddPackageProgress;

                    packageInfo.CurrentVersion = packageInfo.ServerVersion;
                    packageInfo.NeedUpdate = false;
                    break;
                case Lc.Unload:
                    _removeRequest = Client.Remove(packageInfo.Name);
                    EditorApplication.update += RemovePackageProgress;

                    packageInfo.CurrentVersion = "";
                    packageInfo.NeedUpdate = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(versionType), versionType, null);
            }

            _hasProcess = true;
        }

        #region Client Task - 客户端任务

        // 增加一个Package包的相关进程
        private void AddPackageProgress()
        {
            if (_addRequest is not { IsCompleted: true })
                return;

            if (_addRequest.Status != StatusCode.Success)
                D.Error($"{LC.Combine(new[] { Lc.Install, Lc.Error })}: {_addRequest.Error.message}");

            CustomProgressWindow.CloseWindow();
            EditorApplication.update -= AddPackageProgress;
            _addRequest = null;
            _hasProcess = false;
        }

        // 删除一个Package包的相关进程
        private void RemovePackageProgress()
        {
            if (_removeRequest is not { IsCompleted: true })
                return;

            if (_removeRequest.Status != StatusCode.Success)
                D.Error($"{LC.Combine(new[] { Lc.Unload, Lc.Error })}{_removeRequest.Error.message}");

            EditorApplication.update -= RemovePackageProgress;
            _removeRequest = null;
            _hasProcess = false;
        }

        #endregion

        #region Update all package info - 更新数据

        private void GetLocalPackages()
        {
            _packages.Clear();
            _packages = new List<EFPackageInfo>(_config.packagesInfo);
            _config.packagesInfo.Clear();

            foreach (var packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                var name = packageInfo.name;
                if (!name.Contains("cn.efefef."))
                    continue;

                string displayName = string.IsNullOrEmpty(packageInfo.displayName) ? name : packageInfo.displayName;
                _config.packagesInfo.Add(new EFPackageInfo()
                {
                    Name = name,
                    DisplayName = displayName,
                    FromGit = true,
                    Description = packageInfo.description,
                    CurrentVersion = packageInfo.version,
                    ServerVersion = "0.1.0",
                    //packageInfo.source == PackageSource.Git ? packageInfo.git.revision : "0.1.0" //.git.revision = "HEAD"
                });
            }
        }

        private async UniTask UpdateAllFromGitOrGitee()
        {
            var assetData = await DownloadFileAsync(ServerToolkit.GetRawUrl(_config.serverType,
                ServerToolkit.GetFrameworkOwner(_config.serverType), "EFramework", "master", "EF_Unity/" + EFPackageCachePath));

            if (string.IsNullOrEmpty(assetData))
            {
                _config.packagesInfo =  new List<EFPackageInfo>(_packages);
                return;
            }
            _packageFolder.Clear();
            PackageConfig newConfig = PackageConfig.FromJson(assetData);

            var infoList = new List<EFPackageInfo>();
            for (var i = 0; i < newConfig.packagesInfo.Count; i++)
            {
                bool needAdded = true;
                var newInfo = newConfig.packagesInfo[i];
                _packageFolder.Add(newInfo.DisplayName, true);
                for (var j = 0; j < _config.packagesInfo.Count; j++)
                {
                    var oldInfo = _config.packagesInfo[j];
                    if (oldInfo.Name != newInfo.Name) continue;
                    if (EditorUtils.CompareVersion(oldInfo.CurrentVersion, newInfo.CurrentVersion))
                        continue;
                    oldInfo.ServerVersion = newInfo.CurrentVersion;
                    oldInfo.NeedUpdate = true;
                    oldInfo.DisplayName = newInfo.DisplayName;
                    oldInfo.Description = newInfo.Description;
                    _config.packagesInfo[j] = oldInfo;
                    needAdded = false;
                    break;
                }

                if (!needAdded) continue;
                newInfo.FromGit = true;
                newInfo.NeedUpdate = true;
                newInfo.CurrentVersion = "";
                infoList.Add(newInfo);
            }

            foreach (var info in infoList)
            {
                _config.packagesInfo.Add(info);
            }
            infoList.Clear();
            
            ServerToolkit.SavePackageConfig(_config.ToJson());
        }

        private async UniTask<string> DownloadFileAsync(string url)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            if (!string.IsNullOrEmpty(_config.token))
                request.SetRequestHeader("Authorization", $"Bearer {_config.token}");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) 
                return request.downloadHandler.text;
            
            D.Error(LC.Combine(Lc.Update, Lc.Error));
            return null;
        }

        #endregion

        #region Create Package - 创建一个新包

        private void CreatePackage(string packageName, string packageDes, string author)
        {
            if (string.IsNullOrEmpty(packageName) || string.IsNullOrEmpty(packageDes) || string.IsNullOrEmpty(author))
            {
                D.Warning("Please try again, param: packageName, packageDes, author, all three are indispensable.");
                return;
            }
            string packagePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", $"cn.efefef.{packageName.ToLower()}"));
            Directory.CreateDirectory(packagePath);

            try
            {
                CreatePackageJson(packageName, packageDes, author, packagePath);
                CreateAssemblyReference(packageName, packagePath);
            }
            catch (Exception e)
            {
                D.Warning(e);
            }
            finally
            {
                D.Emphasize($"CreatePackage succeed， {packageName}");
                
                _config.packagesInfo.Add(_newPackageInfo);
                _packageFolder.TryAdd(_newPackageInfo.DisplayName, true);
                ServerToolkit.SavePackageConfig(_config);
                
                Client.Resolve();
                EditorCommands.SaveAssets();
                EditorCommands.Refresh();
                _newPackageInfo = null;
            }
        }

        private void CreatePackageJson(string packageName, string packageDes, string author, string rootPath)
        {
            string frameworkPath = ServerToolkit.GetFrameworkPath(_config.serverType);
            GitPackageConfig config = new GitPackageConfig()
            {
                Name = $"cn.efefef.{packageName.ToLower()}",
                DisplayName = $"EF.{packageName}",
                Version = "0.0.1",
                Unity = "2022.3",
                UnityRelease = "10f1",
                Description = packageDes,
                Author = new Author()
                {
                    Name = author,
                },
                ChangelogUrl = frameworkPath,
                DocumentationUrl = frameworkPath,
                LicensesUrl = $"{frameworkPath}?tab=MIT-1-ov-file",
                Dependencies = new Dictionary<string, string>()
                {
                    { "cn.efefef.core", "0.0.1" }
                }
            };

            _newPackageInfo = new EFPackageInfo()
            {
                Name = config.Name,
                DisplayName = config.DisplayName,
                FromGit = false,
                Description = config.Description,
                CurrentVersion = config.Version,
                ServerVersion = "",
            };

            string packageJson = Path.Combine(rootPath, "package.json");
            File.WriteAllText(packageJson, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private void CreateAssemblyReference(string packageName, string rootPath)
        {
            D.Warning(rootPath);
            string editorGuid = AssetDatabase.AssetPathToGUID("Packages/cn.efefef.core/Editor/EF.Editor.asmdef");
            string runtimeGuid = AssetDatabase.AssetPathToGUID("Packages/cn.efefef.core/Runtime/EF.Runtime.asmdef");
            string packageEditorPath = $"{rootPath}/Editor";
            string packageRuntimePath = $"{rootPath}/Runtime";

            Directory.CreateDirectory(packageEditorPath);
            Directory.CreateDirectory(packageRuntimePath);
            File.WriteAllText($"{packageEditorPath}/EF.{packageName}.Editor.asmref",
                $"{{\"reference\": \"GUID:{editorGuid}\"}}");
            File.WriteAllText($"{packageRuntimePath}/EF.{packageName}.Runtime.asmref",
                $"{{\"reference\": \"GUID:{runtimeGuid}\"}}");
        }

        #endregion
    }
}

#endregion