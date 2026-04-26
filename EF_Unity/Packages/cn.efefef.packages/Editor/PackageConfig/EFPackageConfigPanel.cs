/*
 * ================================================
 * Describe:      管理项目相关包资产.
 * Author:        Alvin8412
 * CreationTime:  2026-04-13 14:51:47
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-13 14:51:47
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EasyFramework.Edit.Windows;
using EasyFramework.Edit.Windows.ConfigPanel;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyFramework.Edit.Packages
{
    /// <summary>
    /// 管理项目相关包资产
    /// </summary>
    [EFConfig]
    public class EFPackageConfigPanel : EFConfigPanelBase
    {
        public override int Priority => -1;
        public override string Name => LC.Combine(new Lc[] { Lc.Package, Lc.Assets });

        private const string GitPackagePath = "https://github.com/HiWenHao/EFramework.git?path=/EF_Unity/Packages/";
        private const string GiteePackagePath = "https://gitee.com/AlvinCN/EFramework.git?path=EF_Unity/Packages/";
        
        /// <summary> 远端服务地址 </summary>
        private const string ServerGitPath =
            "https://raw.githubusercontent.com/HiWenHao/EFramework/master/EF_Unity/Packages";

        /// <summary>
        /// 本地资源配置路径
        /// </summary>
        private const string LocationConfigPath = "Packages/cn.efefef.packages/Editor Resources/EFPackageConfig.asset";

        private int _progressCount;
        private bool _updatingVersionInfos;
        private bool _updatingPackageInfos;
        private bool _inEfFoldoutHeader = true;
        private bool _notInEfFoldoutHeader = true;
        private Vector2 _scrollPosition;

        private AddRequest _addRequest;
        private RemoveRequest _removeRequest;

        private PackageConfig _config;
        private EFPackageInfo _currentPackageInfo;
        private SerializedObject _packageConfig;

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }

        public override void LoadWindowData()
        {
            if (null != _config)
                return;

            _config = AssetDatabase.LoadAssetAtPath<PackageConfig>(LocationConfigPath);
            _packageConfig = new SerializedObject(_config);
        }

        public override void OnGUI()
        {
            DrawUpdateVersionsInfo();

            if (_updatingVersionInfos || _packageConfig == null || null == _config)
                return;

            using var changeCheckScope = new EditorGUI.ChangeCheckScope();

            #region Use Git

            _config.useGit = EditorGUILayout.ToggleLeft("GitHub", _config.useGit);
            if (_config.useGit)
                _config.gitToken = EditorGUILayout.TextField("Git Token", _config.gitToken);
            EditorGUILayout.Space(12.0f);

            #endregion
            
            #region Top Button

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(new[] { Lc.Update, Lc.All, Lc.Information }),
                    GUIUtils.Button(Color.green, 16),
                    GUILayout.Height(40)))
            {
                if (EditorUtils.TimestampIsExceeded(_config.lastUpdateTimestamp, 180))
                    UpdateAll();
                else
                    EditorUtility.DisplayDialog(LC.Combine(Lc.Tips),
                        $"{LC.Combine(new[] { Lc.Request, Lc.Too, Lc.Frequently })}, {LC.Combine(new[] { Lc.PleaseWaitMoment, Lc.TryAgain })}",
                        LC.Combine(Lc.Ok));
            }

            // if (GUILayout.Button(LC.Combine(new[] { Lc.Update, Lc.All, Lc.Framework, Lc.Package }),
            //         GUIUtils.Button(Color.green, 16), GUILayout.Height(40)))
            // {
            // }

            if (GUILayout.Button(LC.Combine(new[] { Lc.Create, Lc.Package }), GUIUtils.Button(Color.cyan, 16),
                    GUILayout.Height(40)))
            {
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(12.0f);

            #endregion

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUIUtils.ScrollViewBackground());

            #region EFFoldoutHeader

            _inEfFoldoutHeader = EditorGUILayout.BeginFoldoutHeaderGroup(_inEfFoldoutHeader,
                _inEfFoldoutHeader
                    ? LC.Combine(new Lc[] { Lc.Close, Lc.Assets, Lc.Package, Lc.List })
                    : LC.Combine(new Lc[] { Lc.Open, Lc.Assets, Lc.Package, Lc.List }));
            if (_inEfFoldoutHeader)
            {
                int count = _config.packagesInfo.Count;
                for (int i = 0; i < count; i++)
                {
                    DrawOnePackage(_config.packagesInfo[i]);
                }
            }

            EditorGUILayout.Space(12.0f);
            EditorGUILayout.EndFoldoutHeaderGroup();

            #endregion

            #region OtherFoldoutHeader

            _notInEfFoldoutHeader = EditorGUILayout.BeginFoldoutHeaderGroup(_notInEfFoldoutHeader,
                _notInEfFoldoutHeader
                    ? LC.Combine(new Lc[] { Lc.Close, Lc.Other, Lc.Package, Lc.List })
                    : LC.Combine(new Lc[] { Lc.Open, Lc.Other, Lc.Package, Lc.List }));
            if (_notInEfFoldoutHeader)
            {
            }

            EditorGUILayout.Space(12.0f);
            EditorGUILayout.EndFoldoutHeaderGroup();

            #endregion

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(12.0f);

            if (!changeCheckScope.changed) return;
            _packageConfig.ApplyModifiedPropertiesWithoutUndo();
            _packageConfig.ApplyModifiedProperties();
        }

        private void DrawOnePackage(EFPackageInfo packageInfo)
        {
            Lc versionType = CompareVersion(packageInfo.CurrentVersion, packageInfo.ServerVersion);
            EditorGUILayout.BeginVertical(GUIUtils.BackgroundStyle(), GUILayout.ExpandWidth(true));

            EditorGUILayout.SelectableLabel(packageInfo.DisplayName, GUIUtils.Title());
            EditorGUILayout.LabelField(packageInfo.Description, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LC.Combine(new[] { Lc.Current, Lc.Version }), GUIUtils.Text(),
                GUILayout.Width(80));
            EditorGUILayout.LabelField(packageInfo.CurrentVersion, GUIUtils.Text(), GUILayout.ExpandWidth(true));
            DrawButton(versionType, packageInfo);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LC.Combine(new[] { Lc.Server, Lc.Version }), GUIUtils.Text(),
                GUILayout.Width(80));
            string serverContents = versionType == Lc.Non
                ? LC.Combine(new[] { Lc.Not, Lc.Exist })
                : packageInfo.ServerVersion;
            EditorGUILayout.LabelField(serverContents, GUIUtils.Text(), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
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

            if (GUILayout.Button(LC.Combine(type), GUIUtils.Button(textColor), GUILayout.Width(160)))
            {
                switch (versionType)
                {
                    case Lc.Download:
                    case Lc.Update:
                        //Client.Add(packageInfo.Name);
                        CustomProgressWindow.ShowWindow(LC.Combine(new[] { versionType, Lc.Package, Lc.Assets, Lc.PleaseWaitMoment }), null);
                        string path = _config.useGit ? GitPackagePath : GiteePackagePath;
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
                D.Emphasize($"{versionType}");
            }
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

        #region Common

        /// <summary>
        /// 可以打开进度弹窗
        /// </summary>
        /// <param name="title">进度标题</param>
        /// <param name="cancelAction">取消回调</param>
        private bool CanOpenProgressWindow(string title, Action<bool> cancelAction)
        {
            if (CustomProgressWindow.ShowWindow(title, cancelAction))
                return true;

            EditorUtility.DisplayDialog(LC.Combine(Lc.Tips),
                $"{LC.Combine(new[] { Lc.Already, Lc.Have, Lc.One, Lc.Request })}, {LC.Combine(new[] { Lc.PleaseWaitMoment, Lc.TryAgain })}",
                LC.Combine(Lc.Ok));
            return false;
        }

        #endregion

        #region Download || Unload

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
        
        private void AddPackageProgress()
        {
            if (_addRequest is not { IsCompleted: true })
                return;

            if (_addRequest.Status != StatusCode.Success)
                D.Error($"{LC.Combine(new[] { Lc.Install, Lc.Error })}: {_addRequest.Error.message}");
            else
                _ = SetPackageInfo(true);
            CustomProgressWindow.CloseWindow();
            EditorApplication.update -= AddPackageProgress;
            _currentPackageInfo = null;
            _addRequest = null;
        }

        private void RemovePackageProgress()
        {
            if (_removeRequest is not { IsCompleted: true })
                return;

            if (_removeRequest.Status != StatusCode.Success)
                D.Error($"{LC.Combine(new[] { Lc.Unload, Lc.Error })}{_removeRequest.Error.message}");
            else
                _ = SetPackageInfo(false);

            EditorApplication.update -= RemovePackageProgress;
            _currentPackageInfo = null;
            _removeRequest = null;
        }

        #endregion

        private async void UpdateAll()
        {
            try
            {
                UpdateLocationPackagesInfo();
                await GetPackageListFromGithub();
                await StartCheckAllPackageVersions();
            }
            catch (Exception e)
            {
                D.Exception(e);
            }
        }

        #region Packages Update

        private const string Branch = "master";
        private const string GithubAPIBase = "https://api.github.com/repos/";

        private void CancelUpdatePackagesInfo(bool fromSelf)
        {
            if (!_updatingPackageInfos)
                return;
            _updatingPackageInfos = false;

            if (fromSelf)
                CustomProgressWindow.CloseWindow();
        }

        private async Task GetPackageListFromGithub()
        {
            if (!CanOpenProgressWindow(LC.Combine(new[] { Lc.Update, Lc.Package, Lc.List }), CancelUpdatePackagesInfo))
                return;

            _updatingPackageInfos = true;
            string apiUrl = $"{GithubAPIBase}HiWenHao/EFramework/contents/EF_Unity/Packages?ref={Branch}";
            using UnityWebRequest request = UnityWebRequest.Get(apiUrl);
            request.SetRequestHeader("User-Agent", "UnityEditor");
            request.SetRequestHeader("Accept", "application/vnd.github.v3+json");
            if (_config.useGit && !string.IsNullOrEmpty(_config.gitToken))
                request.SetRequestHeader("Authorization", $"Bearer {_config.gitToken}");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (!_updatingPackageInfos)
                return;

            if (request.result != UnityWebRequest.Result.Success)
            {
                D.Error(
                    $"{LC.Combine(new[] { Lc.Request, Lc.Package, Lc.List, Lc.Error })},[{request.responseCode}]\t {request.downloadHandler.text}");
                CancelUpdatePackagesInfo(true);
                return;
            }

            string json = request.downloadHandler.text;
            var items = JsonConvert.DeserializeObject<List<GitHubContentItem>>(json);

            string pattern = @"^cn\.efefef\.";
            foreach (var item in items)
            {
                if (item.Type != "dir" || !Regex.IsMatch(item.Name, pattern))
                    continue;

                bool needAdded = true;
                for (int i = _config.packagesInfo.Count - 1; i >= 0; i--)
                {
                    var packageInfo = _config.packagesInfo[i];

                    if (packageInfo.Name != item.Name)
                        continue;

                    needAdded = false;
                    break;
                }

                if (needAdded)
                    _config.packagesInfo.Add(new EFPackageInfo()
                    {
                        Name = item.Name,
                        DisplayName = item.Name,
                        FromGit = true,
                        Description = "",
                        CurrentVersion = "",
                        ServerVersion = "",
                    });
            }

            CancelUpdatePackagesInfo(true);
        }

        #endregion

        #region Version Update

        //  绘制 - 更新版本号
        private void DrawUpdateVersionsInfo()
        {
            if (!_updatingVersionInfos)
                return;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField(LC.Combine(new[] { Lc.Updating, Lc.PleaseWaitMoment }), EditorStyles.boldLabel);
            EditorGUILayout.Space(15);

            if (GUILayout.Button(LC.Combine(Lc.Cancel), GUILayout.Height(25)))
                CancelUpdateVersionInfo(true);
        }

        //  取消 - 更新版本号
        private void CancelUpdateVersionInfo(bool fromSelf)
        {
            if (!_updatingVersionInfos)
                return;
            _updatingVersionInfos = false;

            if (fromSelf)
                CustomProgressWindow.CloseWindow();
        }

        //  开始 - 更新全部版本号
        private async Task StartCheckAllPackageVersions()
        {
            try
            {
                await Task.CompletedTask;
                if (!CanOpenProgressWindow(LC.Combine(new[] { Lc.Package, Lc.Information, Lc.Updating }),
                        CancelUpdateVersionInfo))
                    return;

                _progressCount = 0;
                _updatingVersionInfos = true;
                _config.lastUpdateTimestamp = EditorUtils.GetCurrentTimestamp();
                CustomProgressWindow.UpdateInfo(_progressCount, _config.packagesInfo.Count);

                foreach (var packageInfo in _config.packagesInfo)
                {
                    await Task.Delay(500);

                    if (!_updatingVersionInfos)
                        return;

                    StartCheckPackageVersion(packageInfo.Name);
                }
            }
            catch (Exception e)
            {
                D.Exception($"{LC.Combine(new[] { Lc.Request, Lc.Package, Lc.Error })}, {e.Message}");
            }
        }

        //  开始 - 更新单个版本号
        private async void StartCheckPackageVersion(string packageName)
        {
            try
            {
                D.Emphasize(packageName);
                string url = $"{ServerGitPath}/{packageName}/package.json";
                using UnityWebRequest request = UnityWebRequest.Get(url);
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                ++_progressCount;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    D.Error(
                        $"{LC.Combine(new[] { Lc.Request, Lc.Package, Lc.Error })},[{packageName}]\t {request.error}");

                    if (_progressCount >= _config.packagesInfo.Count)
                        CancelUpdateVersionInfo(true);

                    return;
                }

                if (_progressCount >= _config.packagesInfo.Count)
                {
                    CancelUpdateVersionInfo(true);
                    return;
                }

                if (!_updatingVersionInfos)
                    return;

                GitPackageConfig packageNewInfo =
                    JsonConvert.DeserializeObject<GitPackageConfig>(request.downloadHandler.text);

                bool hasInfo = false;
                foreach (var packageInfo in _config.packagesInfo)
                {
                    if (!packageInfo.Name.Equals(packageName))
                        continue;
                    hasInfo = true;
                    packageInfo.ServerVersion = packageNewInfo.Version;
                    packageInfo.Description = packageNewInfo.Description;
                    break;
                }

                if (!hasInfo)
                {
                    _config.packagesInfo.Add(new EFPackageInfo()
                    {
                        Name = packageNewInfo.Name,
                        DisplayName = string.IsNullOrEmpty(packageNewInfo.DisplayName)
                            ? packageNewInfo.Name
                            : packageNewInfo.DisplayName,
                        FromGit = true,
                        Description = packageNewInfo.Description,
                        CurrentVersion = packageNewInfo.Version,
                        ServerVersion = packageNewInfo.Version,
                    });
                }

                CustomProgressWindow.UpdateInfo(_progressCount, _config.packagesInfo.Count);
            }
            catch (Exception e)
            {
                D.Exception($"{LC.Combine(new[] { Lc.Request, Lc.Package, Lc.Error })},[{packageName}]\t {e.Message}");
            }
        }

        //  本地 - 更新全部版本号
        private void UpdateLocationPackagesInfo()
        {
            for (int i = _config.packagesInfo.Count - 1; i >= 0; i--)
            {
                _config.packagesInfo.RemoveAt(i);
            }

            _config.packagesInfo.Clear();

            foreach (var packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                var name = packageInfo.name;
                if (!name.Contains("cn.efefef."))
                    continue;

                //packageInfo.git.
                _config.packagesInfo.Add(new EFPackageInfo()
                {
                    Name = name,
                    DisplayName = string.IsNullOrEmpty(packageInfo.displayName) ? name : packageInfo.displayName,
                    FromGit = packageInfo.source == PackageSource.Git,
                    Description = packageInfo.description,
                    CurrentVersion = packageInfo.version,
                    ServerVersion = "0.1.0",
                    //packageInfo.source == PackageSource.Git ? packageInfo.git.revision : "0.1.0" //.git.revision = "HEAD"
                });
            }
        }

        #endregion
    }
}