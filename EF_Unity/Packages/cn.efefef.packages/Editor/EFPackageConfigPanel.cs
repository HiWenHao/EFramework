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

        public override string Name => "EF" + LC.Combine(Lc.S, Lc.Package, Lc.Assets);

        private bool _allUnfold;
        private bool _hasProcess;
        private string _token;
        private string[] _createPackageTips;

        private ServerType _serverType;
        private Vector2 _scrollPosition;

        private Dictionary<string, bool> _packageFolder;

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

            #region Summary - 摘要

            _config.RefreshSummary();
            EditorGUILayout.BeginHorizontal(GUIUtils.BackgroundStyle());
            DrawSummaryItem(LC.Combine(Lc.Total), _config.totalCount, Color.white);
            DrawSummaryItem(LC.Combine(Lc.Local), _config.localCount, Color.cyan);
            DrawSummaryItem(LC.Combine(Lc.Git), _config.gitCount, Color.yellow);
            DrawSummaryItem(LC.Combine(Lc.No, Lc.Install), _config.notInstalledCount, GUIUtils.LightRed);
            if (_config.needUpdateCount > 0)
                DrawSummaryItem(LC.Combine(Lc.Need, Lc.Update), _config.needUpdateCount, Color.gray);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(12.0f);

            #endregion

            #region Top Buttons - 顶部操作按钮

            bool isMaintainer = ServerToolkit.IsFrameworkProject;
            
            if (isMaintainer)
            {
                EditorGUILayout.LabelField(LC.Combine(Lc.Maintain, Lc.Operating) + "（" + LC.Combine(Lc.To, Lc.Git) + "）", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(LC.Combine(Lc.Generate) + " & " + LC.Combine(Lc.Save, Lc.Catalogue), GUIUtils.Button(Color.cyan), GUILayout.Width(220)))
                {
                    var catalog = ServerToolkit.GenerateCatalogFromLocalPackages();
                    ServerToolkit.SaveCatalog(catalog);
                    GetLocalPackages();
                    MergeCatalogToConfig(catalog);
                    ServerToolkit.SavePackageConfig(_config);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(8f);
            }

            EditorGUILayout.LabelField($"{LC.Combine(Lc.Develop, Lc.Operating)}（{LC.Combine(Lc.Remote, Lc.Sync)}）", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(Lc.Update, Lc.All, Lc.Information), GUIUtils.Button(Color.green)))
            {
                GetLocalPackages();
                UpdateAllFromGitOrGitee().Forget();
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

        /// <summary>
        /// 绘制摘要统计项
        /// </summary>
        private void DrawSummaryItem(string label, int count, Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = color },
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                stretchWidth = true,
            };
            GUILayout.Label($"  {label}: {count}  ", style);
        }

        private void DrawSinglePackage(EFPackageInfo packageInfo)
        {
            EditorGUILayout.BeginVertical(GUIUtils.BackgroundStyle(), GUILayout.ExpandWidth(true));

            var titleStyle = GUIUtils.Title();
            float headerHeight = 22f;

            // 全宽可点击的标题行
            var headerRect = GUILayoutUtility.GetRect(0, headerHeight, GUILayout.ExpandWidth(true));

            // 折叠箭头 + 包名（这是 foldout 自己的点击区域）
            float textWidth = titleStyle.CalcSize(new GUIContent(packageInfo.DisplayName)).x;
            var foldRect = new Rect(headerRect.x + 4, headerRect.y, textWidth + 24, headerRect.height);
            _packageFolder[packageInfo.DisplayName] = EditorGUI.Foldout(
                foldRect, _packageFolder[packageInfo.DisplayName], packageInfo.DisplayName, true, titleStyle);

            // 来源标签（右对齐）
            Color tagColor = packageInfo.SourceType switch
            {
                EFPackageSource.Local => Color.cyan,
                EFPackageSource.Git => Color.yellow,
                EFPackageSource.NotInstalled => GUIUtils.LightRed,
                _ => Color.gray,
            };
            var tagLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                normal = { textColor = tagColor },
            };
            string tagContent = $"[{GetSourceLabel(packageInfo.SourceType)}]";
            float tagWidth = tagLabelStyle.CalcSize(new GUIContent(tagContent)).x + 8;
            var tagRect = new Rect(headerRect.xMax - tagWidth - 4, headerRect.y, tagWidth, headerRect.height);
            GUI.Label(tagRect, tagContent, tagLabelStyle);

            // 整行点击：foldout 区域之外的地方也能展开/折叠
            if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
            {
                if (!foldRect.Contains(Event.current.mousePosition))
                {
                    _packageFolder[packageInfo.DisplayName] = !_packageFolder[packageInfo.DisplayName];
                    Event.current.Use();
                    GUI.changed = true;
                }
            }

            if (_packageFolder[packageInfo.DisplayName])
            {
                Lc versionType = CompareVersion(packageInfo.CurrentVersion, packageInfo.ServerVersion);

                EditorGUILayout.LabelField(packageInfo.Description, GUILayout.ExpandWidth(true));
                EditorGUILayout.Space();

                // 版本行
                EditorGUILayout.BeginHorizontal();
                string currentText = string.IsNullOrEmpty(packageInfo.CurrentVersion)
                    ? LC.Combine(Lc.Non)
                    : packageInfo.CurrentVersion;
                string serverText = string.IsNullOrEmpty(packageInfo.ServerVersion)
                    ? LC.Combine(Lc.Not, Lc.Exist)
                    : packageInfo.ServerVersion;
                
                Color currentColor = packageInfo.SourceType == EFPackageSource.Local
                    ? Color.green : Color.white;
                
                EditorGUILayout.LabelField(
                    $"{LC.Combine(Lc.Current, Lc.Version)}:  {currentText}", 
                    GUILayout.Width(160));
                EditorGUILayout.LabelField(
                    $"{LC.Combine(Lc.Server, Lc.Version)}:  {serverText}", 
                    GUILayout.Width(160));
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                
                // 按钮行
                DrawButtons(packageInfo, versionType);

                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// 根据来源类型获取标签文字
        /// </summary>
        private string GetSourceLabel(EFPackageSource source)
        {
            return source switch
            {
                EFPackageSource.Local => LC.Combine(Lc.Local),
                EFPackageSource.Git => LC.Combine(Lc.Git),
                EFPackageSource.NotInstalled => LC.Combine(Lc.Not, Lc.Install),
                _ => LC.Combine(Lc.Unknown),
            };
        }

        /// <summary>
        /// 根据包来源和版本差，绘制对应操作按钮
        /// </summary>
        private void DrawButtons(EFPackageInfo packageInfo, Lc versionType)
        {
            EditorGUILayout.BeginHorizontal();

            switch (packageInfo.SourceType)
            {
                case EFPackageSource.Local:
                    DrawLocalButtons(packageInfo, versionType);
                    break;
                case EFPackageSource.Git:
                    DrawGitButtons(packageInfo, versionType);
                    break;
                case EFPackageSource.NotInstalled:
                    DrawNotInstalledButtons(packageInfo);
                    break;
                case EFPackageSource.Unknown:
                default:
                    DrawUnknownButtons(packageInfo);
                    break;
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 本地包按钮：打开目录 + 发布/同步/卸载
        /// </summary>
        private void DrawLocalButtons(EFPackageInfo packageInfo, Lc versionType)
        {
            // 按钮1：打开本地目录
            if (GUILayout.Button(LC.Combine(Lc.Open, Lc.Folder), GUILayout.Width(120)))
                OpenPackageDirectory(packageInfo.Name);

            // 按钮2：根据版本差显示不同动作
            string actionLabel = versionType switch
            {
                Lc.Update => LC.Combine(Lc.Sync, Lc.Update),      // 服务端更新 → 同步更新
                Lc.Upload => LC.Combine(Lc.Upload, Lc.Version),   // 本地更新 → 发布版本
                _ => LC.Combine(Lc.Latest),                                     // 一致 → 已最新
            };

            Color actionColor = versionType switch
            {
                Lc.Update => Color.green,
                Lc.Upload => Color.cyan,
                _ => Color.gray,
            };

            bool canAction = versionType != Lc.Unload;
            EditorGUI.BeginDisabledGroup(!canAction);
            if (GUILayout.Button(actionLabel, GUIUtils.Button(actionColor), GUILayout.Width(140)))
            {
                // 本地包的操作后续可以扩展：更新 package.json 版本 / git push 等
                D.Warning($"[EF.Packages] 本地包 [{packageInfo.Name}] 操作暂未实现: {actionLabel}");
            }
            EditorGUI.EndDisabledGroup();

            // 按钮3：卸载
            Color unloadColor = GUIUtils.LightRed;
            if (GUILayout.Button(LC.Combine(Lc.Unload), GUIUtils.Button(unloadColor), GUILayout.Width(100)))
                TryRemovePackage(packageInfo);
        }

        /// <summary>
        /// Git 包按钮：更新 + 卸载
        /// </summary>
        private void DrawGitButtons(EFPackageInfo packageInfo, Lc versionType)
        {
            bool canUpdate = versionType is Lc.Download or Lc.Update;

            // 按钮1：更新 / 已最新
            Color updateColor = canUpdate ? Color.green : Color.gray;
            string updateLabel = canUpdate
                ? LC.Combine(Lc.Update, Lc.Version)
                : LC.Combine(Lc.Latest);

            EditorGUI.BeginDisabledGroup(!canUpdate);
            if (GUILayout.Button(updateLabel, GUIUtils.Button(updateColor), GUILayout.Width(140)))
                TryAddOrUpdatePackage(packageInfo);
            EditorGUI.EndDisabledGroup();

            // 按钮2：卸载
            if (GUILayout.Button(LC.Combine(Lc.Unload), GUIUtils.Button(GUIUtils.LightRed), GUILayout.Width(100)))
                TryRemovePackage(packageInfo);
        }

        /// <summary>
        /// 未安装包按钮：下载
        /// </summary>
        private void DrawNotInstalledButtons(EFPackageInfo packageInfo)
        {
            if (GUILayout.Button(LC.Combine(Lc.Download, Lc.Install),
                    GUIUtils.Button(Color.green), GUILayout.Width(140)))
                TryAddOrUpdatePackage(packageInfo);
        }

        /// <summary>
        /// 未知来源：只显示卸载
        /// </summary>
        private void DrawUnknownButtons(EFPackageInfo packageInfo)
        {
            if (GUILayout.Button(LC.Combine(Lc.Unload), GUIUtils.Button(GUIUtils.LightRed), GUILayout.Width(100)))
                TryRemovePackage(packageInfo);
        }

        /// <summary>
        /// 尝试安装或更新包（调用 Unity PackageManager API）
        /// </summary>
        private void TryAddOrUpdatePackage(EFPackageInfo packageInfo)
        {
            if (_hasProcess)
            {
                D.Warning(LC.Combine(Lc.Current, Lc.Have, Lc.One, Lc.Task, Lc.PleaseWaitMoment, Lc.TryAgain));
                return;
            }

            CustomProgressWindow.ShowWindow(
                LC.Combine(new[] { Lc.Install, Lc.Package, Lc.Assets, Lc.PleaseWaitMoment }), null);

            string path = ServerToolkit.GetFrameworkPath(_config.serverType) + PackageFolderPath;
            _addRequest = Client.Add($"{path}{packageInfo.Name}");
            EditorApplication.update += AddPackageProgress;

            packageInfo.CurrentVersion = packageInfo.ServerVersion;
            packageInfo.NeedUpdate = false;
            _hasProcess = true;
        }

        /// <summary>
        /// 尝试卸载包（调用 Unity PackageManager API）
        /// </summary>
        private void TryRemovePackage(EFPackageInfo packageInfo)
        {
            if (_hasProcess)
            {
                D.Warning(LC.Combine(Lc.Current, Lc.Have, Lc.One, Lc.Task, Lc.PleaseWaitMoment, Lc.TryAgain));
                return;
            }

            _removeRequest = Client.Remove(packageInfo.Name);
            EditorApplication.update += RemovePackageProgress;

            packageInfo.CurrentVersion = "";
            packageInfo.NeedUpdate = true;
            _hasProcess = true;
        }

        /// <summary>
        /// 版本比较
        /// </summary>
        private Lc CompareVersion(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1))
                return Lc.Download;
            if (string.IsNullOrEmpty(v2))
                return Lc.Unload;

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

        /// <summary>
        /// 在文件管理器中打开包的本地目录
        /// </summary>
        private void OpenPackageDirectory(string packageName)
        {
            string packagePath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Packages", packageName));
            if (Directory.Exists(packagePath)) EditorUtility.RevealInFinder(packagePath);
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
            _config.packagesInfo.Clear();
            _packageFolder.Clear();

            foreach (var packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                var name = packageInfo.name;
                if (!name.Contains("cn.efefef."))
                    continue;

                string displayName = string.IsNullOrEmpty(packageInfo.displayName) ? name : packageInfo.displayName;
                
                // 检测包的真实来源
                EFPackageSource source = packageInfo.source switch
                {
                    PackageSource.Embedded => EFPackageSource.Local,
                    PackageSource.Local => EFPackageSource.Local,
                    PackageSource.Git => EFPackageSource.Git,
                    _ => EFPackageSource.Unknown
                };
                
                _config.packagesInfo.Add(new EFPackageInfo()
                {
                    Name = name,
                    DisplayName = displayName,
                    SourceType = source,
                    Description = packageInfo.description,
                    CurrentVersion = packageInfo.version,
                    ServerVersion = "",
                });
                _packageFolder.TryAdd(displayName, _allUnfold);
            }
        }

        /// <summary>
        /// 从远端拉取官方包目录，与本地包对比，保存差异结果
        /// </summary>
        private async UniTask UpdateAllFromGitOrGitee()
        {
            // 1. 从远端下载官方目录
            string remoteUrl = ServerToolkit.GetRawUrl(_config.serverType,
                ServerToolkit.GetFrameworkOwner(_config.serverType), "EFramework", "master",
                ServerToolkit.RemoteCatalogRepoPath);
            
            var catalogJson = await DownloadFileAsync(remoteUrl);

            EFPackageCatalog remoteCatalog;
            if (string.IsNullOrEmpty(catalogJson))
            {
                D.Warning("[EF.Packages] 远端目录下载失败，仅使用本地信息。请先通过 Generate & Save Catalog 生成目录并提交到 Git。");
                remoteCatalog = null;
            }
            else
            {
                try
                {
                    remoteCatalog = EFPackageCatalog.FromJson(catalogJson);
                }
                catch
                {
                    D.Error("[EF.Packages] 远端目录解析失败，格式异常。");
                    remoteCatalog = null;
                }
            }

            // 2. 将远端目录合并到本地配置
            MergeCatalogToConfig(remoteCatalog);

            // 3. 更新时间戳并保存
            _config.lastUpdateTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _config.RefreshSummary();
            ServerToolkit.SavePackageConfig(_config);
        }

        /// <summary>
        /// 将官方目录合并到本地配置
        /// 远端有的包 → 更新 ServerVersion
        /// 本地安装但远端没有 → 保留
        /// 远端有但本地未装 → 标记为 NotInstalled
        /// </summary>
        private void MergeCatalogToConfig(EFPackageCatalog catalog)
        {
            if (catalog == null || catalog.packages.Count == 0)
                return;
            // 遍历远端目录
            foreach (var remoteEntry in catalog.packages)
            {
                bool found = false;
                for (int i = 0; i < _config.packagesInfo.Count; i++)
                {
                    var local = _config.packagesInfo[i];
                    if (local.Name != remoteEntry.name)
                        continue;

                    // 更新 ServerVersion
                    local.ServerVersion = remoteEntry.version;
                    local.DisplayName = remoteEntry.displayName;

                    // 对比版本
                    if (!string.IsNullOrEmpty(local.CurrentVersion) && local.CurrentVersion != remoteEntry.version)
                    {
                        var compare = CompareVersion(local.CurrentVersion, remoteEntry.version);
                        local.NeedUpdate = compare == Lc.Update;
                    }
                    else if (string.IsNullOrEmpty(local.CurrentVersion))
                    {
                        local.NeedUpdate = true;
                    }
                    else
                    {
                        local.NeedUpdate = false;
                    }

                    _config.packagesInfo[i] = local;
                    found = true;
                    break;
                }

                // 远端有但本地未安装 → 添加为 NotInstalled
                if (found) continue;
                _config.packagesInfo.Add(new EFPackageInfo
                {
                    Name = remoteEntry.name,
                    DisplayName = remoteEntry.displayName,
                    Description = remoteEntry.description,
                    SourceType = EFPackageSource.NotInstalled,
                    ServerVersion = remoteEntry.version,
                    CurrentVersion = "",
                    NeedUpdate = true,
                });
                _packageFolder.TryAdd(remoteEntry.displayName, _allUnfold);
            }
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
                SourceType = EFPackageSource.Local,
                Description = config.Description,
                CurrentVersion = config.Version,
                ServerVersion = "",
            };

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