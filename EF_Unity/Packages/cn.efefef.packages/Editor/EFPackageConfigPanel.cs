/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-19 17:04:50
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-19 17:04:50
 * ScriptVersion: 0.2
 * ===============================================
 */

#region UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows;
using EasyFramework.Edit.Windows.ConfigPanel;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyFramework.Packages
{

    /// <summary>
    /// 包版本比较结果（原借 Lc 枚举充当状态机，语言键枚举删除后独立为专用状态类型）
    /// <para>Package version comparison result (previously abused the Lc enum as a state machine;
    /// now an independent state type after the language-key enum was removed).</para>
    /// </summary>
    internal enum PackageVersionState
    {
        // 服务端有、本地无（可下载）
        Download,
        // 无版本差 / 无可比版本
        Unload,
        // 服务端更新（本地可升级）
        Update,
        // 本地更新（可发布）
        Upload,
    }


    /// <summary>
    /// 管理项目相关包资产
    /// </summary>
    [EFConfigPanel(Priority = 10)]
    public class EFPackageConfigPanel : EFConfigPanelBase
    {
        private const string PackageFolderPath = ".git?path=/EF_Unity/Packages/";           // 远端Package文件夹地址

        // SemVer 三按钮：段索引与 ServerToolkit.TryBumpSemVer 一致（0=MAJOR,1=MINOR,2=PATCH）
        private static readonly int[] BumpSegments = { 0, 1, 2 };
        private static readonly string[] BumpLabels =
        {
            "Packages.BumpMajor", "Packages.BumpMinor", "Packages.BumpPatch"
        };
        private static readonly Color[] BumpColors = { Color.red, Color.yellow, Color.cyan };
        private const float BumpButtonWidth = 64f;

        // 新建包程序集依赖的固定 GUID（core Runtime / core Editor / UniTask；与 EF.RedDot 等现有包一致）
        private const string CoreRuntimeAssemblyGuid = "396fd9bc7a7442941a71f32479ef054d";
        private const string CoreEditorAssemblyGuid = "a4a7eea321f5a024bb7118f90940975f";
        private const string UniTaskAssemblyGuid = "f51ebe6a0ceec4240a699833d6309b23";

        public override string Name => "EF" + LC.Combine("Packages.PackageAssets");

        private bool _allUnfold;
        private bool _hasProcess;
        private string _token;
        private string[] _createPackageTips;

        // 是否为框架源码工程（维护者模式）：决定版本上升等维护按钮是否出现。
        // 缓存一次即可，避免每个包每帧重复做文件存在性检测。
        private bool _isMaintainer;

        // 官方目录（Editor Resources/EFPackageCache.json，提交到 Git）里记录的包版本，
        // 作为"恢复默认版本"的目标值来源；面板打开时刷新一次即可。
        private readonly Dictionary<string, string> _catalogVersions = new Dictionary<string, string>();

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
        }

        public override void OnDestroy()
        {
            if (null == _config) return;
            ServerToolkit.SavePackageConfig(_config);
        }

        public override void LoadWindowData()
        {
            _isMaintainer = ServerToolkit.IsFrameworkProject;
            RefreshCatalogVersions();

            if (null == _config)
            {
                _packageFolder = new Dictionary<string, bool>();

                _createPackageTips = new[]
                {
                    LC.Combine("Packages.PackageName") + "cn.efefef.{0}",
                    LC.Combine("Packages.PackageDescription"),
                    LC.Combine("Packages.AuthorName"),
                };

                _allUnfold = true;

                _config = ServerToolkit.GetPackageConfig() ?? new PackageConfig();

                // 旧配置可能缺 DisplayName（JsonUtility 反序列化为 null）或存在重名；
                // 用 TryAdd + Name 兜底，避免 Add 抛异常导致初始化中断、_packageFolder 永久不完整
                if (_config.packagesInfo != null)
                {
                    foreach (EFPackageInfo packageInfo in _config.packagesInfo)
                    {
                        string key = string.IsNullOrEmpty(packageInfo.DisplayName)
                            ? packageInfo.Name : packageInfo.DisplayName;
                        if (!string.IsNullOrEmpty(key))
                            _packageFolder.TryAdd(key, true);
                    }
                }
                _token = ServerToolkit.GetToken(_config.serverType);
            }

            RefreshLocalVersions();
        }

        /// <summary>
        /// 刷新官方目录里的包版本快照（"恢复默认版本"的目标值来源）
        /// <para>Refresh the catalog version snapshot used as the restore target.</para>
        /// </summary>
        private void RefreshCatalogVersions()
        {
            _catalogVersions.Clear();

            EFPackageCatalog catalog = ServerToolkit.LoadCatalog();
            if (null == catalog || null == catalog.packages)
                return;

            foreach (EFPackageCatalogEntry entry in catalog.packages)
            {
                if (null == entry || string.IsNullOrEmpty(entry.name))
                    continue;
                _catalogVersions[entry.name] = entry.version;
            }
        }

        /// <summary>
        /// 以磁盘 package.json 为准刷新各包的当前版本（缓存值可能在外部改动后过期）。
        /// 存在待发布版本时以暂存值为准，避免刷新把面板上的上升结果抹掉。
        /// <para>Refresh each package's current version from the package.json on disk.
        /// A pending (not yet published) version wins, so refreshing never discards a bump.</para>
        /// </summary>
        private void RefreshLocalVersions()
        {
            foreach (EFPackageInfo packageInfo in _config.packagesInfo)
            {
                if (!ServerToolkit.TryReadPackageJsonVersion(packageInfo.Name, out string diskVersion))
                    continue;

                packageInfo.CurrentVersion = string.IsNullOrEmpty(packageInfo.PendingVersion)
                    ? diskVersion
                    : packageInfo.PendingVersion;
            }
        }

        protected override void OnPanelGUI()
        {
            #region GitToggle

            _config.serverType = (ServerType)EditorGUILayout.EnumPopup(
                $"{LC.Combine("Use")} {_config.serverType} {LC.Combine("Update")}", _config.serverType);
            if (_config.serverType != ServerType.Local)
            {
                if (_serverType != _config.serverType)
                {
                    _serverType = _config.serverType;
                    _token = ServerToolkit.GetToken(_config.serverType);
                }
                EditorGUILayout.BeginHorizontal();
                _token = EditorGUILayout.TextField($"{_config.serverType} Token", _token);
                if (GUILayout.Button(LC.Combine("Record"), GUILayout.Width(80)))
                    ServerToolkit.SetToken(_config.serverType, _token);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(12.0f);

            #endregion

            #region Summary - 摘要

            _config.RefreshSummary();
            EditorGUILayout.BeginHorizontal(GUIUtils.BackgroundStyle());
            DrawSummaryItem(LC.Combine("Total"), _config.totalCount, Color.white);
            DrawSummaryItem(LC.Combine("Local"), _config.localCount, Color.cyan);
            DrawSummaryItem(LC.Combine("Git"), _config.gitCount, Color.yellow);
            DrawSummaryItem(LC.Combine("Packages.NotInstalled"), _config.notInstalledCount, GUIUtils.LightRed);
            if (_config.needUpdateCount > 0)
                DrawSummaryItem(LC.Combine("Packages.NeedUpdate"), _config.needUpdateCount, Color.gray);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(12.0f);

            #endregion

            #region Top Buttons - 顶部操作按钮

            bool isMaintainer = _isMaintainer;
            
            if (isMaintainer)
            {
                EditorGUILayout.LabelField(LC.Combine("Packages.MaintainOperating") + "（" + LC.Combine("Packages.ToGit") + "）", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(LC.Combine("Generate") + " & " + LC.Combine("Packages.SaveCatalog"), GUIUtils.Button(Color.cyan), GUILayout.Width(220)))
                {
                    var catalog = ServerToolkit.GenerateCatalogFromLocalPackages();
                    ServerToolkit.SaveCatalog(catalog);
                    GetLocalPackages();
                    MergeCatalogToConfig(catalog);
                    ServerToolkit.SavePackageConfig(_config);
                    // 新目录即新的"默认版本"基准，立即刷新，避免恢复按钮指向旧版本
                    RefreshCatalogVersions();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(8f);
            }

            EditorGUILayout.LabelField($"{LC.Combine("Packages.DevelopOperating")}（{LC.Combine("Packages.RemoteSync")}）", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine("Packages.UpdateAllInformation"), GUIUtils.Button(Color.green)))
            {
                GetLocalPackages();
                UpdateAllFromGitOrGitee().Forget();
            }

            string created = LC.Combine("Packages.CreateNewPackage");
            if (GUILayout.Button(created, GUIUtils.Button(Color.cyan)))
                CustomInputWindow.ShowWindow(created, _createPackageTips, CreatePackage);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(12.0f);

            #endregion

            #region Draw PackInfo

            if (GUILayout.Button(LC.Combine("Packages.AllContent") + " " + (_allUnfold ? LC.Combine("CollapseAll") : LC.Combine("ExpandAll"))))
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
            // 远端更新可能改名 DisplayName，而 _packageFolder 的键在初始化时按旧名建立；
            // 直接索引会 KeyNotFoundException 打断 GUI 绘制，用 TryGetValue 兜底缺键
            string folderKey = packageInfo.DisplayName;
            _packageFolder.TryGetValue(folderKey, out bool foldout);
            _packageFolder[folderKey] = EditorGUI.Foldout(
                foldRect, foldout, packageInfo.DisplayName, true, titleStyle);

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
                PackageVersionState versionType = CompareVersion(packageInfo.CurrentVersion, packageInfo.ServerVersion);

                EditorGUILayout.LabelField(packageInfo.Description, GUILayout.ExpandWidth(true));
                EditorGUILayout.Space();

                // 版本行
                EditorGUILayout.BeginHorizontal();
                bool hasPending = !string.IsNullOrEmpty(packageInfo.PendingVersion);
                string currentText = string.IsNullOrEmpty(packageInfo.CurrentVersion)
                    ? LC.Combine("Non")
                    : packageInfo.CurrentVersion;
                if (hasPending)
                    currentText += "*";     // * = 已上升但尚未写入 package.json

                string serverText = string.IsNullOrEmpty(packageInfo.ServerVersion)
                    ? LC.Combine("NotFound")
                    : packageInfo.ServerVersion;
                
                EditorGUILayout.LabelField(
                    new GUIContent($"{LC.Combine("Packages.CurrentVersion")}:  {currentText}",
                        hasPending ? LC.Combine("Packages.PendingVersionTip") : string.Empty),
                    GUILayout.Width(160));
                EditorGUILayout.LabelField(
                    $"{LC.Combine("Packages.ServerVersion")}:  {serverText}", 
                    GUILayout.Width(160));

                // 维护者专用：SemVer 三段上升（Git 包在 PackageCache 内只读，故只对本地嵌入式包开放）
                if (_isMaintainer && packageInfo.SourceType == EFPackageSource.Local)
                {
                    DrawBumpVersionButtons(packageInfo);
                    // 版本与默认值不一致时才出现的"恢复默认"按钮
                    DrawResetVersionButton(packageInfo);
                }
                
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
        /// SemVer 三段上升按钮：PATCH / MINOR / MAJOR，各自只改面板暂存版本，不写 package.json
        /// <para>SemVer bump buttons for PATCH / MINOR / MAJOR; each stages the version in the
        /// panel only and leaves package.json untouched.</para>
        /// </summary>
        /// <param name="packageInfo">目标包信息<para>Target package info.</para></param>
        private void DrawBumpVersionButtons(EFPackageInfo packageInfo)
        {
            // 内存版本为空时（如刚卸载过）回读磁盘，保证三个按钮基于同一基准
            string sourceVersion = packageInfo.CurrentVersion;
            if (string.IsNullOrEmpty(sourceVersion)
                && !ServerToolkit.TryReadPackageJsonVersion(packageInfo.Name, out sourceVersion))
                sourceVersion = "0.0.0";

            for (int segment = 0; segment < BumpSegments.Length; segment++)
            {
                bool canBump = ServerToolkit.TryBumpSemVer(sourceVersion, segment, out string nextVersion);
                string label = LC.Combine(BumpLabels[segment]);
                string tip = canBump
                    ? LC.Format("Packages.BumpSemVerTip", label, sourceVersion, nextVersion)
                    : LC.Combine("Packages.VersionUnparsable");

                EditorGUI.BeginDisabledGroup(!canBump);
                bool clicked = GUILayout.Button(new GUIContent(label, tip),
                    GUIUtils.Button(canBump ? BumpColors[segment] : Color.gray), GUILayout.Width(BumpButtonWidth));
                EditorGUI.EndDisabledGroup();
                if (!clicked)
                    continue;

                // 只改面板暂存值：真正写 package.json 放在“发布版本”时进行
                packageInfo.CurrentVersion = nextVersion;
                packageInfo.PendingVersion = nextVersion;
                ServerToolkit.SavePackageConfig(_config);
            }
        }

        /// <summary>
        /// 恢复默认版本按钮：仅在当前版本与默认版本不一致时出现，点击后把默认版本写回 package.json
        /// <para>Restore-default button: shown only when the current version differs from the default,
        /// and writes the default version back to package.json when clicked.</para>
        /// </summary>
        /// <param name="packageInfo">目标包信息<para>Target package info.</para></param>
        private void DrawResetVersionButton(EFPackageInfo packageInfo)
        {
            string defaultVersion = ResolveDefaultVersion(packageInfo);
            if (string.IsNullOrEmpty(defaultVersion))
                return;

            // 版本未变化时不显示该按钮（默认版本未知的情况已在上面提前返回）
            string currentVersion = packageInfo.CurrentVersion;
            if (string.Equals(currentVersion, defaultVersion, StringComparison.Ordinal))
                return;

            string tip = LC.Format("Packages.ResetVersionTip",
                string.IsNullOrEmpty(currentVersion) ? LC.Combine("Non") : currentVersion,
                defaultVersion);

            if (!GUILayout.Button(new GUIContent(LC.Combine("Packages.ResetVersion"), tip),
                    GUIUtils.Button(Color.yellow), GUILayout.Width(110)))
                return;

            ResetVersion(packageInfo, defaultVersion);
        }

        /// <summary>
        /// 把包的当前版本恢复为默认版本：丢弃待发布暂存值，必要时回写 package.json
        /// <para>Restore a package's current version to the default: drop the pending stage value
        /// and write package.json back when the disk differs.</para>
        /// </summary>
        /// <param name="packageInfo">目标包信息<para>Target package info.</para></param>
        /// <param name="defaultVersion">默认版本号<para>Default version.</para></param>
        private void ResetVersion(EFPackageInfo packageInfo, string defaultVersion)
        {
            // 磁盘已是默认版本时这里不会产生写入（TryWritePackageVersion 对同值直接成功）
            PackageVersionWriteResult result = ServerToolkit.TryWritePackageVersion(
                packageInfo.Name, defaultVersion, out _);

            if (result != PackageVersionWriteResult.Success)
            {
                D.Error(LC.Format("Packages.ResetVersionFailed", GetVersionWriteFailureReason(result)));
                return;
            }

            packageInfo.CurrentVersion = defaultVersion;
            packageInfo.PendingVersion = string.Empty;
            ServerToolkit.SavePackageConfig(_config);
        }

        /// <summary>
        /// 发布版本：把面板上暂存的版本号写入该包的 package.json
        /// <para>Publish the staged version into the package's package.json.</para>
        /// </summary>
        /// <param name="packageInfo">目标包信息<para>Target package info.</para></param>
        private void PublishVersion(EFPackageInfo packageInfo)
        {
            PackageVersionWriteResult result = ServerToolkit.TryWritePackageVersion(
                packageInfo.Name, packageInfo.CurrentVersion, out _);

            if (result != PackageVersionWriteResult.Success)
            {
                D.Error(LC.Format("Packages.PublishVersionFailed", GetVersionWriteFailureReason(result)));
                return;
            }

            // 暂存值已落到 manifest，清掉待发布标记，当前版本保持不变
            packageInfo.PendingVersion = string.Empty;
            ServerToolkit.SavePackageConfig(_config);
        }

        /// <summary>
        /// 解析该包的“默认版本”：优先取官方目录（提交到 Git 的 EFPackageCache.json），
        /// 目录未收录该包时回落到服务器版本
        /// <para>Resolve the package's default version: the official catalog first,
        /// then the server version when the catalog has no entry.</para>
        /// </summary>
        /// <param name="packageInfo">目标包信息<para>Target package info.</para></param>
        /// <returns>默认版本号；无从得知时返回空串<para>The default version, or an empty string when unknown.</para></returns>
        private string ResolveDefaultVersion(EFPackageInfo packageInfo)
        {
            if (_catalogVersions.TryGetValue(packageInfo.Name, out string catalogVersion)
                && !string.IsNullOrEmpty(catalogVersion))
                return catalogVersion;

            return packageInfo.ServerVersion;
        }

        /// <summary>
        /// 版本写盘失败的本地化原因（上升与恢复共用）
        /// <para>Localized failure reason for a version write (shared by bump and restore).</para>
        /// </summary>
        /// <param name="result">失败的写盘结果<para>Failed write result.</para></param>
        /// <returns>本地化原因文本<para>Localized reason text.</para></returns>
        private static string GetVersionWriteFailureReason(PackageVersionWriteResult result)
        {
            return result switch
            {
                PackageVersionWriteResult.PackageJsonNotFound => LC.Combine("Packages.VersionNoJson"),
                PackageVersionWriteResult.VersionUnparsable => LC.Combine("Packages.VersionUnparsable"),
                _ => LC.Combine("Packages.VersionWriteFailed"),
            };
        }

        /// <summary>
        /// 根据来源类型获取标签文字
        /// </summary>
        private string GetSourceLabel(EFPackageSource source)
        {
            return source switch
            {
                EFPackageSource.Local => LC.Combine("Local"),
                EFPackageSource.Git => LC.Combine("Git"),
                EFPackageSource.NotInstalled => LC.Combine("Packages.NotInstall"),
                _ => LC.Combine("Unknown"),
            };
        }

        /// <summary>
        /// 根据包来源和版本差，绘制对应操作按钮
        /// </summary>
        private void DrawButtons(EFPackageInfo packageInfo, PackageVersionState versionType)
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
        private void DrawLocalButtons(EFPackageInfo packageInfo, PackageVersionState versionType)
        {
            // 按钮1：打开本地目录
            if (GUILayout.Button(LC.Combine("Packages.OpenFolder"), GUILayout.Width(120)))
                OpenPackageDirectory(packageInfo.Name);

            // 按钮2：根据版本差显示不同动作
            string actionLabel = versionType switch
            {
                PackageVersionState.Update => LC.Combine("Packages.SyncUpdate"),      // 服务端更新 → 同步更新
                PackageVersionState.Upload => LC.Combine("Packages.UploadVersion"),   // 本地更新 → 发布版本
                _ => LC.Combine("Latest"),                                     // 一致 → 已最新
            };

            Color actionColor = versionType switch
            {
                PackageVersionState.Update => Color.green,
                PackageVersionState.Upload => Color.cyan,
                _ => Color.gray,
            };

            bool canAction = versionType != PackageVersionState.Unload;
            EditorGUI.BeginDisabledGroup(!canAction);
            if (GUILayout.Button(actionLabel, GUIUtils.Button(actionColor), GUILayout.Width(140)))
            {
                // 本地有新版本时，这一步才真正把版本号写进 package.json
                if (versionType == PackageVersionState.Upload)
                    PublishVersion(packageInfo);
                else
                    D.Warning($"[EF.Packages] 本地包 [{packageInfo.Name}] 操作暂未实现: {actionLabel}");
            }
            EditorGUI.EndDisabledGroup();

            // 按钮3：卸载
            Color unloadColor = GUIUtils.LightRed;
            if (GUILayout.Button(LC.Combine("Unload"), GUIUtils.Button(unloadColor), GUILayout.Width(100)))
                TryRemovePackage(packageInfo);
        }

        /// <summary>
        /// Git 包按钮：更新 + 卸载
        /// </summary>
        private void DrawGitButtons(EFPackageInfo packageInfo, PackageVersionState versionType)
        {
            bool canUpdate = versionType is PackageVersionState.Download or PackageVersionState.Update;

            // 按钮1：更新 / 已最新
            Color updateColor = canUpdate ? Color.green : Color.gray;
            string updateLabel = canUpdate
                ? LC.Combine("Packages.UpdateVersion")
                : LC.Combine("Latest");

            EditorGUI.BeginDisabledGroup(!canUpdate);
            if (GUILayout.Button(updateLabel, GUIUtils.Button(updateColor), GUILayout.Width(140)))
                TryAddOrUpdatePackage(packageInfo);
            EditorGUI.EndDisabledGroup();

            // 按钮2：卸载
            if (GUILayout.Button(LC.Combine("Unload"), GUIUtils.Button(GUIUtils.LightRed), GUILayout.Width(100)))
                TryRemovePackage(packageInfo);
        }

        /// <summary>
        /// 未安装包按钮：下载
        /// </summary>
        private void DrawNotInstalledButtons(EFPackageInfo packageInfo)
        {
            if (GUILayout.Button(LC.Combine("Packages.DownloadInstall"),
                    GUIUtils.Button(Color.green), GUILayout.Width(140)))
                TryAddOrUpdatePackage(packageInfo);
        }

        /// <summary>
        /// 未知来源：只显示卸载
        /// </summary>
        private void DrawUnknownButtons(EFPackageInfo packageInfo)
        {
            if (GUILayout.Button(LC.Combine("Unload"), GUIUtils.Button(GUIUtils.LightRed), GUILayout.Width(100)))
                TryRemovePackage(packageInfo);
        }

        /// <summary>
        /// 尝试安装或更新包（调用 Unity PackageManager API）
        /// </summary>
        private void TryAddOrUpdatePackage(EFPackageInfo packageInfo)
        {
            if (_hasProcess)
            {
                D.Warning(LC.Combine("Packages.TaskInProgressRetry"));
                return;
            }

            CustomProgressWindow.ShowWindow(
                LC.Combine("Packages.InstallingPackageAssets"), null);

            string path = ServerToolkit.GetFrameworkPath(_config.serverType) + PackageFolderPath;
            _addRequest = Client.Add($"{path}{packageInfo.Name}");
            EditorApplication.update += AddPackageProgress;

            packageInfo.CurrentVersion = packageInfo.ServerVersion;
            packageInfo.PendingVersion = string.Empty;      // 安装/更新会替换 manifest，暂存值随之失效
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
                D.Warning(LC.Combine("Packages.TaskInProgressRetry"));
                return;
            }

            _removeRequest = Client.Remove(packageInfo.Name);
            EditorApplication.update += RemovePackageProgress;

            packageInfo.CurrentVersion = "";
            packageInfo.PendingVersion = string.Empty;      // 卸载后包目录消失，暂存值随之失效
            packageInfo.NeedUpdate = true;
            _hasProcess = true;
        }

        /// <summary>
        /// 版本比较（委托 EditorUtils.CompareVersionState：空串=Download/Unload，1.0.1 vs 1.0.0=Upload/Update）
        /// </summary>
        private PackageVersionState CompareVersion(string v1, string v2)
        {
            int result = EditorUtils.CompareVersionState(v1, v2);
            if (result == -2)
                return PackageVersionState.Download;
            if (result == 2)
                return PackageVersionState.Unload;
            if (result > 0)
                return PackageVersionState.Upload;
            if (result < 0)
                return PackageVersionState.Update;
            return PackageVersionState.Unload;
        }

        /// <summary>
        /// 在文件管理器中打开包的本地目录
        /// </summary>
        private void OpenPackageDirectory(string packageName)
        {
            string packagePath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Packages", packageName));
            EditorCommands.OpenFolder(packagePath);
        }

        #region Client Task - 客户端任务

        // 增加一个Package包的相关进程
        private void AddPackageProgress()
        {
            if (_addRequest is not { IsCompleted: true })
                return;

            if (_addRequest.Status != StatusCode.Success)
                D.Error($"{LC.Combine("Packages.InstallError")}: {_addRequest.Error.message}");

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
                D.Error($"{LC.Combine("Packages.UnloadError")}{_removeRequest.Error.message}");

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
                
                // 本地嵌入式包以磁盘 package.json 为准：刚用"版本上升"写过盘时 Unity 缓存仍是旧值
                string currentVersion = ServerToolkit.TryReadPackageJsonVersion(name, out string diskVersion)
                    ? diskVersion
                    : packageInfo.version;

                _config.packagesInfo.Add(new EFPackageInfo()
                {
                    Name = name,
                    DisplayName = displayName,
                    SourceType = source,
                    Description = packageInfo.description,
                    CurrentVersion = currentVersion,
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
            // 本地模式没有远端目录，GetRawUrl(Remote/...) 的 switch 只覆盖 GitHub/Gitee，
            // 传 Local 会抛 NotSupportedException（且在 .Forget() 中被吞成日志）
            if (_config.serverType == ServerType.Local)
            {
                D.Warning("[EFPackage] Local 模式不使用远端目录，跳过更新全部信息。");
                return;
            }

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
            if (catalog == null || catalog.packages == null || catalog.packages.Count == 0)
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
                        local.NeedUpdate = compare == PackageVersionState.Update;
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
            // Token 来自面板输入框 _token（写入 EditorPrefs），_config.token 从未被赋值，
            // 用它会让私有仓库下载始终缺 Authorization 头而 401/404
            if (!string.IsNullOrEmpty(_token))
                request.SetRequestHeader("Authorization", $"Bearer {_token}");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) 
                return request.downloadHandler.text;
            
            D.Error(LC.Combine("Packages.UpdateError"));
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

            bool success = false;
            try
            {
                CreatePackageJson(packageName, packageDes, author, packagePath);
                CreatePackageStructure(packageName, packagePath);
                success = true;
            }
            catch (Exception e)
            {
                D.Warning(e);
            }
            finally
            {
                // 仅在创建成功时提交配置：失败路径下 _newPackageInfo 可能为 null，
                // 强行 Add(null) 会把无效包写进配置并污染后续 Resolve。
                if (success && _newPackageInfo != null)
                {
                    _config.packagesInfo.Add(_newPackageInfo);
                    _packageFolder.TryAdd(_newPackageInfo.DisplayName, true);
                    ServerToolkit.SavePackageConfig(_config);

                    Client.Resolve();
                    EditorCommands.SaveAssets();
                    EditorCommands.Refresh();
                }
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

        /// <summary>
        /// 为新包创建独立的运行时/编辑器程序集与多语言目录（自有 asmdef，非 asmref 挂靠 core）
        /// <para>Create standalone runtime/editor assemblies and a localization folder for the new
        /// package (own asmdefs, not asmrefs attached to core).</para>
        /// </summary>
        /// <param name="packageName">包名（首字母大写的短名，如 UI）<para>Package short name.</para></param>
        /// <param name="rootPath">包根目录绝对路径<para>Absolute path of the package root.</para></param>
        private void CreatePackageStructure(string packageName, string rootPath)
        {
            string packageEditorPath = Path.Combine(rootPath, "Editor");
            string packageRuntimePath = Path.Combine(rootPath, "Runtime");
            string langPath = Path.Combine(rootPath, "Editor Resources", "Description");
            Directory.CreateDirectory(packageEditorPath);
            Directory.CreateDirectory(packageRuntimePath);
            Directory.CreateDirectory(langPath);

            // 自身 Runtime 程序集 GUID：Editor asmdef 引用它（每个新包唯一）
            string runtimeGuid = Guid.NewGuid().ToString("N");

            // 1) Runtime asmdef：引用 core Runtime + UniTask（与 EF.RedDot 等现有包一致）
            string runtimeAsmdefName = $"EF.{packageName}";
            EditorUtils.WriteTextWithMeta(Path.Combine(packageRuntimePath, runtimeAsmdefName + ".asmdef"),
                BuildAssemblyDefinition(runtimeAsmdefName, new[]
                {
                    UniTaskAssemblyGuid,
                    CoreRuntimeAssemblyGuid,
                }, false), runtimeGuid);

            // 2) Editor asmdef：Editor 平台，引用 core Editor + 自身 Runtime + core Runtime
            string editorAsmdefName = $"EF.{packageName}.Editor";
            EditorUtils.WriteTextWithMeta(Path.Combine(packageEditorPath, editorAsmdefName + ".asmdef"),
                BuildAssemblyDefinition(editorAsmdefName, new[]
                {
                    CoreEditorAssemblyGuid,
                    runtimeGuid,
                    CoreRuntimeAssemblyGuid,
                }, true));

            // 3) 多语言：Editor Resources/Description/Editorlanguages.json（空键清单）
            EditorUtils.WriteTextWithMeta(Path.Combine(langPath, "Editorlanguages.json"),
                "{\n  \"LcList\": []\n}");
        }

        /// <summary>
        /// 构建 asmdef JSON 内容（格式与现有包一致：4 空格缩进）
        /// <para>Build asmdef JSON content (4-space indent, consistent with existing packages).</para>
        /// </summary>
        /// <param name="name">程序集名<para>Assembly name.</para></param>
        /// <param name="referenceGuids">依赖的 GUID 列表<para>Referenced assembly GUIDs.</para></param>
        /// <param name="editorOnly">是否仅 Editor 平台<para>Whether it is Editor-only.</para></param>
        /// <returns>asmdef JSON 文本<para>The asmdef JSON text.</para></returns>
        private static string BuildAssemblyDefinition(string name, string[] referenceGuids, bool editorOnly)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine($"    \"name\": \"{name}\",");
            builder.AppendLine("    \"rootNamespace\": \"\",");
            builder.AppendLine("    \"references\": [");
            for (int i = 0; i < referenceGuids.Length; i++)
            {
                string comma = i < referenceGuids.Length - 1 ? "," : string.Empty;
                builder.AppendLine($"        \"GUID:{referenceGuids[i]}\"{comma}");
            }
            builder.AppendLine("    ],");
            if (editorOnly)
                builder.AppendLine("    \"includePlatforms\": [\n        \"Editor\"\n    ],");
            else
                builder.AppendLine("    \"includePlatforms\": [],");
            builder.AppendLine("    \"excludePlatforms\": [],");
            builder.AppendLine("    \"allowUnsafeCode\": false,");
            builder.AppendLine("    \"overrideReferences\": false,");
            builder.AppendLine("    \"precompiledReferences\": [],");
            builder.AppendLine("    \"autoReferenced\": true,");
            builder.AppendLine("    \"defineConstraints\": [],");
            builder.AppendLine("    \"versionDefines\": [],");
            builder.AppendLine("    \"noEngineReferences\": false");
            builder.AppendLine("}");
            return builder.ToString();
        }

        #endregion
    }
}

#endregion