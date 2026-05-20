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
        private const string EditorResourcesPath = "Packages/cn.efefef.packages/Editor Resources/";     // 包相关资源存放路径
        private const string LocationConfigPath = EditorResourcesPath + "EFPackageConfig.asset";        // 本地资源配置路径
        private const string EFPackageConfigPath = "EF_Unity/Packages/cn.efefef.packages/Editor Resources/EFPackageConfig.asset";

        public override string Name => "EF" + LC.Combine(Lc.S, Lc.Package, Lc.Assets);

        private bool _isAlvin;
        private bool _allUnfold;
        private string _token;
        private string _packageRootPath;
        private string[] _createPackageTips;

        private ServerType _serverType;
        private Vector2 _scrollPosition;

        private Dictionary<string, bool> _packageFolder;
        
        private PackageConfig _config;
        private EFPackageInfo _currentPackageInfo;
        private SerializedObject _packageConfig;

        private AddRequest _addRequest;
        private RemoveRequest _removeRequest;

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
            _isAlvin = EditorPrefs.GetString("EF_EditorUser").StartsWith("Alvin");
            if (EditorUtils.TimestampIsExceeded(_config.lastUpdateTimestamp, 180))
                FindCustomPackages();
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

            _packageRootPath = Application.dataPath + "../Packages";
            _allUnfold = true;
            
            _config = AssetDatabase.LoadAssetAtPath<PackageConfig>(LocationConfigPath);
            _packageConfig = new SerializedObject(_config);
            foreach (EFPackageInfo packageInfo in _config.packagesInfo)
            {
                _packageFolder.Add(packageInfo.DisplayName, true);
            }

            _serverType = _config.serverType;
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
                    _token =  ServerToolkit.GetToken(_config.serverType);
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
                    UpdateAllFromGitOrGitee().Forget();
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

            if (!GUILayout.Button(LC.Combine(type), GUIUtils.Button(textColor),GUILayout.Width(140))) 
                return;
            
            switch (versionType)
            {
                case Lc.Download:
                case Lc.Update:
                    CustomProgressWindow.ShowWindow(LC.Combine(new[] { versionType, Lc.Package, Lc.Assets, Lc.PleaseWaitMoment }), null);
                    string path = ServerToolkit.GetFrameworkPath(_config.serverType) + PackageFolderPath;
                    _addRequest = Client.Add($"{path}{packageInfo.Name}");
                    EditorApplication.update += AddPackageProgress;
                    break;
                case Lc.Unload:
                    _removeRequest = Client.Remove(packageInfo.Name);
                    EditorApplication.update += RemovePackageProgress;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(versionType), versionType, null);
            }

            _currentPackageInfo = packageInfo;
        }

        // 设置Package信息，增加或删除某个包
        private async Task SetPackageInfo(bool added)
        {
            await Task.CompletedTask;
            if (null == _currentPackageInfo) 
                return;
            
            await Task.Delay(2000);
            if (!added)
            {
                _config.packagesInfo.Remove(_currentPackageInfo);
                _currentPackageInfo = null;
                return;
            }

            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName(_currentPackageInfo.Name);
            if (null != package)
            {
                _currentPackageInfo.DisplayName = package.displayName;
                _currentPackageInfo.Description = package.description;
                _currentPackageInfo.FromGit = true;
                _currentPackageInfo.CurrentVersion = package.version;
                _currentPackageInfo.ServerVersion = package.version;
            }
        }

        // 增加一个Package包的相关进程
        private void AddPackageProgress()
        {
            if (_addRequest is not { IsCompleted: true })
                return;

            if (_addRequest.Status != StatusCode.Success)
                D.Error($"{LC.Combine(new[] { Lc.Install, Lc.Error })}: {_addRequest.Error.message}");
            else
                SetPackageInfo(true).Dispose();
            CustomProgressWindow.CloseWindow();
            EditorApplication.update -= AddPackageProgress;
            _currentPackageInfo = null;
            _addRequest = null;
        }

        // 删除一个Package包的相关进程
        private void RemovePackageProgress()
        {
            if (_removeRequest is not { IsCompleted: true })
                return;

            if (_removeRequest.Status != StatusCode.Success)
                D.Error($"{LC.Combine(new[] { Lc.Unload, Lc.Error })}{_removeRequest.Error.message}");
            else
                SetPackageInfo(false).Dispose();

            EditorApplication.update -= RemovePackageProgress;
            _currentPackageInfo = null;
            _removeRequest = null;
        }

        private void FindCustomPackages()
        {
            string[] allSubDirs = Directory.GetDirectories(Path.GetFullPath("Packages"));

            List<string> matchedDirs = new List<string>();

            foreach (string dir in allSubDirs)
            {
                if (!Path.GetFileName(dir).StartsWith("cn.efefef."))
                    continue;
                matchedDirs.Add(dir);
            }

            _packageFolder.Clear();
            _config.packagesInfo.Clear();
            foreach (string dirPath in matchedDirs)
            {
                string jsonPath = Path.Combine(dirPath, "package.json");
                if (!File.Exists(jsonPath))
                    continue;
                GitPackageConfig config = JsonConvert.DeserializeObject<GitPackageConfig>(File.ReadAllText(jsonPath));
                _config.packagesInfo.Add(new EFPackageInfo
                {
                    Name = config.Name,
                    DisplayName = config.DisplayName,
                    FromGit = false,
                    NeedUpdate = false,
                    Description = config.Description,
                    ServerVersion = config.Version,
                    CurrentVersion = config.Version
                });
                
                _packageFolder.Add(config.DisplayName, true);
            }
        }

        private void GetPackages()
        {
            _config.packagesInfo.Clear();

            foreach (var packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                var name = packageInfo.name;
                if (!name.Contains("cn.efefef."))
                    continue;

                _config.packagesInfo.Add(new EFPackageInfo()
                {
                    Name = name,
                    DisplayName = string.IsNullOrEmpty(packageInfo.displayName) ? name : packageInfo.displayName,
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
            byte[] assetData = await DownloadFileAsync(ServerToolkit.GetRawUrl(_config.serverType,
                ServerToolkit.GetFrameworkOwner(_config.serverType), "EFramework", "master", EFPackageConfigPath));
    
            if (assetData == null) 
                return;

            if (_isAlvin)
            {
                D.Log($"配置加载成功");
                return;
            }
            
            await File.WriteAllBytesAsync(LocationConfigPath, assetData);
    
            EditorCommands.Refresh();
            
            _config = AssetDatabase.LoadAssetAtPath<PackageConfig>(LocationConfigPath);
        }

        // 异步下载指定 URL 的文件内容
        private async UniTask<byte[]> DownloadFileAsync(string url)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            if (!string.IsNullOrEmpty(_config.token))
                request.SetRequestHeader("Authorization", $"Bearer {_config.token}");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) 
                return request.downloadHandler.data;
            
            D.Error(LC.Combine(Lc.Update, Lc.Error));
            return null;
        }


        #region Create Package - 创建一个新包

        private void CreatePackage(string packageName, string packageDes, string author)
        {
            if (string.IsNullOrEmpty(packageName) || string.IsNullOrEmpty(packageDes) || string.IsNullOrEmpty(author))
            {
                D.Warning("Please try again, param: packageName, packageDes, author, all three are indispensable.");
                return;
            }

            string packageNameToLower = $"cn.efefef.{packageName.ToLower()}";
            string packagePath = $"{_packageRootPath}/{packageNameToLower}";
            Directory.CreateDirectory(packagePath);

            try
            {
                CreatePackageJson(packageName, packageDes, author, packagePath);
                CreateAssemblyReference(packageName, packagePath);
            }
            finally
            {
                D.Emphasize($"CreatePackage succeed， {packageName}");
                _packageConfig.ApplyModifiedProperties();
                EditorCommands.SaveAssets();
                EditorCommands.Refresh();
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

            _config.packagesInfo.Add(new EFPackageInfo()
            {
                Name = config.Name,
                DisplayName = config.DisplayName,
                FromGit = false,
                Description = config.Description,
                CurrentVersion = config.Version,
                ServerVersion = "",
            });

            string packageJson = Path.Combine(rootPath, "package.json");
            File.WriteAllText(packageJson, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private void CreateAssemblyReference(string packageName, string rootPath)
        {
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