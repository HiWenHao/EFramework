/*
 * ================================================
 * Describe:        This script is used to builder with editor.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2023-02-13 16:46:15
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-28 16:35:35
 * ScriptVersion:   0.4
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EasyFramework.Managers.Ui;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// Ui builder with editor.
    /// </summary>
    [CustomEditor(typeof(UiBinding))]
    public class UiBindingEditor : Editor
    {
        /// <summary> 绑定行中 名字 与 拖拽区 的最小宽度 </summary>
        private const float BindRowFieldMinWidth = 80f;

        private UiBinding _builder;
        private UiBindingConfig _setting;
        private Vector2 _bindListScrollPos;
        private List<string> _tempFiledNames;
        private List<string> _tempComponentTypeNames;
        private Dictionary<string, int> _componentsName;
        private bool _programmaticChangePending;

        private void OnEnable()
        {
            _builder = (UiBinding)target;
            _setting = UiBindingConfig.Instance;

            _builder.Namespace = string.IsNullOrEmpty(_builder.Namespace)
                ? ConfigManager.Project.ScriptNamespace
                : _builder.Namespace;
            if (_builder.CreatePrefab)
                _builder.PrefabPath = string.IsNullOrEmpty(_builder.PrefabPath)
                    ? ConfigManager.Path.UIPrefabPath
                    : _builder.PrefabPath;
            _builder.ScriptPath = string.IsNullOrEmpty(_builder.ScriptPath)
                ? ConfigManager.Path.UICodePath
                : _builder.ScriptPath;

            _tempFiledNames = new List<string>();
            _tempComponentTypeNames = new List<string>();
            _componentsName = new Dictionary<string, int>();

            _builder.SortByType =
                EditorPrefs.GetInt(ConfigManager.Project.AppConst.AppPrefix + "UiBindSortType", 1) == 1;
            _builder.SortByNameLength =
                EditorPrefs.GetInt(ConfigManager.Project.AppConst.AppPrefix + "UiBindSortName", 1) == 1;
            _builder.PackUpBindList = true;

            serializedObject.ApplyModifiedProperties();
        }

        private void OnDisable()
        {
            _tempFiledNames.Clear();
            _componentsName.Clear();
            _tempComponentTypeNames.Clear();
            _setting = null;
            _tempFiledNames = null;
            _componentsName = null;
            _tempComponentTypeNames = null;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            _programmaticChangePending = false;

            DrawSetting();

            DrawAutoBind();

            DrawKvData();

            DrawStartBind();

            bool guiChanged = EditorGUI.EndChangeCheck();
            if (!guiChanged && !_programmaticChangePending)
                return;
            if (!target || !serializedObject.targetObject)
                return;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        #region Draw. 绘制编辑器内容

        private void DrawSetting()
        {
            EditorGUILayout.BeginHorizontal();
            _builder.Namespace =
                EditorGUILayout.TextField(LC.Combine(Lc.Script, Lc.Namespace), _builder.Namespace);
            if (GUILayout.Button(LC.Combine(Lc.Default, Lc.Settings)))
                _builder.Namespace = ConfigManager.Project.ScriptNamespace;
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(LC.Combine(Lc.Script, Lc.Class, Lc.Name), _builder.gameObject.name);
            EditorGUI.EndDisabledGroup();

            _builder.Describe =
                EditorGUILayout.TextField(LC.Combine(Lc.Script, Lc.Description), _builder.Describe);
            EditorGUILayout.Space(6f, true);

            _builder.AutoDestroy =
                EditorGUILayout.Toggle(LC.Combine(Lc.Auto, Lc.Destroy), _builder.AutoDestroy);
            if (_builder.AutoDestroy)
            {
                _builder.AutoDestroyCountdown =
                    EditorGUILayout.FloatField(LC.Combine(Lc.Destroy, Lc.Countdown),
                        _builder.AutoDestroyCountdown);
                EditorGUILayout.Space(6f, true);
            }

            _builder.ViewType = (UIViewType)EditorGUILayout.EnumPopup("UI" + LC.Combine(Lc.Type), _builder.ViewType);
            if (_builder.ViewType is UIViewType.Cache)
                _builder.ViewType = UIViewType.Page;

            // UI类型下方的两个动画相关字段：动画类型 + 关闭时是否重播动画
            _builder.ViewAnimationType = (UiViewAnimationType)EditorGUILayout.EnumPopup(
                LC.Combine(Lc.Animat, Lc.Type), _builder.ViewAnimationType);
            _builder.CloseViewReverseAnimation = EditorGUILayout.ToggleLeft(
                "  " + LC.Combine(Lc.When, Lc.Close,Lc.View) + ", " + LC.Combine(Lc.Reverse, Lc.Play1, Lc.Animat), _builder.CloseViewReverseAnimation);

            EditorGUILayout.Space(12f, true);

            EditorGUI.BeginChangeCheck();
            _builder.CreatePrefab = EditorGUILayout.Toggle(
                _builder.CreatePrefab
                    ? LC.Combine(Lc.Prefab, Lc.Save, Lc.Path)
                    : LC.Combine(Lc.Create, Lc.Prefab),
                _builder.CreatePrefab);
            if (EditorGUI.EndChangeCheck())
            {
                if (_builder.CreatePrefab && string.IsNullOrEmpty(_builder.PrefabPath))
                    _builder.PrefabPath = ConfigManager.Path.UIPrefabPath;
                Repaint();
                EditorUtility.SetDirty(_builder);
            }

            EditorGUILayout.BeginHorizontal();
            if (_builder.CreatePrefab)
            {
                if (string.IsNullOrEmpty(_builder.PrefabPath))
                    _builder.PrefabPath = ConfigManager.Path.UIPrefabPath;

                EditorGUILayout.LabelField(_builder.PrefabPath, GUILayout.ExpandWidth(true));
                DrawSettingSelectPath(false);
            }
            else
            {
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12f, true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LC.Combine(Lc.Code, Lc.Save, Lc.Path));
            DrawSettingSelectPath(true);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(_builder.ScriptPath);
        }

        private void DrawSettingSelectPath(bool isCodePath)
        {
            if (GUILayout.Button(LC.Combine(Lc.Select, Lc.Path), GUILayout.Width(100)))
            {
                string folder = Path.Combine(Application.dataPath,
                    isCodePath ? _builder.ScriptPath : _builder.PrefabPath);
                if (!Directory.Exists(folder)) folder = Application.dataPath;
                string path = EditorUtility.OpenFolderPanel(LC.Combine(Lc.Select, Lc.Path), folder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    string assetPath = AbsoluteToAssetPath(path);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        if (isCodePath)
                            _builder.ScriptPath = assetPath + "/";
                        else
                            _builder.PrefabPath = assetPath + "/";
                        EditorUtility.SetDirty(_builder);
                    }
                    else
                    {
                        D.Warning($"Selected path '{path}' is not inside the Assets folder.");
                    }
                }

                Repaint();
            }

            if (!GUILayout.Button(LC.Combine(Lc.Default, Lc.Settings), GUILayout.Width(100))) return;
            if (isCodePath)
                _builder.ScriptPath = ConfigManager.Path.UICodePath;
            else
                _builder.PrefabPath = ConfigManager.Path.UIPrefabPath;
            EditorUtility.SetDirty(_builder);
            Repaint();
        }

        private void DrawAutoBind()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            _builder.SortByType =
                EditorGUILayout.Toggle(LC.Combine(Lc.By, Lc.Type, Lc.Sort), _builder.SortByType);
            _builder.SortByNameLength =
                EditorGUILayout.Toggle(LC.Combine(Lc.By, Lc.Name, Lc.Length, Lc.Sort),
                    _builder.SortByNameLength);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button(LC.Combine(Lc.Auto, Lc.Bind, Lc.Component)))
            {
                AutoBindComponent();
            }
        }

        private void DrawKvData()
        {
            _builder.PackUpBindList = EditorGUILayout.BeginFoldoutHeaderGroup(_builder.PackUpBindList,
                _builder.PackUpBindList ? LC.Combine(Lc.Close, Lc.List) : LC.Combine(Lc.Open, Lc.List));

            if (_builder.PackUpBindList)
            {
                EditorGUILayout.BeginVertical("box");
                int bindCount = _builder.BindDatas.Count;
                int visibleRows = bindCount < 10 ? 10 : Mathf.Min(bindCount, 25);
                float rowH = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                _bindListScrollPos = EditorGUILayout.BeginScrollView(
                    _bindListScrollPos, GUILayout.Height(visibleRows * rowH));

                int needDeleteIndex = -1;
                for (int i = 0; i < _builder.BindDatas.Count; i++)
                {
                    var bindData = _builder.BindDatas[i];

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{i}", GUILayout.Width(20));
                    bool isEnabled = !bindData.Disabled;
                    bool toggled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));
                    if (toggled != isEnabled)
                    {
                        Undo.RecordObject(_builder, "Toggle BindData Enabled");
                        _builder.BindDatas[i].Disabled = !toggled;
                    }

                    EditorGUI.BeginChangeCheck();
                    string editedName = EditorGUILayout.TextField(bindData.ScriptName, GUILayout.ExpandWidth(true), GUILayout.MinWidth(BindRowFieldMinWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        string filtered = FilterIdentifier(editedName);
                        if (filtered != bindData.ScriptName)
                        {
                            Undo.RecordObject(_builder, "Rename BindData ScriptName");
                            _builder.BindDatas[i].ScriptName = filtered;
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    Component newCom = (Component)EditorGUILayout.ObjectField(
                        bindData.BindCom, typeof(Component), true, GUILayout.ExpandWidth(true), GUILayout.MinWidth(BindRowFieldMinWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_builder, "Set BindCom (ObjectField)");
                        _builder.BindDatas[i].BindCom = newCom;
                        if (newCom != null)
                        {
                            _builder.BindDatas[i].RealName = newCom.gameObject.name;
                            // 拖拽绑定后：按用户规则重写字段名（缩写_对象名 / 缩写_原后缀 / 缩写_非法原值）
                            _builder.BindDatas[i].ScriptName = GenerateScriptName(
                                newCom.GetType().Name, newCom.gameObject.name, bindData.ScriptName);
                        }
                    }

                    DrawBindComponentPopup(i);

                    if (GUILayout.Button("X", GUILayout.Width(22))) needDeleteIndex = i;

                    EditorGUILayout.EndHorizontal();
                }

                if (needDeleteIndex != -1)
                {
                    Undo.RecordObject(_builder, "Remove BindData");
                    _builder.BindDatas.RemoveAt(needDeleteIndex);
                    _programmaticChangePending = true;
                    serializedObject.Update();
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                var nameIssues = ValidateScriptNames(_builder.BindDatas);
                if (nameIssues.Count > 0)
                    EditorGUILayout.HelpBox(string.Join("\n", nameIssues), MessageType.Warning);

                if (GUILayout.Button("+ " + LC.Combine(Lc.Add, Lc.One, Lc.Empty, Lc.Element), GUILayout.Height(24)))
                {
                    Undo.RecordObject(_builder, "Add empty BindData");
                    _builder.BindDatas.Add(new UiBinding.BindData());
                    _programmaticChangePending = true;
                    serializedObject.Update();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// 最右侧组件 Popup：列出目标子物体上的所有组件，点选切换绑定。
        /// 下拉项与当前 BindCom 每帧重新对齐避免错位；同类组件用 #序号 去重。
        /// </summary>
        private void DrawBindComponentPopup(int index)
        {
            var bindData = _builder.BindDatas[index];

            GameObject targetGo = bindData.BindCom != null
                ? bindData.BindCom.gameObject
                : FindChildByName(_builder.transform, bindData.RealName);

            var options = new List<string> { "None" };
            var components = new List<Component> { null };
            int selected = bindData.BindCom == null ? 0 : -1;

            if (targetGo != null)
            {
                var comps = targetGo.GetComponents<Component>();

                var totalByType = new Dictionary<string, int>();
                foreach (var c in comps)
                {
                    if (c == null || c is UiBinding) continue;
                    string t = c.GetType().Name;
                    totalByType[t] = totalByType.TryGetValue(t, out int v) ? v + 1 : 1;
                }

                var seenByType = new Dictionary<string, int>();
                foreach (var c in comps)
                {
                    if (c == null || c is UiBinding) continue;
                    string t = c.GetType().Name;
                    int n = seenByType.GetValueOrDefault(t, 0);
                    seenByType[t] = n + 1;
                    string label = totalByType[t] > 1 ? $"{t} #{n + 1}" : t;
                    options.Add(label);
                    components.Add(c);
                    if (c == bindData.BindCom) selected = components.Count - 1;
                }
            }

            if (selected < 0 && bindData.BindCom != null)
            {
                options.Insert(1, $"{bindData.BindCom.GetType().Name} (外部引用)");
                components.Insert(1, bindData.BindCom);
                selected = 1;
            }

            EditorGUI.BeginChangeCheck();
            int newSelected = EditorGUILayout.Popup(selected, options.ToArray(), GUILayout.Width(100));
            if (!EditorGUI.EndChangeCheck() || newSelected < 0 || newSelected >= components.Count) return;
            Undo.RecordObject(_builder, "Set BindCom (popup)");
            _builder.BindDatas[index].BindCom = components[newSelected];
            if (components[newSelected] == null) return;
            _builder.BindDatas[index].RealName = components[newSelected].gameObject.name;
            // 下拉选择后：按用户规则重写字段名（缩写_对象名 / 缩写_原后缀 / 缩写_非法原值）
            _builder.BindDatas[index].ScriptName = GenerateScriptName(
                components[newSelected].GetType().Name, components[newSelected].gameObject.name, bindData.ScriptName);
        }

        /// <summary>
        /// 按名称在层级中递归查找子物体（与自动绑定的命名规则匹配；存在同名时返回首个匹配）。
        /// </summary>
        private static GameObject FindChildByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;

            var direct = root.Find(name);
            if (direct != null) return direct.gameObject;

            foreach (Transform child in root)
            {
                var found = FindChildByName(child, name);
                if (found != null) return found;
            }

            return null;
        }

        #region ScriptName 校验. 字段名合法性（生成代码的 C# 标识符）

        /// <summary> C# 关键字集合（字段名命中则生成代码无法编译） </summary>
        private static readonly HashSet<string> CsharpKeywords = new HashSet<string>
        {
            "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const",
            "continue","default","delegate","do","double","else","enum","event","explicit","extern","false",
            "finally","fixed","float","for","foreach","goto","if","implicit","in","int","interface","internal",
            "is","lock","long","namespace","new","null","object","operator","out","override","params","private",
            "protected","public","readonly","ref","return","sbyte","sealed","short","sizeof","stackalloc","static",
            "string","struct","switch","this","throw","true","try","typeof","uint","ulong","unchecked","unsafe",
            "ushort","using","virtual","void","volatile","while","var","nameof"
        };

        /// <summary>
        /// 实时过滤用户输入，仅保留合法标识符字符（字母/数字/下划线，首字符须为字母或下划线）。
        /// 非法字符在输入时即被丢弃，避免生成不可编译代码。
        /// </summary>
        private static string FilterIdentifier(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var sb = new StringBuilder(raw.Length);
            for (int k = 0; k < raw.Length; k++)
            {
                char c = raw[k];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || (k > 0 && c >= '0' && c <= '9');
                if (ok) sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary> 判断是否为合法 C# 标识符（与 FilterIdentifier 规则一致） </summary>
        private static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            char first = name[0];
            if (!(first == '_' || (first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z'))) return false;
            for (int k = 1; k < name.Length; k++)
            {
                char c = name[k];
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 校验所有绑定字段名，返回会导致生成代码编译失败的问题列表（空/重复/关键字/非法字符）。
        /// 既用于列表底部的红色 HelpBox 警告，也在点击「创建」时作为最后一道防呆——
        /// 若存在任意问题则跳过代码/预制件生成并打印 error，避免产出编译不过的脚本。
        /// </summary>
        private static List<string> ValidateScriptNames(List<UiBinding.BindData> datas)
        {
            var issues = new List<string>();
            var seen = new Dictionary<string, int>();
            for (int idx = 0; idx < datas.Count; idx++)
            {
                string name = datas[idx].ScriptName;
                if (string.IsNullOrEmpty(name))
                {
                    issues.Add($"{idx} {LC.Combine(Lc.Row)}: {LC.Combine(Lc.Name, Lc.Is, Lc.Empty)}, {LC.Combine(Lc.Generate, Lc.Code, Lc.Will, Lc.Compile, Lc.Error)}");
                    continue;
                }
                if (CsharpKeywords.Contains(name))
                    issues.Add($"{idx} {LC.Combine(Lc.Row)}: {LC.Combine(Lc.Name, Lc.Is)} C# {LC.Combine(Lc.Keyword, Lc.Generate, Lc.Code, Lc.Will, Lc.Compile, Lc.Error)}");
                if (!IsValidIdentifier(name))
                    issues.Add($"{idx} {LC.Combine(Lc.Row)}: {LC.Combine(Lc.Name, Lc.Is, Lc.Illegal, Lc.Character)}, {LC.Combine(Lc.First, Lc.Character, Lc.Must, Lc.Be, Lc.Letter, Lc.Or, Lc.Underscore)}");
                if (seen.TryGetValue(name, out int first))
                    issues.Add($"{idx} {LC.Combine(Lc.Row)}: {LC.Combine(Lc.Name, Lc.And)} {first} {LC.Combine(Lc.Row, Lc.Repetition, Lc.Generate, Lc.Code, Lc.Will, Lc.Compile, Lc.Error)}");
                else
                    seen[name] = idx;
            }
            return issues;
        }

        /// <summary>
        /// 将组件类型名映射为 _rulePrefixes 中的缩写前缀（如 Image→Img、Button→Btn、TextMeshProUGUI→Tmp）；
        /// 不在规则表中则退化为合法化的类型名。
        /// </summary>
        private string GetPrefixAbbreviation(string componentTypeName)
        {
            if (_setting != null)
            {
                foreach (var rule in _setting.RulePrefixes)
                {
                    if (rule.FullName == componentTypeName)
                        return FilterIdentifier(rule.Prefix);
                }
            }
            return FilterIdentifier(componentTypeName);
        }

        /// <summary>
        /// 选定组件后，按用户规则重写 ScriptName：
        /// ① ScriptName 为空        → 组件类型缩写_对象名（对象名实时过滤为合法标识符）；
        /// ② ScriptName 非空且合法   → 仅把首个“_”之前的前缀替换为组件类型缩写，保留“_”之后的自定义部分；
        /// ③ ScriptName 非空但不合法 → 组件类型缩写_原（不合法）ScriptName。
        /// 命名只由所选组件类型 + 物体名决定，不做强去重；重名由底部 ValidateScriptNames 红色 HelpBox 提示。
        /// </summary>
        private string GenerateScriptName(string componentTypeName, string objectName, string currentScriptName)
        {
            string prefix = GetPrefixAbbreviation(componentTypeName);

            // ① 空：缩写_对象名
            if (string.IsNullOrEmpty(currentScriptName))
            {
                string objPart = FilterIdentifier(objectName);
                return string.IsNullOrEmpty(objPart) ? prefix : prefix + "_" + objPart;
            }

            // ② 合法：替换首个“_”之前的旧前缀，保留其后自定义部分
            if (IsValidIdentifier(currentScriptName))
            {
                int idx = currentScriptName.IndexOf('_');
                string rest = idx >= 0 ? currentScriptName.Substring(idx + 1) : currentScriptName;
                return string.IsNullOrEmpty(rest) ? prefix : prefix + "_" + rest;
            }

            // ③ 不合法：缩写_原值（保留用户输入，交由校验提示）
            return prefix + "_" + currentScriptName;
        }

        #endregion

        private void DrawStartBind()
        {
            EditorGUILayout.Space(24f);
            EditorGUILayout.Space(12f);

            var originalColor = GUI.contentColor;
            GUI.contentColor = GUIUtils.LightGreen;
            bool createClicked = GUILayout.Button(LC.Combine(Lc.Bind, Lc.Create), GUILayout.Height(25.0f));
            GUI.contentColor = originalColor;

            if (!createClicked) return;
            var nameIssues = ValidateScriptNames(_builder.BindDatas);
            if (nameIssues.Count > 0)
            {
                foreach (var issue in nameIssues)
                {
                    D.Error($"[UiBinding] 生成已终止，字段名问题 → {issue}");
                }
                return;
            }
            GenAutoBindCode();
            if (_builder.CreatePrefab) CreateOrModifyPrefab();
            EditorApplication.delayCall += AssetDatabase.Refresh;
        }

        #endregion

        /// <summary>
        /// 将绝对路径安全转换为 Unity Assets 路径（以 "Assets/" 开头）
        /// </summary>
        private static string AbsoluteToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;

            string normalizedInput = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string normalizedDataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');

            if (normalizedInput.StartsWith(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + normalizedInput[normalizedDataPath.Length..];
            }

            return null;
        }

        #region Bind. 绑定数据内容

        private void AutoBindComponent()
        {
            if (Application.isPlaying)
                return;

            Undo.RecordObject(_builder, "Auto Bind Components");
            _builder.BindDatas.Clear();
            _componentsName.Clear();
            Transform[] children = _builder.gameObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child == _builder.transform) continue;

                // 跳过属于其他 UiBinding 子物体的控件
                UiBinding autoSelf = child.gameObject.GetComponent<UiBinding>();
                UiBinding autoParent = child.gameObject.GetComponentInParent<UiBinding>(true);
                if (autoSelf == null && autoParent != null && autoParent != _builder) continue;

                _tempFiledNames.Clear();
                _tempComponentTypeNames.Clear();
                if (!IsValidBind(child, _tempFiledNames, _tempComponentTypeNames)) continue;

                for (int i = 0; i < _tempFiledNames.Count; i++)
                {
                    Component com = child.GetComponent(_tempComponentTypeNames[i]);
                    if (com == null) D.Error($"{child.name}上不存在{_tempComponentTypeNames[i]}的组件");
                    else AddBindData(child.name, _tempFiledNames[i], com);
                }
            }

            _programmaticChangePending = true;
            serializedObject.Update();
        }

        private void AddBindData(string rectName, string filedName, Component needBindingComponent)
        {
            int bindingCount = _builder.BindDatas.Count;
            List<string> nameList = new List<string>();
            for (int i = 0; i < bindingCount; i++)
            {
                string elementName = _builder.BindDatas[i].ScriptName;
                if (elementName == filedName)
                {
                    D.Error($"有重复名字！请检查后重新生成！Name:{rectName}");
                    return;
                }

                nameList.Add(elementName);
            }

            string componentsName = needBindingComponent.GetType().Name;
            List<string> comNameList = new List<string>();
            if (_builder.SortByType && _builder.SortByNameLength)
            {
                if (!_componentsName.TryAdd(componentsName, bindingCount))
                {
                    foreach (var item in _componentsName.Keys) comNameList.Add(item);
                    int indexOf = comNameList.IndexOf(componentsName) + 1;
                    for (int i = indexOf; i < comNameList.Count; i++) ++_componentsName[comNameList[i]];
                    int endIndex = indexOf >= comNameList.Count
                        ? _builder.BindDatas.Count
                        : _componentsName[comNameList[indexOf]] - 1;
                    bindingCount = EditorUtils.GetIndexWithLengthSort(filedName.Length, nameList,
                        _componentsName[comNameList[indexOf - 1]], endIndex);
                }
            }
            else if (_builder.SortByType && !_builder.SortByNameLength)
            {
                if (!_componentsName.TryAdd(componentsName, bindingCount))
                {
                    bindingCount = ++_componentsName[componentsName];
                    foreach (var item in _componentsName.Keys) comNameList.Add(item);
                    for (int i = comNameList.IndexOf(componentsName) + 1; i < comNameList.Count; i++)
                        ++_componentsName[comNameList[i]];
                }
            }
            else if (!_builder.SortByType && _builder.SortByNameLength)
                bindingCount = EditorUtils.GetIndexWithLengthSort(filedName.Length, nameList, 0, nameList.Count);
            else
                bindingCount = nameList.Count;

            _builder.BindDatas.Insert(bindingCount, new UiBinding.BindData
            {
                BindCom = needBindingComponent,
                RealName = rectName,
                ScriptName = filedName,
            });
        }

        private bool IsValidBind(Transform trans, List<string> filedNames, List<string> componentTypeNames)
        {
            string[] strArray = trans.name.Split('_');
            if (strArray.Length == 1) return false;

            bool isFind = false;
            string filedName = strArray[^1];
            filedName = EditorUtils.RemovePunctuation(filedName).Trim();

            for (int i = 0; i < strArray.Length - 1; i++)
            {
                string prefixes = strArray[i];
                foreach (RulePrefixes autoBindRulePrefix in _setting.RulePrefixes)
                {
                    if (!autoBindRulePrefix.Prefix.Equals(prefixes)) continue;
                    filedNames.Add($"{prefixes}_{filedName}");
                    componentTypeNames.Add(autoBindRulePrefix.FullName);
                    isFind = true;
                    break;
                }

                if (!isFind) D.Warning($"{trans.name}的命名中{prefixes}不存在对应的组件类型，绑定失败");
            }

            return isFind;
        }

        #endregion

        #region Create prefab file. 生成预制件文件

        private void CreateOrModifyPrefab()
        {
            string fullPath = Path.Combine(Application.dataPath, _builder.PrefabPath, $"{_builder.name}.prefab");
            fullPath = fullPath.Replace("Assets/Assets", "Assets");
            fullPath = fullPath.Replace("Assets\\Assets", "Assets");
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir) && dir != null) Directory.CreateDirectory(dir);
            PrefabUtility.SaveAsPrefabAssetAndConnect(_builder.gameObject, fullPath, InteractionMode.UserAction);
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region Create code file. 生成代码文件

        private const string StrChangeTime = "* ModifyTime:";
        private const string StrChangeAuthor = "* ModifyAuthor:";
        private const string ButtonEventsStart = "#region Button invoke event. Do not change here.不要更改这行 -- Auto";
        private const string ButtonEventsEnd = "#endregion button invoke event. Do not change here.不要更改这行 -- Auto";

        private const string ScriptExplain =
            "    //-----The script is auto generated. Please do not make any changes-----";

        /// <summary>
        /// Gen auto bind code on the basis of special scriptName.
        /// 基于特殊名称生成自动绑定代码
        /// </summary>
        private void GenAutoBindCode()
        {
            string codePath = !string.IsNullOrEmpty(_builder.ScriptPath)
                ? _builder.ScriptPath
                : ConfigManager.Path.UICodePath;

            string className = _builder.name;
            string viewPath = Path.Combine(codePath, "UiView", $"{className}");
            string logicPath = Path.Combine(codePath, "UiViewLogic", $"{className}");

            if (!Directory.Exists(viewPath))
                Directory.CreateDirectory(viewPath);
            if (!Directory.Exists(logicPath))
                Directory.CreateDirectory(logicPath);

            // 修复：使用同步列表存储其他组件信息，避免 Dictionary 遍历顺序不确定导致的字段绑定错位
            var buttonNames = new List<string>(); // Button 的真实物体名 (RealName)
            var buttonScriptNames = new List<string>(); // Button 的字段名 (ScriptName)
            var buttonProNames = new List<string>();
            var buttonProScriptNames = new List<string>();
            var otherRealNames = new List<string>(); // 其他组件的真实物体名
            var otherScriptNames = new List<string>(); // 其他组件的字段名
            var otherTypeNames = new List<string>(); // 其他组件的类型名 (如 "Text", "Image")

            foreach (var bindingData in _builder.BindDatas)
            {
                if (bindingData.BindCom == null || bindingData.Disabled) continue;
                Type type = bindingData.BindCom.GetType();
                if (type == typeof(ButtonPro))
                {
                    buttonProNames.Add(bindingData.RealName);
                    buttonProScriptNames.Add(bindingData.ScriptName);
                }
                else if (type == typeof(UnityEngine.UI.Button))
                {
                    buttonNames.Add(bindingData.RealName);
                    buttonScriptNames.Add(bindingData.ScriptName);
                }
                else
                {
                    otherRealNames.Add(bindingData.RealName);
                    otherScriptNames.Add(bindingData.ScriptName);
                    otherTypeNames.Add(type.Name);
                }
            }

            viewPath = Path.Combine(viewPath, $"{className}.cs");
            logicPath = Path.Combine(logicPath, $"{className}Logic.cs");

            List<string> usingNamespaces = new List<string>();

            string[] baseNamespaces =
            {
                "UnityEngine",
                "UnityEngine.UI",
                "System.Collections.Generic",
                "Cysharp.Threading.Tasks",
                "EasyFramework",
                "EasyFramework.Managers.Ui"
            };
            foreach (string ns in baseNamespaces)
            {
                if (!usingNamespaces.Contains(ns))
                    usingNamespaces.Add(ns);
            }

            foreach (var bindingData in _builder.BindDatas)
            {
                if (bindingData.BindCom == null || bindingData.Disabled) continue;
                Type type = bindingData.BindCom.GetType();
                string ns = type.Namespace;
                if (!string.IsNullOrEmpty(ns) && !usingNamespaces.Contains(ns))
                    usingNamespaces.Add(ns);
            }

            bool hasTMPro = false;
            foreach (var bindingData in _builder.BindDatas)
            {
                if (bindingData.BindCom == null || bindingData.Disabled) continue;
                if (bindingData.BindCom.GetType().Name == "TextMeshProUGUI")
                {
                    hasTMPro = true;
                    break;
                }
            }

            if (hasTMPro && !usingNamespaces.Contains("TMPro"))
                usingNamespaces.Add("TMPro");

            StringBuilder commonSb = new StringBuilder();
            commonSb.AppendLine(EditorInfoToolkit.GetFileHead(_builder.Describe));
            commonSb.AppendLine();
            foreach (string ns in usingNamespaces)
            {
                commonSb.AppendLine($"using {ns};");
            }

            commonSb.AppendLine();
            string commonNamespace =
                !string.IsNullOrEmpty(_builder.Namespace) ? _builder.Namespace : EFC.Projects.ScriptNamespace;
            commonSb.AppendLine($"namespace {commonNamespace}");
            commonSb.AppendLine("{");

            // ========== 生成 View 文件 ==========
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ScriptExplain);
            sb.AppendLine($"    public partial class {className} : IUiView");
            sb.AppendLine("    {");
            sb.AppendLine("        uint IUiView.SerialId { get; set; }");
            sb.AppendLine("        public UiBinding Binding { get; private set; }");
            sb.AppendLine("        public RectTransform View { get; private set; }");
            sb.AppendLine();
            for (int i = 0; i < otherScriptNames.Count; i++)
                sb.AppendLine($"        private {otherTypeNames[i]} {otherScriptNames[i]};");
            sb.AppendLine();
            foreach (var scriptName in buttonScriptNames)
                sb.AppendLine($"        private Button {scriptName};");
            sb.AppendLine();
            foreach (var scriptName in buttonProScriptNames)
                sb.AppendLine($"        private ButtonPro {scriptName};");
            sb.AppendLine();
            sb.AppendLine("        void IUiView.Bind(RectTransform uiViewRect, UiBinding binding)");
            sb.AppendLine("        {");
            sb.AppendLine("            View = uiViewRect;");
            sb.AppendLine("            Binding = binding;");

            for (int i = 0; i < otherScriptNames.Count; i++)
                sb.AppendLine(
                    $"            {otherScriptNames[i]} = Binding.Resolve<{otherTypeNames[i]}>(nameof({otherScriptNames[i]}));");

            for (int i = 0; i < buttonScriptNames.Count; i++)
            {
                sb.AppendLine(
                    $"            {buttonScriptNames[i]} = Binding.Resolve<Button>(nameof({buttonScriptNames[i]}));");
                sb.AppendLine(
                    $"            {buttonScriptNames[i]}.onClick.AddListener(OnClick{buttonScriptNames[i]});");
            }

            for (int i = 0; i < buttonProScriptNames.Count; i++)
            {
                sb.AppendLine(
                    $"            {buttonProScriptNames[i]} = Binding.Resolve<ButtonPro>(nameof({buttonProScriptNames[i]}));");
                sb.AppendLine(
                    $"            {buttonProScriptNames[i]}.AddClickListener(OnClick{buttonProScriptNames[i]});");
            }

            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        void IUiView.Dispose()");
            sb.AppendLine("        {");
            foreach (var scriptName in buttonScriptNames)
            {
                sb.AppendLine($"            {scriptName}?.onClick.RemoveListener(OnClick{scriptName});");
                sb.AppendLine($"            {scriptName} = null;");
            }

            foreach (var scriptName in buttonProScriptNames)
            {
                sb.AppendLine($"            {scriptName}?.RemoveClickListener(OnClick{scriptName});");
                sb.AppendLine($"            {scriptName} = null;");
            }

            foreach (var scriptName in otherScriptNames)
            {
                sb.AppendLine($"            {scriptName} = null;");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine(ScriptExplain);
            sb.AppendLine("}");
            File.WriteAllText(viewPath, commonSb + sb.ToString(), Encoding.UTF8);

            // ========== 生成/更新 Logic 文件 ==========
            sb.Clear();
            if (!File.Exists(logicPath))
            {
                // 首次创建 Logic 文件
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {_builder.Describe}");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public partial class {className}");
                sb.AppendLine("    {");
                sb.AppendLine("        void IUiView.Awake()");
                sb.AppendLine("        {");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        void IUiView.Quit()");
                sb.AppendLine("        {");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        " + ButtonEventsStart);
                foreach (var scriptName in buttonProScriptNames)
                {
                    sb.AppendLine();
                    sb.AppendLine($"        private void OnClick{scriptName}()");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            D.Log(\"OnClick:  {scriptName}\");");
                    sb.AppendLine("        }");
                }

                foreach (var scriptName in buttonScriptNames)
                {
                    sb.AppendLine();
                    sb.AppendLine($"        private void OnClick{scriptName}()");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            D.Log(\"OnClick:  {scriptName}\");");
                    sb.AppendLine("        }");
                }

                sb.AppendLine();
                sb.AppendLine("        " + ButtonEventsEnd);
                sb.AppendLine("    }");
                sb.AppendLine("}");
                File.WriteAllText(logicPath, commonSb + sb.ToString(), Encoding.UTF8);
            }
            else
            {
                List<string> logicList = new List<string>(File.ReadAllLines(logicPath));
                int endIndex = -1;
                for (int i = logicList.Count - 1; i >= 0; i--)
                {
                    if (!logicList[i].Contains(ButtonEventsEnd)) continue;
                    endIndex = i;
                    break;
                }

                if (endIndex != -1)
                {
                    // 合并所有按钮的字段名（用于去重）
                    var allButtonScriptNames = new List<string>();
                    allButtonScriptNames.AddRange(buttonProScriptNames);
                    allButtonScriptNames.AddRange(buttonScriptNames);
                    foreach (var btnScript in allButtonScriptNames)
                    {
                        bool alreadyExists = false;
                        foreach (var line in logicList)
                        {
                            if (!line.Contains($"private void OnClick{btnScript}()"))
                                continue;

                            alreadyExists = true;
                            break;
                        }

                        if (alreadyExists) continue;

                        string newEvent =
                            $"        private void OnClick{btnScript}()\n        {{\n            D.Log(\"OnClick:  {btnScript}\");\n        }}\n";
                        logicList.Insert(endIndex, newEvent);
                        endIndex++; // 插入后索引后移
                    }
                }

                ChangeFileHead(logicList);
                File.WriteAllLines(logicPath, logicList);
            }
        }

        private void ChangeFileHead(List<string> strList)
        {
            for (int i = 0; i < strList.Count; i++)
            {
                if (strList[i].Contains(StrChangeAuthor))
                {
                    strList[i] = $" {StrChangeAuthor}    {EditorInfoToolkit.GetAuthorName()}";
                    continue;
                }

                if (!strList[i].Contains(StrChangeTime)) continue;
                strList[i] = $" {StrChangeTime}      {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                return;
            }
        }

        #endregion
    }
}