/*
 * ================================================
 * Describe:      GM 总控台 Editor 面板：元数据列表、参数输入、风险确认与执行历史。
 * Author:        Alvin8412
 * CreationTime:  2026-11-24
 * ModifyTime:    2026-11-24
 * Version:       0.3
 * ===============================================
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.GameMaster.Editor
{
    /// <summary>
    /// GM 总控台窗口：从运行时元数据 API 读取命令并展示，仅 Play 模式可用。
    /// <para>GM console window: reads commands from the runtime metadata API; Play mode only.</para>
    /// </summary>
    public sealed class GmWindow : EditorWindowBase
    {
        // 日志前缀
        private const string LogTag = "[ GM.Editor ] ";

        // 多语言键前缀
        private const string LcPrefix = "GameMaster.";

        // 折叠箭头符号（用非交互 Label 绘制，避免 Foldout 控件的 hover 高亮）
        private const string ArrowFolded = "▶";
        private const string ArrowExpanded = "▼";

        // 箭头列宽
        private const float ArrowWidth = 14f;

        // 风险徽章列宽
        private const float BadgeWidth = 96f;

        // 命令元数据快照（只存元数据，不存委托）
        private List<GmCommandInfo> _infos = new List<GmCommandInfo>();

        // 执行历史快照
        private List<GmResult> _history = new List<GmResult>();

        // 每个命令的参数输入：命令 ID -> (键 -> 字符串值)
        private readonly Dictionary<string, Dictionary<string, string>> _args =
            new Dictionary<string, Dictionary<string, string>>();

        // 展开状态：命令 ID -> 是否展开
        private readonly Dictionary<string, bool> _expanded = new Dictionary<string, bool>();

        // 分区折叠状态：分区名 -> 是否展开
        private readonly Dictionary<string, bool> _sectionExpanded = new Dictionary<string, bool>();

        // 滚动位置
        private Vector2 _scroll;

        // 是否显示历史
        private bool _showHistory;

        // 是否显示图例说明
        private bool _showLegend = true;

        // 过滤文本
        private string _filter = string.Empty;

        // 风险徽章样式（克隆避免每帧分配；正常/悬停/聚焦均着色）
        private GUIStyle _riskBadgeStyle;

        /// <summary>
        /// 打开 GM 窗口。
        /// <para>Opens the GM window.</para>
        /// </summary>
        [MenuItem(MenuItemToolkit.GameMaster + "Open Panel", false, MenuItemToolkit.GameMasterPriority + 1)]
        private static void Open()
        {
            var window = GetWindow<GmWindow>();
            if (window == null) return;
            window.titleContent = new GUIContent("Game Master");
            window.minSize = new Vector2(420f, 320f);
            window.Show();
        }

        #region 生命周期

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            // 退出 Play 时清空快照，防止跨会话持有旧对象
            ClearSnapshot();
            base.OnDisable();
        }

        /// <inheritdoc/>
        protected override void LoadWindowData()
        {
            Refresh();
        }

        /// <inheritdoc/>
        protected override void OnSmartGUI()
        {
            DrawToolbar();

            DrawLegend();

            if (!EditorApplication.isPlaying || !GameMasterManager.IsReady)
            {
                EditorGUILayout.HelpBox(Tr("PlayModeOnly"), MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawCommands();
            if (_showHistory)
                DrawHistory();
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region 绘制

        // 顶部工具栏
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(Tr("Refresh"), EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    Refresh();

                if (GUILayout.Button(Tr("CancelAll"), EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    GameMasterManager.CancelAll();

                _showHistory = GUILayout.Toggle(_showHistory, Tr("History"),
                    EditorStyles.toolbarButton, GUILayout.Width(55f));

                GUILayout.FlexibleSpace();
                GUILayout.Label(Format("TotalCount", _infos.Count),
                    EditorStyles.miniLabel, GUILayout.Width(70f));
            }

            _filter = EditorGUILayout.TextField(Tr("Filter"), _filter);
        }

        // 图例：说明面板每一块内容的含义（过滤框下方常驻）
        private void DrawLegend()
        {
            Rect header = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(header, GUIContent.none, GUIStyle.none))
                _showLegend = !_showLegend;

            GUI.Label(new Rect(header.x, header.y, ArrowWidth, header.height),
                _showLegend ? ArrowExpanded : ArrowFolded, EditorStyles.miniLabel);
            GUI.Label(new Rect(header.x + ArrowWidth, header.y, header.width - ArrowWidth, header.height),
                Tr("PanelGuide"), EditorStyles.boldLabel);
            if (!_showLegend) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                LegendRow("LegendToolbar");
                LegendRow("LegendFilter");
                LegendRow("LegendSection");
                LegendRow("LegendRisk");
                LegendRow("LegendCommand");
                LegendRow("LegendArgs");
                LegendRow("LegendHistory");
            }
        }

        // 图例单行：标题与描述都从词条读取
        private static void LegendRow(string keySuffix)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(Tr(keySuffix + "Title"), EditorStyles.miniBoldLabel, GUILayout.Width(78f));
                GUILayout.Label(Tr(keySuffix + "Desc"), EditorStyles.wordWrappedMiniLabel);
            }
        }

        // 命令列表：按分区折叠
        private void DrawCommands()
        {
            if (_infos.Count == 0)
            {
                EditorGUILayout.HelpBox(Tr("NoCommands"), MessageType.None);
                return;
            }

            // 先按 section 归组，保持首次出现顺序（不用 LINQ）
            var sections = new List<string>();
            var grouped = new Dictionary<string, List<GmCommandInfo>>();
            foreach (var info in _infos)
            {
                if (info == null || !MatchFilter(info)) continue;

                string key = info.Section;
                if (!grouped.TryGetValue(key, out var list))
                {
                    list = new List<GmCommandInfo>();
                    grouped[key] = list;
                    sections.Add(key);
                }

                list.Add(info);
            }

            if (sections.Count == 0)
            {
                EditorGUILayout.HelpBox(Tr("NoMatched"), MessageType.None);
                return;
            }

            foreach (var section in sections)
            {
                bool expanded = _sectionExpanded.GetValueOrDefault(section, true);
                Rect header = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

                // 透明按钮消费整行点击；全部视觉用非交互 Label 绘制，杜绝半亮高亮
                if (GUI.Button(header, GUIContent.none, GUIStyle.none))
                {
                    expanded = !expanded;
                    _sectionExpanded[section] = expanded;
                }

                GUI.Label(new Rect(header.x, header.y, ArrowWidth, header.height),
                    expanded ? ArrowExpanded : ArrowFolded, EditorStyles.miniLabel);
                GUI.Label(new Rect(header.x + ArrowWidth, header.y, header.width - ArrowWidth, header.height),
                    Format("SectionHeader", section, grouped[section].Count), EditorStyles.boldLabel);

                if (!expanded) continue;

                EditorGUI.indentLevel++;
                var list = grouped[section];
                foreach (var gmCommandInfo in list)
                    DrawCommand(gmCommandInfo);

                EditorGUI.indentLevel--;
            }
        }

        // 单条命令：顶部整行可点击折叠
        private void DrawCommand(GmCommandInfo info)
        {
            bool expanded = _expanded.TryGetValue(info.Id, out bool state) && state;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Rect header = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                DrawCommandHeader(header, info, ref expanded);

                if (!expanded) return;

                EditorGUILayout.LabelField("ID", info.Id, EditorStyles.miniLabel);

                DrawArgs(info);

                if (!GUILayout.Button(Tr("Run"), GUILayout.Width(90f))) return;
                if (IsDangerous(info.Risk))
                {
                    bool confirm = EditorUtility.DisplayDialog(
                        Tr("ConfirmTitle"), Tr("ConfirmMessage"), Tr("Run"), Tr("Cancel"));
                    if (!confirm) return;
                }

                // 参数字典在实例方法内构建，避免静态辅助反向 GetWindow
                var args = BuildArgs(info);
                RunAndRefresh(info.Id, args).Forget();
                RefreshHistory();
            }
        }

        // 绘制命令顶部整行：整行点击切换折叠，右侧风险徽章带颜色
        private void DrawCommandHeader(Rect rect, GmCommandInfo info, ref bool expanded)
        {
            // 唯一交互控件：透明按钮消费整行点击。
            // 其余箭头/名称/徽章全部用非交互 Label 绘制，避免控件 ID 错位导致的半亮高亮与双重切换
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                expanded = !expanded;
                _expanded[info.Id] = expanded;
            }

            GUI.Label(new Rect(rect.x, rect.y, ArrowWidth, rect.height),
                expanded ? ArrowExpanded : ArrowFolded, EditorStyles.miniLabel);

            Rect labelRect = new Rect(rect.x + ArrowWidth, rect.y,
                rect.width - ArrowWidth - BadgeWidth, rect.height);
            GUI.Label(labelRect, GetDisplayName(info), EditorStyles.boldLabel);

            Rect riskRect = new Rect(rect.xMax - BadgeWidth, rect.y, BadgeWidth, rect.height);
            DrawRiskBadge(riskRect, info.Risk);
        }

        // 风险徽章着色：用独立样式设置 textColor，而非 GUI.color（后者与样式基色相乘会压暗）
        private void DrawRiskBadge(Rect rect, GmRisk risk)
        {
            if (_riskBadgeStyle == null)
                _riskBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel);

            Color color = GetRiskColor(risk);
            _riskBadgeStyle.normal.textColor = color;
            _riskBadgeStyle.hover.textColor = color;
            _riskBadgeStyle.focused.textColor = color;
            GUI.Label(rect, risk.ToString(), _riskBadgeStyle);
        }

        // 风险等级 -> 颜色（light 主题下均取足够深的色值）
        private static Color GetRiskColor(GmRisk risk)
        {
            switch (risk)
            {
                case GmRisk.ReadOnly: return new Color(0.35f, 0.40f, 0.45f);
                case GmRisk.Normal: return new Color(0.10f, 0.50f, 0.20f);
                case GmRisk.Dangerous: return new Color(0.85f, 0.45f, 0.02f);
                case GmRisk.Destructive: return new Color(0.80f, 0.10f, 0.10f);
                default: return Color.gray;
            }
        }

        // 是否危险命令（需二次确认）
        private static bool IsDangerous(GmRisk risk) =>
            risk == GmRisk.Dangerous || risk == GmRisk.Destructive;

        // 参数输入区
        private void DrawArgs(GmCommandInfo info)
        {
            string[] keys = info.GetParamKeys();
            if (keys.Length == 0) return;

            EditorGUILayout.LabelField(Tr("Arguments"), EditorStyles.miniBoldLabel);
            Dictionary<string, string> values = GetArgs(info.Id);

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                values.TryGetValue(key, out string current);
                string next = EditorGUILayout.TextField(key, current ?? string.Empty);
                values[key] = next;
            }
        }

        // 执行历史
        private void DrawHistory()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(Tr("HistoryTitle"), EditorStyles.boldLabel);

            for (int i = _history.Count - 1; i >= 0; i--)
            {
                GmResult result = _history[i];
                if (result == null) continue;
                string mark = result.Success ? "✓" : "✗";
                EditorGUILayout.LabelField($"{mark} {result.Id}  {result.Message}", EditorStyles.miniLabel);
            }
        }

        #endregion

        #region 数据

        // 异步执行并在完成后刷新历史（UniTask<T> 无 Forget 重载，显式包装）
        private async UniTaskVoid RunAndRefresh(string id, Dictionary<string, object> args)
        {
            try
            {
                await GameMasterManager.Run(id, args);
            }
            catch (System.Exception e)
            {
                D.Error(LogTag + e.Message);
            }

            RefreshHistory();
            Repaint();
        }

        // 刷新元数据与历史（只取快照，不缓存委托）
        private void Refresh()
        {
            if (!EditorApplication.isPlaying || !GameMasterManager.IsReady)
            {
                ClearSnapshot();
                return;
            }

            _infos = GameMasterManager.GetInfos();
            RefreshHistory();
            Repaint();
        }

        // 刷新历史
        private void RefreshHistory()
        {
            _history = GameMasterManager.GetHistory();
        }

        // 清空所有快照
        private void ClearSnapshot()
        {
            _infos.Clear();
            _history.Clear();
            _args.Clear();
            _expanded.Clear();
            _sectionExpanded.Clear();
        }

        // Play 模式切换时主动失效缓存
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
                ClearSnapshot();
            else
                Refresh();
        }

        // 显示名：直接显示注册时传入的文本，不做多语言解析；为空回退 ID
        private static string GetDisplayName(GmCommandInfo info)
        {
            return string.IsNullOrEmpty(info.DisplayKey) ? info.Id : info.DisplayKey;
        }

        // 过滤匹配
        private bool MatchFilter(GmCommandInfo info)
        {
            if (string.IsNullOrEmpty(_filter)) return true;
            return info.Id.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   info.DisplayKey != null &&
                   info.DisplayKey.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   info.Section != null &&
                   info.Section.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // 获取命令的参数输入缓存
        private Dictionary<string, string> GetArgs(string id)
        {
            if (!_args.TryGetValue(id, out var values))
            {
                values = new Dictionary<string, string>();
                _args[id] = values;
            }

            return values;
        }

        // 构建参数字典：数值字符串尝试解析为 int/float
        private Dictionary<string, object> BuildArgs(GmCommandInfo info)
        {
            var result = new Dictionary<string, object>();
            string[] keys = info.GetParamKeys();
            if (!_args.TryGetValue(info.Id, out var values)) return result;

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (!values.TryGetValue(key, out string raw)) raw = string.Empty;

                if (int.TryParse(raw, out int intValue)) result[key] = intValue;
                else if (float.TryParse(raw, out float floatValue)) result[key] = floatValue;
                else result[key] = raw;
            }

            return result;
        }

        // 取面板文本（作用域键）
        private static string Tr(string suffix) => LC.Combine(LcPrefix + suffix);

        // 取面板格式化文本（作用域键 + 占位符参数）
        private static string Format(string suffix, params object[] args) =>
            LC.Format(LcPrefix + suffix, args);

        #endregion
    }
}