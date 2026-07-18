/*
 * ================================================
 * Describe:        Excel表配置面板, 简化配表和生成数据等繁琐内容
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 17:51:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 17:51:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System.Collections.Generic;
using System.IO;
using System.Text;
using EasyFramework.Edit.Windows.ConfigPanel;
using UnityEngine;
using UnityEditor;

namespace EasyFramework.Edit.Windows.Dataable
{
    /// <summary>
    /// Excel表配置面板，方便用户设置内容
    /// </summary>
    [EFConfigPanel]
    public class DatableConfigPanel : EFConfigPanelBase
    {
        // Excel表目录
        private static string ExcelFolderPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "EFTools", "Datable"));

        // 包内 Luban 工具根目录
        private static string ToolSourceDir =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "cn.efefef.datable", "LubanTools~"));

        private static DatableConfig _config;

        private string _excelScanPath; // 表格缓存路径，不同时自动刷新
        private List<string> _excelFiles; // 正在显示的表格列表
        private Vector2 _excelScroll;

        public override string Name => LC.Combine(Lc.Excel, Lc.Data, Lc.Config);

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }

        public override void OnDestroy()
        {
            _excelFiles?.Clear();
            _excelFiles = null;
            _config = null;
        }

        public override void LoadWindowData()
        {
            if (_config is not null || !EditorUtils.CheckAssets<DatableConfig>(out string path))
                return;

            _config = EditorUtils.LoadSettingAtPath<DatableConfig>(path);
            InitConfig();
        }

        public override void OnGUI()
        {
            if (!_config)
            {
                EditorGUILayout.Space(12f);
                EditorGUILayout.HelpBox(LC.Combine(Lc.Config, Lc.Not, Lc.Exist), MessageType.Warning);
                EditorGUILayout.Space(12f);
                if (!GUILayout.Button(LC.Combine(Lc.Create, Lc.Config))) return;
                _config = Create.CreateSettings.Instance<DatableConfig>(true, ConfigManager.ConfigEditPath);
                InitConfig();
                ResetUserConfig(false);
                RefreshExcelList();
                return;
            }

            DrawPathSelect();
            EditorGUILayout.Space(24f);
            DrawConfigSelect();
            EditorGUILayout.Space(12f);
            DrawExcelList();
            EditorGUILayout.Space(12f);
        }

        // 绘制路径选择
        private void DrawPathSelect()
        {
            int checkCount = 0;
            float width = 111.5f;
            EditorGUILayout.LabelField(LC.Combine(Lc.Excel, Lc.Path), GUIUtils.ColorText(textColor: Color.white));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_config.LubanSourcePath);
            if (GUILayout.Button(LC.Combine(Lc.Copy), GUILayout.Width(width)))
            {
                if (!string.IsNullOrEmpty(_config.LubanSourcePath))
                    EditorGUIUtility.systemCopyBuffer = _config.LubanSourcePath;
            }
            EditorGuiToolkit.OpenFolderButton(_config.LubanSourcePath, width);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            if (EditorGuiToolkit.SelectionFolderPathInAssets(LC.Combine(Lc.Data, Lc.Generate, Lc.Path),
                    ref _config.LubanDataPath))
                checkCount++;
            EditorGUILayout.Space();
            if (EditorGuiToolkit.SelectionFolderPathInAssets(LC.Combine(Lc.Code, Lc.Generate, Lc.Path),
                    ref _config.LubanCodePath))
                checkCount++;

            if (checkCount == 0) return;
            EditorUtility.SetDirty(_config);
            EditorCommands.SaveAssets();
            EditorCommands.Refresh();
        }

        // 绘制配置选择
        private void DrawConfigSelect()
        {
            if (GUILayout.Button(LC.Combine(Lc.Reset, Lc.To, Lc.Default, Lc.Config)))
                ResetUserConfig(true);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            var newDataType = (DataType)EditorGUILayout.EnumPopup(LC.Combine(Lc.Data, Lc.Type), _config.LubanDataType);
            if (EditorGUI.EndChangeCheck())
            {
                _config.LubanDataType = newDataType;
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            var newTargetType = (DataTargetType)EditorGUILayout.EnumPopup(LC.Combine(Lc.Generate, Lc.Platform), _config.LubanDataTargetType);
            if (EditorGUI.EndChangeCheck())
            {
                _config.LubanDataTargetType = newTargetType;
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            _config.LubanNamespace = EditorGUILayout.TextField(LC.Combine(Lc.Generate, Lc.Of, Lc.Namespace),
                _config.LubanNamespace);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (_excelFiles.Count != 0 && GUILayout.Button(LC.Combine(Lc.Generate, Lc.All, Lc.Excel, Lc.Data),
                    GUIUtils.Button(color: Color.green)))
                StartDatable();
            if (GUILayout.Button(LC.Combine(Lc.Refresh, Lc.Excel, Lc.List)))
                RefreshExcelList();
            EditorGUILayout.EndHorizontal();
        }

        // 绘制电子表格列表
        private void DrawExcelList()
        {
            if (_config == null) return;
            string root = _config.LubanSourcePath;
            if (string.IsNullOrEmpty(root))
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Please, Lc.Settings, Lc.Excel, Lc.Path), MessageType.Info);
                return;
            }

            if (!Directory.Exists(root))
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Path, Lc.Not, Lc.Exist) + $"：{root}", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(LC.Combine(Lc.Excel, Lc.List, Lc.Count) + $"（{_excelFiles.Count}）",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (_excelFiles.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    LC.Combine(Lc.In, Lc.This, Lc.Folder, Lc.Under, Lc.No, Lc.Found, Lc.Any, Lc.Excel),
                    MessageType.Info);
                return;
            }

            _excelScroll = EditorGUILayout.BeginScrollView(_excelScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _excelFiles.Count; i++)
            {
                DrawSingleExcel(i, _excelFiles[i]);
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        // 绘制单一表格
        private void DrawSingleExcel(int index, string path)
        {
            EditorGUILayout.BeginHorizontal(index % 2 == 0 ? EditorStyles.helpBox : GUI.skin.box);
            EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(path),
                GUIUtils.ColorText(14, textColor: Color.white), GUILayout.ExpandWidth(true));
            if (GUILayout.Button(LC.Combine(Lc.Open), GUILayout.Width(80)))
            {
                if (File.Exists(path))
                    EditorUtility.OpenWithDefaultApp(path);
                else
                    D.Warning($"[ DatableConfigPanel ] 文件不存在，无法打开：{path}");
            }

            EditorGUILayout.EndHorizontal();
        }

        // 初始化配置
        private void InitConfig()
        {
            bool dirty = false;
            string fixedSource = Path.Combine(ExcelFolderPath, "Datas\\");
            if (_config.LubanSourcePath != fixedSource)
            {
                _config.LubanSourcePath = fixedSource;
                dirty = true;
            }

            string fixedCode = NormalizeToAssetsRelative(_config.LubanCodePath);
            if (_config.LubanCodePath != fixedCode)
            {
                _config.LubanCodePath = fixedCode;
                dirty = true;
            }
            string fixedData = NormalizeToAssetsRelative(_config.LubanDataPath);
            if (_config.LubanDataPath != fixedData)
            {
                _config.LubanDataPath = fixedData;
                dirty = true;
            }

            if (dirty)
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }

            if (!Directory.Exists(fixedSource))
                Directory.CreateDirectory(fixedSource);

            EnsureUserConfig();

            _excelFiles = new List<string>();
            RefreshExcelList();
        }

        // 刷新Excel列表
        private void RefreshExcelList()
        {
            _excelFiles.Clear();
            string root = _config.LubanSourcePath;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return;
            foreach (string path in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (!path.EndsWith(".xls") && !path.EndsWith(".xlsx")) continue;
                if (Path.GetFileName(path).StartsWith("__")) continue;
                _excelFiles.Add(path);
            }
        }

        // 确保用户可编辑配置副本存在：luban.conf 直接创建；Datas/Defines 两文件夹照旧从包内复制
        private static void EnsureUserConfig()
        {
            CopyDefaultDataFolders();
            string confPath = Path.Combine(ExcelFolderPath, "luban.conf");
            if (!File.Exists(confPath))
                CreateConfig();
        }

        // 当本地不存在时从包内默认 DataTables 中复制 Datas 与 Defines 两文件夹。
        private static void CopyDefaultDataFolders()
        {
            string src = Path.Combine(ToolSourceDir, "DataTables");
            if (!Directory.Exists(src))
            {
                D.Warning($"[ DatableConfigPanel ] 包内默认配置缺失：{src}");
                return;
            }

            foreach (string name in new[] { "Datas", "Defines" })
            {
                string srcEntry = Path.Combine(src, name);
                string dstEntry = Path.Combine(ExcelFolderPath, name);
                if (Directory.Exists(srcEntry) && !Directory.Exists(dstEntry))
                    FileUtil.CopyFileOrDirectory(srcEntry, dstEntry);
            }
        }

        // 直接在 EFTools/Datable 下创建 luban.conf
        private static void CreateConfig()
        {
            string dstDir = ExcelFolderPath;
            Directory.CreateDirectory(dstDir);

            string sourceRoot = "Datas/";
            if (_config != null && !string.IsNullOrEmpty(_config.LubanSourcePath) &&
                Directory.Exists(_config.LubanSourcePath))
                sourceRoot = Path.GetRelativePath(dstDir, _config.LubanSourcePath).Replace('\\', '/').TrimEnd('/') + "/";

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("\t\"groups\":");
            sb.AppendLine("\t[");
            sb.AppendLine("\t\t{\"names\":[\"c\"], \"default\":true},");
            sb.AppendLine("\t\t{\"names\":[\"s\"], \"default\":true},");
            sb.AppendLine("\t\t{\"names\":[\"e\"], \"default\":true}");
            sb.AppendLine("\t],");
            sb.AppendLine("\t\"schemaFiles\":");
            sb.AppendLine("\t[");
            sb.AppendLine("\t\t{\"fileName\":\"Defines\", \"type\":\"\"},");
            sb.AppendLine($"\t\t{{\"fileName\":\"{sourceRoot}__tables__.xlsx\", \"type\":\"table\"}},");
            sb.AppendLine($"\t\t{{\"fileName\":\"{sourceRoot}__beans__.xlsx\", \"type\":\"bean\"}},");
            sb.AppendLine($"\t\t{{\"fileName\":\"{sourceRoot}__enums__.xlsx\", \"type\":\"enum\"}}");
            sb.AppendLine("\t],");
            sb.AppendLine("\t\"dataDir\": \"Datas\",");
            sb.AppendLine("\t\"targets\":");
            sb.AppendLine("\t[");
            sb.AppendLine(
                $"\t\t{{\"name\":\"server\", \"manager\":\"LC\", \"groups\":[\"s\"], \"topModule\":\"{_config.LubanNamespace}\"}},");
            sb.AppendLine(
                $"\t\t{{\"name\":\"client\", \"manager\":\"LC\", \"groups\":[\"c\"], \"topModule\":\"{_config.LubanNamespace}\"}},");
            sb.AppendLine(
                $"\t\t{{\"name\":\"all\", \"manager\":\"LC\", \"groups\":[\"c\",\"s\",\"e\"], \"topModule\":\"{_config.LubanNamespace}\"}}");
            sb.AppendLine("\t],");
            sb.AppendLine("\t\"xargs\":");
            sb.AppendLine("\t[");
            sb.AppendLine("\t]");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(dstDir, "luban.conf"), sb.ToString());
            EditorCommands.Refresh();
        }

        // 重置为默认配置：luban.conf 直接重建，Datas/Defines 两文件夹照旧从包内复制
        private void ResetUserConfig(bool showDialog)
        {
            if (showDialog && !EditorUtility.DisplayDialog(LC.Combine(Lc.Reset, Lc.Config), 
                    "将用默认配置覆盖你的本地修改，确定？\n" +
                    "This will overwrite your local modifications with the default settings. Are you sure?",
                    LC.Combine(Lc.Confirm), LC.Combine(Lc.Cancel)))
                return;
            string dst = ExcelFolderPath;
            // 清掉 luban.conf 与 Datas/Defines（保留 Datable 目录本身及同级其它内容）
            foreach (string name in new[] { "luban.conf", "Datas", "Defines" })
            {
                string p = Path.Combine(dst, name);
                if (File.Exists(p) || Directory.Exists(p))
                    FileUtil.DeleteFileOrDirectory(p);
            }

            CopyDefaultDataFolders();
            CreateConfig();
        }

        // 获取目标生成端
        private static string ToTarget(DataTargetType type) => type switch
        {
            DataTargetType.Client => "client",
            DataTargetType.Server => "server",
            DataTargetType.Both => "all",
            _ => "all"
        };

        // 获取目标数据类型
        private static string ToDataType(DataType type) => type switch
        {
            DataType.Json => "json",
            DataType.Bytes => "bin",
            _ => "bin"
        };

        // 清空 Luban 数据输出目录
        private static void ClearOutputDirectory(string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta")) continue;
                File.Delete(file);
            }
            foreach (string meta in Directory.GetFiles(dir, "*.meta", SearchOption.AllDirectories))
            {
                string sibling = meta.Substring(0, meta.Length - 5);
                if (!File.Exists(sibling) && !Directory.Exists(sibling))
                    File.Delete(meta);
            }
        }

        // 解析输出目录绝对路径直接使用，相对路径与项目根目录组合，避免重复拼接导致路径错误。
        private static string ResolveOutputDir(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (Path.IsPathRooted(path)) return path;
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, path);
        }

        // 将输出目录归一化为 Assets/... 相对形式，保证工程可移植：
        private static string NormalizeToAssetsRelative(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            string normalized = path.Replace('\\', '/').Trim();
            if (normalized.Equals("Assets", System.StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                return normalized.EndsWith("/") ? normalized : normalized + "/";

            string dataPath = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            if (normalized.StartsWith(dataPath, System.StringComparison.OrdinalIgnoreCase))
            {
                string rel = "Assets" + normalized[dataPath.Length..].TrimStart('/');
                return rel.EndsWith("/") ? rel : rel + "/";
            }
            return normalized.EndsWith("/") ? normalized : normalized + "/";
        }

        [MenuItem(MenuItemToolkit.Utility + "📊 Start Datable (Luban)", false, MenuItemToolkit.UtilityPriority + 2)]
        private static void StartDatable()
        {
            if (_config == null)
            {
                D.Warning("[ DatableConfigPanel ] 配置未加载，无法生成。");
                return;
            }

            string sourcePath = _config.LubanSourcePath;
            if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
            {
                D.Warning($"[ DatableConfigPanel ] Excel 源路径无效或未设置：{sourcePath}");
                return;
            }

            string toolDir = ToolSourceDir;
            string userConf = Path.Combine(ExcelFolderPath, "luban.conf");
            if (!File.Exists(userConf))
            {
                D.Warning($"[ DatableConfigPanel ] 未找到用户配置副本：{userConf}，请重新打开面板以自动创建默认配置。");
                return;
            }

            string lubanDll = Path.Combine(toolDir, "Luban", "Luban.dll");
            string dataDir = ResolveOutputDir(_config.LubanDataPath);
            string codeDir = ResolveOutputDir(_config.LubanCodePath);
            string[] args =
            {
                lubanDll,
                "-t", ToTarget(_config.LubanDataTargetType),
                "-c", "cs-bin",
                "-d", ToDataType(_config.LubanDataType),
                "--conf", userConf,
                "-x", $"outputCodeDir={codeDir}",
                "-x", $"outputDataDir={dataDir}",
                "-x", "lineEnding=CRLF"
            };

            ClearOutputDirectory(dataDir);

            var result = ProcessToolkit.RunCaptured("dotnet", toolDir, args);
            if (result.ExitCode != 0)
            {
                D.Error($"[ DatableConfigPanel ] Luban 执行失败，退出码 {result.ExitCode}。\n{result.Error}\n{result.Output}");
                return;
            }

            EditorCommands.SaveAssets();
            EditorCommands.Refresh();
        }
    }
}