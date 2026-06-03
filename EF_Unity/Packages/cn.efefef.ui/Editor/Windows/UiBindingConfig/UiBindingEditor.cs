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
        private UiBinding _builder;
        private UiBindingConfig _setting;
        private Vector2 _bindListScrollPos;
        private List<string> _tempFiledNames;
        private List<string> _tempComponentTypeNames;
        private Dictionary<string, int> _componentsName;

        private void OnEnable()
        {
            _builder = (UiBinding)target;
            _setting = EditorUtils.LoadSettingAtPath<UiBindingConfig>();

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

            DrawSetting();

            DrawAutoBind();

            DrawKvData();

            DrawStartBind();

            if (!EditorGUI.EndChangeCheck() || !target || !serializedObject.targetObject)
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
                _bindListScrollPos = EditorGUILayout.BeginScrollView(_bindListScrollPos, GUILayout.MaxHeight(300));

                int needDeleteIndex = -1;
                for (int i = 0; i < _builder.BindDatas.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(25));
                    EditorGUILayout.PrefixLabel(_builder.BindDatas[i].ScriptName);
                    _builder.BindDatas[i].BindCom =
                        (Component)EditorGUILayout.ObjectField(_builder.BindDatas[i].BindCom, typeof(Component), true);
                    if (GUILayout.Button("X")) needDeleteIndex = i;
                    EditorGUILayout.EndHorizontal();
                }

                if (needDeleteIndex != -1) _builder.BindDatas.RemoveAt(needDeleteIndex);

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawStartBind()
        {
            EditorGUILayout.Space(24f);
            _builder.DeleteScript = EditorGUILayout.Toggle(LC.Combine(Lc.Unload, Lc.This, Lc.Script),
                _builder.DeleteScript);
            EditorGUILayout.Space(12f);

            var originalColor = GUI.contentColor;
            GUI.contentColor = GUIUtils.LightGreen;
            bool createClicked = GUILayout.Button(LC.Combine(Lc.Bind, Lc.Create), GUILayout.Height(25.0f));
            GUI.contentColor = originalColor;

            if (createClicked)
            {
                GenAutoBindCode();
                if (_builder.CreatePrefab) CreateOrModifyPrefab();
                if (_builder.DeleteScript) DestroyImmediate(_builder);
                EditorApplication.delayCall += AssetDatabase.Refresh;
            }
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
                Type type = bindingData.BindCom.GetType();
                string ns = type.Namespace;
                if (!string.IsNullOrEmpty(ns) && !usingNamespaces.Contains(ns))
                    usingNamespaces.Add(ns);
            }

            bool hasTMPro = false;
            foreach (var bindingData in _builder.BindDatas)
            {
                if (bindingData.BindCom.GetType().Name == "TextMeshProUGUI")
                {
                    hasTMPro = true;
                    break;
                }
            }

            if (hasTMPro && !usingNamespaces.Contains("TMPro"))
                usingNamespaces.Add("TMPro");

            StringBuilder commonSb = new StringBuilder();
            commonSb.AppendLine(EditorToolkit.GetFileHead(_builder.Describe));
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
            string autoDestroy = _builder.AutoDestroy ? "true" : "false";
            sb.AppendLine(ScriptExplain);
            sb.AppendLine($"    public partial class {className} : IUiView");
            sb.AppendLine("    {");
            sb.AppendLine($"        public static async UniTask<{className}> Open(params object[] args)");
            sb.AppendLine("        {");
            sb.AppendLine($"            return await UiSystem.Instance.OpenPageView<{className}>(args);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static async UniTask<bool> Close(params object[] args)");
            sb.AppendLine("        {");
            sb.AppendLine($"            return await UiSystem.Instance.CloseView<{className}>(args);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        bool IUiView.AutoDestroy => {autoDestroy};");
            sb.AppendLine($"        float IUiView.AutoDestroyCountdown => {_builder.AutoDestroyCountdown}f;");
            sb.AppendLine("        uint IUiView.SerialId { get; set; }");
            sb.AppendLine($"        public UIViewType ViewType => UIViewType.{_builder.ViewType};");
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
            sb.AppendLine("        void IUiView.Bind(RectTransform uiViewRect)");
            sb.AppendLine("        {");
            sb.AppendLine("            View = uiViewRect;");

            for (int i = 0; i < otherScriptNames.Count; i++)
                sb.AppendLine(
                    $"            {otherScriptNames[i]} = EF.Tool.Find<{otherTypeNames[i]}>(uiViewRect.transform, \"{otherRealNames[i]}\");");

            for (int i = 0; i < buttonScriptNames.Count; i++)
            {
                sb.AppendLine(
                    $"            {buttonScriptNames[i]} = EF.Tool.Find<Button>(uiViewRect.transform, \"{buttonNames[i]}\");");
                sb.AppendLine(
                    $"            {buttonScriptNames[i]}.onClick.AddListener(OnClick{buttonScriptNames[i]});");
            }

            for (int i = 0; i < buttonProScriptNames.Count; i++)
            {
                sb.AppendLine(
                    $"            {buttonProScriptNames[i]} = EF.Tool.Find<ButtonPro>(uiViewRect.transform, \"{buttonProNames[i]}\");");
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
                    strList[i] = $" {StrChangeAuthor}    {EditorToolkit.GetAuthorName()}";
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