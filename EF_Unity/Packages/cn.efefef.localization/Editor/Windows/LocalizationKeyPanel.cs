/*
 * ================================================
 * Describe:        多语言设置面板（Key 列表 / 分组与种类 两个互斥子页）
 * Author:          Alvin5100
 * CreationTime:    2026-10-10 15:26:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-10 16:10:00
 * ScriptVersion:   0.3
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// 多语言设置面板
    /// <para>双向同步：外部改 JSON 后点「重新读取」读进来；面板内改动点「保存」覆写。</para>
    /// </summary>
    [EFConfigPanel(Priority = 190)]
    internal class LocalizationKeyPanel : EFConfigPanelBase
    {
        /// <summary> 清单文件所在目录（相对包根） </summary>
        private const string DataDir = "Description";

        /// <summary> 清单文件名 </summary>
        private const string FileName = "LocalizationKeys.json";

        /// <summary> 默认分组名 </summary>
        private const string DefaultGroup = "Default";

        /// <summary> 子页：Key 列表 </summary>
        private enum SubTab
{
            /// <summary> Key 列表 </summary>
            [InspectorName("KeyList")] Keys = 0,

            /// <summary> 分组与种类 </summary>
            [InspectorName("GroupType")] Groups = 1,
        }

        #region 列宽
        /// <summary> name 列权重 </summary>
        private const float WeightName = 3f;

        /// <summary> des 列权重 </summary>
        private const float WeightDes = 2f;

        /// <summary> group 列权重 </summary>
        private const float WeightGroup = 2f;

        /// <summary> note 列权重 </summary>
        private const float WeightNote = 3f;

        /// <summary> 分组页 name 列权重 </summary>
        private const float WeightGroupName = 4f;

        /// <summary> 分组页 count 列权重 </summary>
        private const float WeightGroupCount = 1.5f;

        /// <summary> 窄列固定宽度（不参与比例分配） </summary>
        private const float COL_KIND = 90f;

        private const float COL_BTN = 26f;

        /// <summary> 行尾留白列宽：避免删除按钮紧贴右边界 </summary>
        private const float COL_TAIL_PAD = 12f;

        private const float COL_GROUP_ACTIONS = 126f;

        /// <summary>
        /// 按权重把可用宽度分配给各列
        /// </summary>
        /// <param name="available">可用宽度</param>
        /// <param name="fixedWidth">已扣除的固定宽度合计</param>
        /// <param name="weights">各列权重</param>
        /// <returns>各列实际宽度，顺序与 weights 一致</returns>
        private static float[] DistributeWidth(float available, float fixedWidth, params float[] weights)
        {
            var result = new float[weights.Length];

            float total = 0f;
            foreach (var w in weights) total += w;
            if (total <= 0f) return result;

            // 空间不足时按比例压缩固定列，保证所有列合计不超过可用宽度
            float usable = available;
            if (fixedWidth > usable)
            {
                float shrink = usable / fixedWidth;
                fixedWidth *= shrink;
            }

            float flexible = Mathf.Max(1f, usable - fixedWidth);

            float used = 0f;
            for (var i = 0; i < weights.Length; i++)
            {
                // 最后一项吃掉舍入误差，避免总宽超出
                result[i] = i == weights.Length - 1
                    ? flexible - used
                    : Mathf.Floor(flexible * weights[i] / total);

                used += result[i];
            }

            return result;
        }


        /// <summary>
        /// 当前面板可用于表格的宽度
        /// </summary>
        private float AvailableWidth
        {
            get
            {
                // currentViewWidth 是整个 EditorWindow 宽度，含总面板左侧菜单(140) + 分隔(10) + 边框余量
                float w = EditorGUIUtility.currentViewWidth - LEFT_MENU_RESERVED;
                if (w <= MIN_TABLE_WIDTH) w = MIN_TABLE_WIDTH;
                return w;
            }
        }

        /// <summary>
        /// 总面板左侧菜单 + 分隔条占用的宽度
        /// <para>EFConfigsPanel 左侧列表 140px + hostview 分隔 10px + 边框余量。</para>
        /// </summary>
        private const float LEFT_MENU_RESERVED = 165f;

        /// <summary> 表格最小宽度：低于此值时各列按权重均分，不再无限压缩 </summary>
        private const float MIN_TABLE_WIDTH = 320f;


        #endregion

        /// <summary> 当前数据 </summary>
        private LocalizationKeyFile _file;

        /// <summary> 当前子页 </summary>
        private SubTab _tab = SubTab.Keys;

        /// <summary> 展开的分组集合 </summary>
        private readonly HashSet<string> _collapsedGroups = new HashSet<string>();

        /// <summary> 有未保存改动 </summary>
        private bool _dirty;

        /// <summary> 搜索过滤 </summary>
        private string _filter = string.Empty;

        /// <summary> 只看 Dynamic 的过滤开关 </summary>
        private bool _onlyDynamic;

        private Vector2 _scrollPos;
        private Vector2 _groupScrollPos;

        /// <summary> 新增分组输入框 </summary>
        private string _newGroupName = string.Empty;

        /// <summary>
        /// 面板名称
        /// </summary>
        public override string Name => LC.Combine(Lc.Multilingual, Lc.Config);

        /// <summary>
        /// 清单 JSON 绝对路径
        /// </summary>
        private static string JsonPath => Path.Combine(
            Path.Combine(Application.dataPath, "Packages/cn.efefef.localization"),
            Path.Combine(DataDir, FileName));

        #region EFConfigPanelBase 生命周期

        /// <summary>
        /// 首次进入面板
        /// </summary>
        /// <param name="assetsPath">配置总路径（本面板不使用，文件路径为包内固定路径）</param>
        public override void OnEnable(string assetsPath)
        {
            LoadOrCreate();
        }

        /// <summary>
        /// 窗口数据加载
        /// </summary>
        public override void LoadWindowData()
        {
            if (_file != null) return;
            LoadOrCreate();
        }

        /// <summary>
        /// 离开面板时自动落盘，避免未保存改动丢失
        /// </summary>
        public override void OnDestroy()
        {
            if (_dirty)
                Save(silent: true);
        }

        #endregion

        /// <summary>
        /// 加载清单；不存在时保持空状态，等待用户点击创建
        /// <para>Load only. If the file does not exist, stay in the empty state.</para>
        /// </summary>
        private void LoadOrCreate()
        {
            _file = LocalizationKeyFile.Load(JsonPath);
            _dirty = false;
        }

        /// <summary>
        /// 创建清单文件并写入模板内容
        /// </summary>
        private void CreateFile()
        {
            if (_file != null) return;

            var template = LocalizationKeyFile.CreateTemplate();
            template.EnsureDefaults();

            if (!template.Save(JsonPath))
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Error),
                    $"LocalizationKeys.json\n{LC.Combine(Lc.Save, Lc.Failed)}", LC.Combine(Lc.Confirm));
                return;
            }

            AssetDatabase.Refresh();
            _file = LocalizationKeyFile.Load(JsonPath);
            _dirty = false;
        }

        /// <summary>
        /// 覆写 JSON
        /// </summary>
        private bool Save(bool silent = false)
        {
            if (_file == null) return false;

            SyncGroupsFromKeys();

            if (!_file.Save(JsonPath))
            {
                if (!silent)
                    EditorUtility.DisplayDialog("保存失败", "无法写入 Key 清单文件，请查看 Console。", "确定");
                return false;
            }

            _dirty = false;
            AssetDatabase.Refresh();

            return true;
        }

        /// <summary>
        /// 把 key 实际用到的分组并入登记表
        /// <para>只补不删：登记表里可以有暂时没有 key 的空分组（预设分组）。</para>
        /// </summary>
        private void SyncGroupsFromKeys()
        {
            if (_file?.meta == null) return;
            _file.meta.groups ??= new List<string>();

            foreach (var entry in _file.keys)
            {
                if (string.IsNullOrEmpty(entry.group)) continue;
                if (!_file.meta.groups.Contains(entry.group))
                    _file.meta.groups.Add(entry.group);
            }
        }

        #region 绘制

        /// <summary>
        /// 界面绘制
        /// </summary>
        public override void OnGUI()
        {
            if (_file == null) LoadOrCreate();

            // 首次进入：只显示引导页，点创建后才出现表格内容
            if (_file == null)
            {
                DrawEmptyState();
                return;
            }

            DrawToolbar();

            switch (_tab)
            {
                case SubTab.Keys:
                    DrawKeysTab();
                    break;

                case SubTab.Groups:
                    DrawGroupsTab();
                    break;
            }
        }

        /// <summary>
        /// 空状态引导页：尚未创建 Key 清单时显示
        /// </summary>
        private void DrawEmptyState()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.HelpBox(LC.Combine(Lc.Multilingual, Lc.Config, Lc.Not, Lc.Exist), MessageType.Warning);
            EditorGUILayout.Space(12f);

            if (!GUILayout.Button(LC.Combine(Lc.Create, Lc.Config), GUILayout.Width(AvailableWidth))) return;
            CreateFile();
        }

        /// <summary>
        /// 顶部工具栏
        /// </summary>
        private void DrawToolbar()
        {
            // 保存 / 刷新：填满面板宽度，各占一半（与下方子页切换一致）
            EditorGUILayout.BeginHorizontal();

            var prevColor = GUI.contentColor;
            GUI.contentColor = GUIUtils.LightGreen;
            // 宽度按 AvailableWidth 均分，与下方表格列对齐（裸 Button 会按整行宽撑开）
            float half = Mathf.Floor(AvailableWidth * 0.5f);

            if (GUILayout.Button(LC.Combine(Lc.Save, Lc.Content),
                    GUILayout.Width(half), GUILayout.Height(20f)))
                Save();
            GUI.contentColor = prevColor;

            if (GUILayout.Button(LC.Combine(Lc.Refresh, Lc.All, Lc.Content),
                    GUILayout.Width(AvailableWidth - half), GUILayout.Height(20f)))
            {
                if (!_dirty ||
                    EditorUtility.DisplayDialog(LC.Combine(Lc.Tips), LC.Combine(Lc.Refresh, Lc.Text), "确定", "取消"))
                    LoadOrCreate();
            }

            EditorGUILayout.EndHorizontal();

            if (_dirty)
                GUILayout.Label(LC.Combine(Lc.Need, Lc.Save, Lc.Text), GUIUtils.ColorText(textColor:Color.yellow));

            EditorGUILayout.Space(_dirty ? 18f : 37f);

            // 子页切换（等宽平分，原生 Tab 组观感）
            DrawSubTabs();

            EditorGUILayout.Space(2f);
        }

        /// <summary>
        /// 子页切换按钮（等宽，选中态高亮）
        /// <para>自己画背景 + Box 而非用 GUI.backgroundColor：后者是乘性着色，
        /// 会与按钮贴图相乘导致发灰偏暗，且随编辑器主题漂移。</para>
        /// </summary>
        /// <summary>
        /// 子页切换
        /// <para>原生 Tab 组外观，宽度按内容分配（两段文案等长时视觉上等宽）。</para>
        /// </summary>
        private void DrawSubTabs()
        {
            var contents = new[] { new GUIContent(TabText(SubTab.Keys)), new GUIContent(TabText(SubTab.Groups)) };
            _tab = (SubTab)GUILayout.Toolbar((int)_tab, contents, GUILayout.Height(20f), GUILayout.Width(AvailableWidth));
        }

        /// <summary>
        /// 子页标题文案
        /// </summary>
        private static string TabText(SubTab tab) => tab switch
        {
            SubTab.Keys => LC.Combine(Lc.Key, Lc.Name, Lc.List),
            SubTab.Groups => LC.Combine(Lc.Multilingual, Lc.Config, Lc.Settings),
            _ => string.Empty,
        };

        #endregion

        #region 子页1 · Key 列表

        /// <summary>
        /// 子页 1：Key 列表
        /// </summary>
        private void DrawKeysTab()
        {
            // 过滤区
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(LC.Combine(Lc.Find), GUILayout.Width(40f));
            _filter = EditorGUILayout.TextField(_filter, GUILayout.Width(200f));

            _onlyDynamic = GUILayout.Toggle(_onlyDynamic, $"{LC.Combine(Lc.Only, Lc.Display)} Dynamic", GUILayout.Width(180f));

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2f);

            DrawKeysTable();
            DrawKeysFooter();
            
        }

        /// <summary>
        /// Key 表格
        /// <para>先按分组聚合再渲染，保证同名分组并到一处；否则末尾新增同组 key 会插出第二个组头。</para>
        /// </summary>
        private void DrawKeysTable()
        {
            DrawKeysHeader();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true));

            // 聚合：order 保持分组首次出现顺序，ungrouped 收纳无分组条目
            var order = new List<string>();
            var buckets = new Dictionary<string, List<LocalizationKeyEntry>>(StringComparer.Ordinal);
            List<LocalizationKeyEntry> ungrouped = null;

            foreach (var entry in _file.keys)
            {
                if (!PassFilter(entry)) continue;

                var g = entry.group ?? string.Empty;
                if (string.IsNullOrEmpty(g))
                {
                    // 无分组条目统一排在最后
                    ungrouped ??= new List<LocalizationKeyEntry>();
                    ungrouped.Add(entry);
                    continue;
                }

                if (!buckets.TryGetValue(g, out var list))
                {
                    list = new List<LocalizationKeyEntry>();
                    buckets[g] = list;
                    order.Add(g);
                }

                list.Add(entry);
            }

            foreach (var group in order)
            {
                DrawGroupHeader(group);
                if (_collapsedGroups.Contains(group)) continue;

                foreach (var entry in buckets[group]) DrawKeyRow(entry);
            }

            if (ungrouped != null)
            {
                foreach (var entry in ungrouped) DrawKeyRow(entry);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary> 窄列实际宽度（空间不足时收缩），由 GetKeyColWidths 每帧更新 </summary>
        private float _kindWidth = COL_KIND;
        private float _btnWidth = COL_BTN;

        /// <summary>
        /// 计算 Key 表格各列宽度
        /// <para>表头与数据行共用，避免列宽不一致导致错位。</para>
        /// </summary>
        private float[] GetKeyColWidths()
        {
            float avail = AvailableWidth;

            // 窄列按可用空间收缩，最小 24px（否则文字完全不可见）
            float fixedTotal = COL_KIND + COL_BTN;
            if (fixedTotal > avail * 0.45f)
            {
                float shrink = avail * 0.45f / fixedTotal;
                _kindWidth = Mathf.Max(24f, COL_KIND * shrink);
                _btnWidth = Mathf.Max(24f, COL_BTN * shrink);
            }
            else
            {
                _kindWidth = COL_KIND;
                _btnWidth = COL_BTN;
            }

            // 扣除窄列与行尾留白，剩余空间按权重分给弹性列
            return DistributeWidth(avail, _kindWidth + _btnWidth + COL_TAIL_PAD,
                WeightName, WeightDes, WeightGroup, WeightNote);
        }

        /// <summary>
        /// 计算分组页各列宽度
        /// </summary>
        private float[] GetGroupColWidths()
            => DistributeWidth(AvailableWidth, COL_GROUP_ACTIONS + COL_TAIL_PAD,
                WeightGroupName, WeightGroupCount);

        /// <summary>
        /// Key 表格表头
        /// </summary>
        private void DrawKeysHeader()
        {
            var w = GetKeyColWidths();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(LC.Combine(Lc.Key, Lc.Name), GUILayout.Width(w[0]));
            EditorGUILayout.LabelField(LC.Combine(Lc.Description), GUILayout.Width(w[1]));
            EditorGUILayout.LabelField(LC.Combine(Lc.Group, Lc.Name), GUILayout.Width(w[2]));
            EditorGUILayout.LabelField(LC.Combine(Lc.Display, Lc.Type), GUILayout.Width(_kindWidth));
            EditorGUILayout.LabelField(LC.Combine(Lc.Remark), GUILayout.Width(w[3]));
            EditorGUILayout.LabelField(LC.Combine(Lc.Delete), GUILayout.Width(_btnWidth));
            EditorGUILayout.LabelField("", GUILayout.Width(COL_TAIL_PAD));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary> group 下拉选项（每帧构造一次，供所有行共享） </summary>
        private readonly List<string> _groupOptions = new List<string>();

        /// <summary>
        /// 构造 group 下拉选项：首项为空分组占位，其后为登记分组
        /// <para>Build group dropdown options once per frame, shared by all rows.</para>
        /// </summary>
        private List<string> GetGroupOptions()
        {
            _groupOptions.Clear();
            _groupOptions.Add(string.Empty); // 无分组

            if (_file != null)
                _groupOptions.AddRange(_file.GetGroupsOrInfer());

            return _groupOptions;
        }

        /// <summary>
        /// Key 单行
        /// </summary>
        private void DrawKeyRow(LocalizationKeyEntry entry)
        {
            var w = GetKeyColWidths();

            EditorGUILayout.BeginHorizontal();

            // name：重名或非法字符标红
            var dup = IsDuplicated(entry);
            var prevColor = GUI.color;
            if (dup || !entry.IsValidName()) GUI.color = new Color(1f, 0.62f, 0.62f);

            var newName = EditorGUILayout.TextField(entry.name, EditorStyles.miniTextField, GUILayout.Width(w[0]));
            if (newName != entry.name)
            {
                entry.name = newName;
                _dirty = true;
            }

            GUI.color = prevColor;

            // des：为空标黄（必填项缺失）
            if (string.IsNullOrEmpty(entry.des)) GUI.color = new Color(1f, 0.95f, 0.7f);
            var newDes = EditorGUILayout.TextField(entry.des, EditorStyles.miniTextField, GUILayout.Width(w[1]));
            if (newDes != entry.des)
            {
                entry.des = newDes;
                _dirty = true;
            }

            GUI.color = prevColor;

            // group：下拉，选项来自登记表 + 实际用到的分组（选项表每帧只构造一次）
            var options = GetGroupOptions();

            int currentGroupIndex = options.IndexOf(entry.group ?? string.Empty);
            if (currentGroupIndex < 0)
            {
                // key 的分组不在登记表中（可能手改过 JSON）：在临时副本里补到首位，
                // 不能改动共享列表，否则会影响其它行
                options = new List<string>(options) { entry.group };
                currentGroupIndex = 0;
            }

            EditorGUI.BeginChangeCheck();
            var newGroup = EditorGUILayout.Popup(currentGroupIndex, options.ToArray(),
                EditorStyles.miniTextField, GUILayout.Width(w[2]));
            if (EditorGUI.EndChangeCheck())
            {
                entry.group = options[newGroup];
                _dirty = true;
            }

            // kind：枚举下拉
            EditorGUI.BeginChangeCheck();
            var newKind = (LocalizationKeyKind)EditorGUILayout.EnumPopup(
                entry.kind, EditorStyles.miniTextField, GUILayout.Width(_kindWidth));
            if (EditorGUI.EndChangeCheck())
            {
                entry.kind = newKind;
                _dirty = true;
            }

            // note
            var newNote = EditorGUILayout.TextField(entry.note, EditorStyles.miniTextField, GUILayout.Width(w[3]));
            if (newNote != entry.note)
            {
                entry.note = newNote;
                _dirty = true;
            }

            // 删除
            if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(_btnWidth)))
            {
                _file.keys.Remove(entry);
                _dirty = true;
            }

            // 行尾留白，避免删除按钮紧贴右边界
            EditorGUILayout.LabelField("", GUILayout.Width(COL_TAIL_PAD));

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Key 列表底部：新增按钮 + 问题汇总
        /// </summary>
        private void DrawKeysFooter()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button($"+ {LC.Combine(Lc.Add, Lc.One, Lc.Element)}", GUILayout.Width(120f)))
            {
                _file.keys.Add(new LocalizationKeyEntry { name = "", des = "", group = DefaultGroup });
                _dirty = true;
            }

            GUILayout.FlexibleSpace();

            var issues = Validate();
            if (issues.Count > 0)
            {
                var warnColor = GUI.contentColor;
                GUI.contentColor = new Color(1f, 0.78f, 0.4f);
                GUILayout.Label($"⚠ {issues.Count} {LC.Combine(Lc.Element, Lc.Is, Lc.Illegal)}",
                    EditorStyles.miniLabel);
                GUI.contentColor = warnColor;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(18f);

            // 宽度显式指定：裸 Button 会按整行宽撑开，比表格列宽多出左侧菜单那部分，导致右侧被裁
            if (GUILayout.Button(LC.Combine(Lc.Delete, Lc.All), GUILayout.Width(AvailableWidth)))
                DeleteAllKeys();
        }

        /// <summary>
        /// 删除全部 key：二次确认后才执行
        /// <para>危险操作，必须走确认弹窗，避免误点丢失全部数据。</para>
        /// </summary>
        private void DeleteAllKeys()
        {
            int count = _file?.keys.Count ?? 0;
            if (count == 0) return;

            if (!EditorUtility.DisplayDialog(
                    LC.Combine(Lc.Delete, Lc.All),
                    $"{LC.Combine(Lc.Confirm, Lc.Delete, Lc.All, Lc.Key)} ? " +
                    $"\n{LC.Combine(Lc.Current, Lc.Count, Lc.Is)}:  {count} ",
                    LC.Combine(Lc.Confirm),
                    LC.Combine(Lc.Cancel)))
                return;

            _file?.keys.Clear();
            _dirty = true;
        }

        /// <summary>
        /// 过滤判定
        /// </summary>
        private bool PassFilter(LocalizationKeyEntry entry)
        {
            if (_onlyDynamic && entry.kind != LocalizationKeyKind.Dynamic)
                return false;

            if (string.IsNullOrEmpty(_filter)) return true;

            return Contains(entry.name, _filter) || Contains(entry.des, _filter)
                                                 || Contains(entry.group, _filter) || Contains(entry.note, _filter);
        }

        private static bool Contains(string source, string keyword)
            => !string.IsNullOrEmpty(source) &&
               source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// 分组折叠头
        /// </summary>
        private void DrawGroupHeader(string group)
        {
            bool collapsed = _collapsedGroups.Contains(group);

            EditorGUILayout.BeginHorizontal();
            bool nowCollapsed = EditorGUILayout.Foldout(collapsed, $" {LC.Combine(Lc.Group)}: [ {group} ]", true);
            EditorGUILayout.EndHorizontal();

            _collapsedGroups.Remove(group);
            if (nowCollapsed) _collapsedGroups.Add(group);
        }

        /// <summary>
        /// 该条目的 name 是否与其它条目重复
        /// </summary>
        private bool IsDuplicated(LocalizationKeyEntry entry)
        {
            if (string.IsNullOrEmpty(entry.name)) return false;

            int count = 0;
            foreach (var e in _file.keys)
            {
                if (string.Equals(e.name, entry.name, StringComparison.Ordinal))
                    count++;

                if (count > 1) return true;
            }

            return false;
        }

        #endregion

        #region 子页2 · 分组与种类

        /// <summary>
        /// 正在重命名的分组（为空表示非重命名模式）
        /// </summary>
        private string _renamingGroup;

        /// <summary>
        /// 重命名中的临时输入
        /// </summary>
        private string _renamingInput = string.Empty;

        /// <summary>
        /// 子页 2：分组与种类
        /// </summary>
        private void DrawGroupsTab()
        {
            DrawGroupsHeader();

            _groupScrollPos = EditorGUILayout.BeginScrollView(_groupScrollPos, GUILayout.ExpandWidth(true));

            var groups = _file.GetGroupsOrInfer();

            if (groups.Count == 0)
                EditorGUILayout.HelpBox($"[Localization] {LC.Combine(Lc.Class, Lc.Is, Lc.Empty)}", MessageType.Info);

            foreach (var group in groups)
            {
                // groups 是 GetGroupsOrInfer 返回的新 List（值拷贝），删除操作不影响本次遍历
                DrawGroupItem(group);
                EditorGUILayout.Space(18);
            }

            EditorGUILayout.EndScrollView();

            DrawGroupsFooter();
        }

        /// <summary>
        /// 分组列表单行：空分组标黄；重命名时就地编辑
        /// </summary>
        private void DrawGroupItem(string group)
        {
            var w = GetGroupColWidths();

            EditorGUILayout.BeginHorizontal();

            int keyCount = _file.GetGroupKeyCount(group);
            bool renaming = _renamingGroup == group;

            if (renaming)
            {
                var newName = EditorGUILayout.TextField(_renamingInput, EditorStyles.miniTextField,
                    GUILayout.Width(w[0]));

                // 占位，保持与表头 / 非重命名态的列对齐
                EditorGUILayout.LabelField("", EditorStyles.miniLabel, GUILayout.Width(w[1]));

                // 回车确认 / ESC 取消
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    {
                        CommitRename(group, newName);
                    }
                    else if (Event.current.keyCode == KeyCode.Escape)
                    {
                        _renamingGroup = null;
                        _renamingInput = string.Empty;
                    }
                }

                if (GUILayout.Button(LC.Combine(Lc.Confirm), EditorStyles.miniButton, GUILayout.Width(60f)))
                    CommitRename(group, newName);

                if (GUILayout.Button(LC.Combine(Lc.Cancel), EditorStyles.miniButton, GUILayout.Width(60f)))
                {
                    _renamingGroup = null;
                    _renamingInput = string.Empty;
                }

                EditorGUILayout.LabelField("", GUILayout.Width(COL_TAIL_PAD));

                EditorGUILayout.EndHorizontal();
                return;
            }

            var prevColor = GUI.color;
            if (keyCount == 0) GUI.color = new Color(1f, 0.95f, 0.7f);

            EditorGUILayout.LabelField(group, EditorStyles.miniTextField, GUILayout.Width(w[0]));

            GUI.color = prevColor;

            EditorGUILayout.LabelField($"{keyCount} {LC.Combine(Lc.Row)}",
                EditorStyles.miniLabel, GUILayout.Width(w[1]));

            if (GUILayout.Button(LC.Combine(Lc.Rename), EditorStyles.miniButton, GUILayout.Width(60f)))
            {
                _renamingGroup = group;
                _renamingInput = group;
            }

            if (GUILayout.Button(LC.Combine(Lc.Delete), EditorStyles.miniButton, GUILayout.Width(60f)))
                DeleteGroup(group);

            EditorGUILayout.LabelField("", GUILayout.Width(COL_TAIL_PAD));

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 提交重命名，同步修改所有 key 的归属
        /// </summary>
        private void CommitRename(string oldName, string newName)
        {
            newName = (newName ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(newName) || newName == oldName)
            {
                _renamingGroup = null;
                _renamingInput = string.Empty;
                return;
            }

            if (_file.GetGroupsOrInfer().Contains(newName))
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Error), $"{newName} {LC.Combine(Lc.Exist)}", "确定");
                return;
            }

            _file.meta ??= new LocalizationKeyMeta();
            _file.meta.groups ??= new List<string>();

            var index = _file.meta.groups.IndexOf(oldName);
            if (index >= 0) _file.meta.groups[index] = newName;

            int affected = 0;
            foreach (var entry in _file.keys)
            {
                if (!string.Equals(entry.group, oldName, StringComparison.Ordinal)) continue;
                entry.group = newName;
                affected++;
            }

            _collapsedGroups.Remove(oldName);
            _renamingGroup = null;
            _renamingInput = string.Empty;
            _dirty = true;
            D.Log($"[Localization] 分组 {oldName} → {newName}，影响 {affected} 条 key");
        }

        /// <summary>
        /// 删除分组
        /// <para>只从登记表移除；已有 key 仍归属该分组（会在下次保存时被 SyncGroupsFromKeys 补回），
        /// 若要一并转移归属需先用 Key 页的 group 下拉改。</para>
        /// </summary>
        private void DeleteGroup(string group)
        {
            _file.meta ??= new LocalizationKeyMeta();
            _file.meta.groups ??= new List<string>();

            int keyCount = _file.GetGroupKeyCount(group);

            // 还有 key 归属该组时，明确告知「删不掉」，避免误解
            if (keyCount > 0)
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Tips),
                    $"{group} {LC.Combine(Lc.Row)}: {keyCount}\n\n" +
                    $"{LC.Combine(Lc.Class, Lc.Delete)}: {keyCount}",
                    "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog(LC.Combine(Lc.Tips), group, "确定", "取消"))
                return;

            _file.meta.groups.Remove(group);
            _collapsedGroups.Remove(group);
            _dirty = true;
            D.Log($"[Localization] 已删除分组：{group}");
        }

        /// <summary>
        /// 分组列表表头
        /// </summary>
        private void DrawGroupsHeader()
        {
            var w = GetGroupColWidths();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(LC.Combine(Lc.Class, Lc.Name), EditorStyles.toolbarButton,
                GUILayout.Width(w[0]));
            EditorGUILayout.LabelField(LC.Combine(Lc.Count), EditorStyles.toolbarButton,
                GUILayout.Width(w[1]));
            EditorGUILayout.LabelField("", EditorStyles.toolbarButton, GUILayout.Width(COL_GROUP_ACTIONS));
            EditorGUILayout.LabelField("", EditorStyles.toolbarButton, GUILayout.Width(COL_TAIL_PAD));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 分组列表底部：新增分组 + 种类与文件信息
        /// </summary>
        private void DrawGroupsFooter()
        {
            var w = GetGroupColWidths();

            EditorGUILayout.BeginHorizontal();

            _newGroupName = EditorGUILayout.TextField(_newGroupName, GUILayout.Width(w[0]));

            var prevColor = GUI.contentColor;
            GUI.contentColor = GUIUtils.LightGreen;
            if (GUILayout.Button($"+ {LC.Combine(Lc.Class)}", GUILayout.Width(COL_GROUP_ACTIONS)))
                AddGroup(_newGroupName);
            GUI.contentColor = prevColor;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);

            // 种类：枚举代码级，不在此处增删
            EditorGUILayout.LabelField(LC.Combine(Lc.Type), EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Static / Dynamic",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(4f);

            // 其他设置
            EditorGUILayout.LabelField(LC.Combine(Lc.Config), EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{LC.Combine(Lc.Text, Lc.File)}: {Path.GetFileName(JsonPath)}",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(LC.Combine(Lc.Class, Lc.Count) + $": {_file.meta?.groups?.Count ?? 0}",
                EditorStyles.miniLabel);
        }

        /// <summary>
        /// 新增分组
        /// </summary>
        private void AddGroup(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            _file.meta ??= new LocalizationKeyMeta();
            _file.meta.groups ??= new List<string>();

            if (_file.meta.groups.Contains(name))
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Error), $"{name} {LC.Combine(Lc.Exist)}", "确定");
                return;
            }

            _file.meta.groups.Add(name);
            _newGroupName = string.Empty;
            _dirty = true;
            D.Log($"[Localization] 已新增分组：{name}");
        }

        #endregion

        #region 校验

        /// <summary>
        /// 校验当前数据
        /// </summary>
        public List<string> Validate()
        {
            var issues = new List<string>();
            if (_file == null) return issues;

            foreach (var entry in _file.keys)
            {
                if (string.IsNullOrEmpty(entry.name))
                    issues.Add($"{LC.Combine(Lc.Name, Lc.Is, Lc.Empty)}");
                else if (IsDuplicated(entry))
                    issues.Add($"{LC.Combine(Lc.Row)} {LC.Combine(Lc.Repetition)}: {entry.name}");

                if (!entry.IsValidName())
                    issues.Add(
                        $"{LC.Combine(Lc.Row)} {LC.Combine(Lc.Name, Lc.Is, Lc.Illegal, Lc.Character)}: {entry.name}");

                if (string.IsNullOrEmpty(entry.des))
                    issues.Add($"des {LC.Combine(Lc.Is, Lc.Empty)}: {entry.name}");
            }

            return issues;
        }

        /// <summary>
        /// 供菜单调用：校验并打印报告
        /// </summary>
        [MenuItem(MenuItemToolkit.Utility + "🔍 Validate Localization Keys", false,
            MenuItemToolkit.UtilityPriority + 30)]
        public static void ValidateFromMenu()
        {
            var file = LocalizationKeyFile.Load(JsonPath);
            if (file == null)
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Error), "LocalizationKeys.json not found.", "确定");
                return;
            }

            file.EnsureDefaults();
            var issues = ValidateStatic(file);

            if (issues.Count == 0)
            {
                D.Log($"[Localization] Key 清单校验通过，共 {file.keys.Count} 条");
                EditorUtility.DisplayDialog(LC.Combine(Lc.Success), $"共 {file.keys.Count} 条 key。", "确定");
                return;
            }

            var text = string.Join("\n", issues);
            D.Warning($"[Localization] Key 清单存在 {issues.Count} 项问题\n{text}");
            EditorUtility.DisplayDialog(LC.Combine(Lc.Error), $"{issues.Count} items:\n\n{text}", "确定");
        }

        /// <summary>
        /// 静态校验（面板未打开时用）
        /// </summary>
        private static List<string> ValidateStatic(LocalizationKeyFile file)
        {
            var issues = new List<string>();
            var seen = new HashSet<string>();

            foreach (var entry in file.keys)
            {
                if (string.IsNullOrEmpty(entry.name)) issues.Add("空 name");
                else if (!seen.Add(entry.name)) issues.Add($"name 重复：{entry.name}");

                if (!entry.IsValidName()) issues.Add($"name 含非法字符：{entry.name}");
                if (string.IsNullOrEmpty(entry.des)) issues.Add($"des 为空：{entry.name}");
            }

            return issues;
        }

        #endregion
    }
}