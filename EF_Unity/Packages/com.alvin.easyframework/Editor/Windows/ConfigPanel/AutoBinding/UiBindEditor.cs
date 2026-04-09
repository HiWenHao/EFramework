/*
 * ================================================
 * Describe:      This script is used to builder with editor.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-13 16:46:15
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 16:47:08
 * ScriptVersion: 0.1
 * ===============================================
 */

using EasyFramework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EasyFramework.Manager.UI;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// Ui builder with editor.
    /// </summary>
    [CustomEditor(typeof(UiBinding))]
    public class UiBuilderEditor : Editor
    {
        private UiBinding _builder;

        private AutoBindingConfig _setting;

        private List<string> _tempFiledNames;
        private List<string> _tempComponentTypeNames;
        private Dictionary<string, int> _componentsName;

        private void OnEnable()
        {
            _builder = (UiBinding)target;
            _setting = EditorUtils.LoadSettingAtPath<AutoBindingConfig>();

            _builder.Namespace = string.IsNullOrEmpty(_builder.Namespace) ? _setting.Namespace : _builder.Namespace;
            if (_builder.CreatePrefab)
                _builder.PrefabPath = string.IsNullOrEmpty(_builder.PrefabPath)
                    ? ProjectUtility.Path.UIPrefabPath
                    : _builder.PrefabPath;
            _builder.ScriptPath = string.IsNullOrEmpty(_builder.ScriptPath)
                ? ProjectUtility.Path.UICodePath
                : _builder.ScriptPath;

            _tempFiledNames = new List<string>();
            _tempComponentTypeNames = new List<string>();
            _componentsName = new Dictionary<string, int>();

            _builder.SortByType = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "UiBindSortType", 1) ==
                                  1;
            _builder.SortByNameLength =
                EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "UiBindSortName", 1) == 1;
            _builder.PackUpBindList = true;
            AutoBindComponent();
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
            serializedObject.Update();

            DrawSetting();

            DrawAutoBind();

            DrawKvData();

            DrawStartBind();

            if (!target || !serializedObject.targetObject)
                return;
            serializedObject.ApplyModifiedProperties();
        }

        #region Draw. 绘制编辑器内容

        /// <summary>
        /// Draw setting
        /// 绘制设置项
        /// </summary>
        private void DrawSetting()
        {
            EditorGUILayout.BeginHorizontal();
            _builder.Namespace =
                EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Script, Lc.Namespace }), _builder.Namespace);
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Default, Lc.Settings })))
            {
                _builder.Namespace = _setting.Namespace;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Script, Lc.Class, Lc.Name }), _builder.gameObject.name);
            EditorGUI.EndDisabledGroup();
            _builder.Describe = EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Script, Lc.Description }), _builder.Describe);


            EditorGUILayout.Space(6f, true);
            _builder.AutoDestroy = EditorGUILayout.Toggle(LC.Combine(new []{Lc.Auto, Lc.Destroy}), _builder.AutoDestroy);
            _builder.ViewType = (UIViewType)EditorGUILayout.EnumPopup("UI" + LC.Combine(Lc.Type), _builder.ViewType);
            if (_builder.ViewType is UIViewType.Cache or UIViewType.Popup or UIViewType.Tips)
                _builder.ViewType = UIViewType.Page;

            EditorGUILayout.Space(12f, true);

            _builder.CreatePrefab = GUILayout.Toggle(_builder.CreatePrefab,
                    _builder.CreatePrefab
                        ? LC.Combine(new Lc[] { Lc.Prefab, Lc.Save, Lc.Path })
                        : LC.Combine(new Lc[] { Lc.Create, Lc.Prefab }))
                ;
            if (_builder.CreatePrefab)
            {
                EditorGUILayout.LabelField(_builder.PrefabPath);
                // EditorGUILayout.BeginHorizontal();
                // if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Select, Lc.Path })))
                // {
                //     string folder = Path.Combine(Application.dataPath, _builder.PrefabPath);
                //     if (!Directory.Exists(folder))
                //     {
                //         folder = Application.dataPath;
                //     }
                //
                //     string path =
                //         EditorUtility.OpenFolderPanel(LC.Combine(new Lc[] { Lc.Select, Lc.Path }), folder, "");
                //     if (!string.IsNullOrEmpty(path))
                //     {
                //         _builder.PrefabPath = path.Replace(Application.dataPath + "/", "Assets/") + "/";
                //     }
                // }
                //
                // if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Default, Lc.Settings })))
                // {
                //     _builder.PrefabPath = ProjectUtility.Path.UIPrefabPath;
                // }
                //
                // EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12f, true);

            EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Code, Lc.Save, Lc.Path }));
            EditorGUILayout.LabelField(_builder.ScriptPath);
            // EditorGUILayout.BeginHorizontal();
            // if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Select, Lc.Path })))
            // {
            //     string folder = Application.dataPath + "/" + _builder.ScriptPath;
            //     if (!Directory.Exists(folder))
            //     {
            //         folder = Application.dataPath;
            //     }
            //
            //     string path = EditorUtility.OpenFolderPanel(LC.Combine(new Lc[] { Lc.Select, Lc.Path }), folder, "");
            //     if (!string.IsNullOrEmpty(path))
            //     {
            //         _builder.ScriptPath = path.Replace(Application.dataPath + "/", "Assets/") + "/";
            //     }
            // }
            //
            // if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Default, Lc.Settings })))
            // {
            //     _builder.ScriptPath = ProjectUtility.Path.UICodePath;
            // }
            //
            // EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw auto binding
        /// 绘制绑定按钮
        /// </summary>
        private void DrawAutoBind()
        {
            GUILayout.Space(12.0f);
            GUILayout.BeginHorizontal();
            _builder.SortByType =
                GUILayout.Toggle(_builder.SortByType, LC.Combine(new Lc[] { Lc.By, Lc.Type, Lc.Sort }));
            _builder.SortByNameLength = GUILayout.Toggle(_builder.SortByNameLength,
                LC.Combine(new Lc[] { Lc.By, Lc.Name, Lc.Length, Lc.Sort }));
            GUILayout.EndHorizontal();
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Auto, Lc.Bind, Lc.Component })))
            {
                AutoBindComponent();
            }
        }

        /// <summary>
        /// Draw key and value data
        /// 绘制键值对数据
        /// </summary>
        private void DrawKvData()
        {
            _builder.PackUpBindList = EditorGUILayout.BeginFoldoutHeaderGroup(_builder.PackUpBindList,
                _builder.PackUpBindList
                    ? LC.Combine(new Lc[] { Lc.Close, Lc.List })
                    : LC.Combine(new Lc[] { Lc.Open, Lc.List }));
            if (!_builder.PackUpBindList)
                return;

            int needDeleteIndex = -1;
            EditorGUILayout.BeginVertical();

            for (int i = 0; i < _builder.BindDatas.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(25));
                EditorGUILayout.PrefixLabel(_builder.BindDatas[i].ScriptName);
                _builder.BindDatas[i].BindCom =
                    (Component)EditorGUILayout.ObjectField(_builder.BindDatas[i].BindCom, typeof(Component), true);

                if (GUILayout.Button("X"))
                {
                    //将元素下标添加进删除list
                    needDeleteIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            //删除data
            if (needDeleteIndex != -1)
            {
                _builder.BindDatas.RemoveAt(needDeleteIndex);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw start bind button
        /// 绘制开始绑定按钮
        /// </summary>
        private void DrawStartBind()
        {
            GUILayout.Space(24f);
            _builder.DeleteScript =
                GUILayout.Toggle(_builder.DeleteScript, LC.Combine(new Lc[] { Lc.Unload, Lc.Script }));

            GUILayout.Space(12f);
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Bind, Lc.Create }), GUILayout.Height(25.0f)))
            {
                GenAutoBindCode();
                if (_builder.CreatePrefab)
                {
                    CreateOrModifyPrefab();
                }

                if (_builder.DeleteScript)
                {
                    DestroyImmediate(_builder);
                }

                AssetDatabase.Refresh();
            }
        }

        #endregion

        #region Bind. 绑定数据内容

        /// <summary>
        /// Auto bind component.
        /// 自动绑定组件
        /// </summary>
        private void AutoBindComponent()
        {
            if (Application.isPlaying)
                return;
            
            _builder.BindDatas.Clear();
            _componentsName.Clear();
            Transform[] children = _builder.gameObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                _tempFiledNames.Clear();
                _tempComponentTypeNames.Clear();

                if (child == _builder.transform)
                    continue;

                UiBinding autoSelf = child.gameObject.GetComponent<UiBinding>();
                UiBinding autoParent = child.gameObject.GetComponentInParent<UiBinding>(true);

                if (autoSelf == null && autoParent != null && autoParent != _builder)
                    continue;

                if (!IsValidBind(child, _tempFiledNames, _tempComponentTypeNames))
                    continue;

                for (int i = 0; i < _tempFiledNames.Count; i++)
                {
                    Component com = child.GetComponent(_tempComponentTypeNames[i]);
                    if (com == null)
                    {
                        D.Error($"{child.name}上不存在{_tempComponentTypeNames[i]}的组件");
                    }
                    else
                    {
                        AddBindData(child.name, _tempFiledNames[i], child.GetComponent(_tempComponentTypeNames[i]));
                    }
                }
            }
        }

        /// <summary>
        /// Add bind data.
        /// 添加绑定数据
        /// </summary>
        /// <param name="rectName">物体对象名</param>
        /// <param name="filedName">生成的字段名</param>
        /// <param name="needBindingComponent">需要绑定的组件</param>
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
                    nameList.Clear();
                    return;
                }

                nameList.Add(elementName);
            }

            List<string> comNameList = new List<string>();
            string componentsName = needBindingComponent.GetType().Name;
            if (_builder.SortByType && _builder.SortByNameLength)
            {
                if (!_componentsName.TryAdd(componentsName, bindingCount))
                {
                    foreach (var item in this._componentsName.Keys)
                        comNameList.Add(item);

                    int indexOf = comNameList.IndexOf(componentsName) + 1;
                    for (int i = indexOf; i < comNameList.Count; i++)
                        ++_componentsName[comNameList[i]];

                    int endIndex = indexOf >= comNameList.Count
                        ? _builder.BindDatas.Count
                        : _componentsName[comNameList[indexOf]] - 1;
                    bindingCount = EditorUtils.GetIndexWithLengthSort(filedName.Length, nameList,
                        this._componentsName[comNameList[indexOf - 1]], endIndex);
                    comNameList.Clear();
                }
            }
            else if (_builder.SortByType && !_builder.SortByNameLength)
            {
                if (!_componentsName.TryAdd(componentsName, bindingCount))
                {
                    bindingCount = ++_componentsName[componentsName];
                    foreach (var item in _componentsName.Keys)
                        comNameList.Add(item);
                    for (int i = comNameList.IndexOf(componentsName) + 1; i < comNameList.Count; i++)
                        ++_componentsName[comNameList[i]];
                    comNameList.Clear();
                }
            }
            else if (!_builder.SortByType && _builder.SortByNameLength)
                bindingCount = EditorUtils.GetIndexWithLengthSort(filedName.Length, nameList, 0, nameList.Count);
            else
                bindingCount = nameList.Count;

            _builder.BindDatas.Insert(bindingCount, new UiBinding.BindData()
            {
                BindCom = needBindingComponent,
                RealName = rectName,
                ScriptName = filedName,
            });
            nameList.Clear();
        }

        /// <summary>
        /// 查找是否为有效绑定
        /// </summary>
        /// <param name="trans">目标</param>
        /// <param name="filedNames">对象名</param>
        /// <param name="componentTypeNames">对象组件</param>
        /// <returns>是否有效</returns>
        private bool IsValidBind(Transform trans, List<string> filedNames, List<string> componentTypeNames)
        {
            string[] strArray = trans.name.Split('_');

            if (strArray.Length == 1)
                return false;

            bool isFind = false;
            string filedName = strArray[^1];
            filedName = EditorUtils.RemovePunctuation(filedName).Trim();

            for (int i = 0; i < strArray.Length - 1; i++)
            {
                string prefixes = strArray[i];
                foreach (RulePrefixes autoBindRulePrefix in _setting.RulePrefixes)
                {
                    if (!autoBindRulePrefix.Prefixe.Equals(prefixes))
                        continue;

                    var comName = autoBindRulePrefix.FullName;
                    filedNames.Add($"{prefixes}_{filedName}");
                    componentTypeNames.Add(comName);
                    isFind = true;
                    break;
                }

                if (!isFind)
                {
                    D.Warning($"{trans.name}的命名中{prefixes}不存在对应的组件类型，绑定失败");
                }
            }

            return isFind;
        }

        #endregion

        #region Create prefab file. 生成预制件文件

        private void CreateOrModifyPrefab()
        {
            string path = Application.dataPath.Equals(_builder.PrefabPath)
                ? $"{_builder.PrefabPath}/{_builder.name}.prefab"
                : $"{Application.dataPath}/{_builder.PrefabPath}/{_builder.name}.prefab";
            if (path.Contains("Assets/Assets"))
                path = path.Replace("Assets/Assets", "Assets");

            if (File.Exists(path))
            {
                File.Delete(path);
                File.Delete(path + ".meta");
            }

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(_builder.gameObject, path);
            DestroyImmediate(prefabAsset.GetComponent<UiBinding>(), true);

            AssetDatabase.SaveAssets();
        }

        #endregion

        #region Create code file. 生成代码文件

        private const string StrChangeTime = "* ModifyTime:";
        private const string StrChangeAuthor = "* ModifyAuthor:";
        private const string ButtonEventsStart = "#region Button invoke event. Do not change here.不要更改这行 -- Auto";
        private const string ButtonEventsEnd = "#endregion button invoke event. Do not change here.不要更改这行 -- Auto";
        private const string ScriptExplain = "    //-----The script is auto generated. Please do not make any changes-----";

        /// <summary>
        /// Gen auto bind code on the basis of special scriptName.
        /// 基于特殊名称生成自动绑定代码
        /// </summary>
        private void GenAutoBindCode()
        {
            string codePath = !string.IsNullOrEmpty(_builder.ScriptPath)
                ? _builder.ScriptPath
                : ProjectUtility.Path.UICodePath;

            string className = _builder.name;
            string viewPath = Path.Combine(codePath, "UiView", $"{className}");
            string logicPath = Path.Combine(codePath, "UiViewLogic", $"{className}");
            
            if (!Directory.Exists(viewPath))
                Directory.CreateDirectory(viewPath);
            if (!Directory.Exists(logicPath))
                Directory.CreateDirectory(logicPath);

            
            var buttonLst = new List<string>();
            var buttonProLst = new List<string>();
            var otherNameLst = new List<string>();
            var buttonNameLst = new List<string>();
            var buttonProNameLst = new List<string>();
            var otherComponent = new Dictionary<string, string>();
            
            foreach (var bindingData in _builder.BindDatas)
            {
                Type type = bindingData.BindCom.GetType();

                string ns = type.Namespace;

                if (type == typeof(ButtonPro))
                {
                    buttonProNameLst.Add(bindingData.RealName);
                    buttonProLst.Add(bindingData.ScriptName);
                }
                else if (type == typeof(UnityEngine.UI.Button))
                {
                    buttonNameLst.Add(bindingData.RealName);
                    buttonLst.Add(bindingData.ScriptName);
                }
                else
                {
                    otherNameLst.Add(bindingData.RealName);
                    otherComponent.Add(bindingData.ScriptName, bindingData.BindCom.GetType().Name);
                }
            }

            viewPath = Path.Combine(viewPath, $"{className}.cs");
            logicPath = Path.Combine(logicPath, $"{className}Logic.cs");

            #region Common start
            
            StringBuilder commonSb = new StringBuilder();
            commonSb.AppendLine(GetFileHead());
            
            commonSb.AppendLine();
            commonSb.AppendLine("using EasyFramework;");
            commonSb.AppendLine("using EasyFramework.UI;");
            commonSb.AppendLine("using System.Collections.Generic;");
            commonSb.AppendLine("using EasyFramework.Manager.UI;");
            commonSb.AppendLine("using UnityEngine;");
            commonSb.AppendLine("using UnityEngine.UI;");
            
            commonSb.AppendLine();
            
            string commonNamespace = !string.IsNullOrEmpty(_builder.Namespace) ? _builder.Namespace : _setting.Namespace;
            commonSb.AppendLine($"namespace {commonNamespace}");
            commonSb.AppendLine("{");
            
            #endregion
            
            int btnIndex = -1;
            StringBuilder sb = new StringBuilder();

            #region view script

            string autoDestroy = _builder.AutoDestroy ? "true" : "false";
            sb.AppendLine(ScriptExplain);
            sb.AppendLine($"    public partial class {className} : IUiView");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        public static {className} Open(params object[] args)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return EF.Uii.OpenPageView<{className}>(args);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        public static bool Close(params object[] args)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return EF.Uii.CloseView<{className}>(args);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        bool IUiView.AutoDestroy => {autoDestroy};");
            sb.AppendLine($"        uint IUiView.SerialId {{ get; set; }}");
            sb.AppendLine($"        public UIViewType ViewType => UIViewType.{_builder.ViewType};");
            sb.AppendLine($"        public RectTransform View {{ get; private set; }}");
            sb.AppendLine();
            foreach (var item in otherComponent)
            {
                sb.AppendLine($"        private {item.Value} {item.Key};");
            }
            sb.AppendLine($"        private List<Button> m_AllButtons;");
            sb.AppendLine($"        private List<ButtonPro> m_AllButtonPros;");
            sb.AppendLine();
            sb.AppendLine($"        void IUiView.Bind(RectTransform uiViewRect)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            View = uiViewRect;");
            
            foreach (var item in otherComponent)
                sb.AppendLine(
                    $"            {item.Key} = EF.Tool.Find<{item.Value}>(uiViewRect.transform, \"{otherNameLst[++btnIndex]}\");");

            btnIndex = -1;
            foreach (var btn in buttonLst)
            {
                sb.AppendLine(
                    $"            EF.Tool.Find<Button>(uiViewRect.transform, \"{buttonNameLst[++btnIndex]}\").RegisterInListAndBindEvent(OnClick{btn}, ref m_AllButtons);");
            }

            btnIndex = -1;
            foreach (var btnPro in buttonProLst)
            {
                sb.AppendLine(
                    $"            EF.Tool.Find<ButtonPro>(uiViewRect.transform, \"{buttonProNameLst[++btnIndex]}\").RegisterInListAndBindEvent(OnClick{btnPro}, ref m_AllButtonPros);");
            }
            
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        void IUiView.Dispose()");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            m_AllButtons.ReleaseAndRemoveEvent();");
            sb.AppendLine($"            m_AllButtons = null;");
            sb.AppendLine($"            m_AllButtonPros.ReleaseAndRemoveEvent();");
            sb.AppendLine($"            m_AllButtonPros = null;");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine(ScriptExplain);
            sb.AppendLine($"}}");

            File.WriteAllText(viewPath, commonSb + sb.ToString(), Encoding.UTF8);

            #endregion

            sb.Clear();
            #region Logic script
            if (!File.Exists(logicPath))
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// {_builder.Describe}");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    public partial class {className}");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        void IUiView.Awake()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                sb.AppendLine($"        void IUiView.Quit()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                sb.AppendLine($"        " + ButtonEventsStart);
                foreach (var bindData in _builder.BindDatas)
                {
                    Type type = bindData.BindCom.GetType();
                    if (type != typeof(ButtonPro) && type != typeof(UnityEngine.UI.Button))
                        continue;
                    
                    sb.AppendLine();
                    sb.AppendLine($"        private void OnClick{bindData.ScriptName}()");
                    sb.AppendLine($"        {{");
                    sb.AppendLine($"            D.Log(\"OnClick:  {bindData.RealName}\");");
                    sb.AppendLine($"        }}");
                }
                sb.AppendLine();
                sb.AppendLine("        " + ButtonEventsEnd);
                sb.AppendLine($"    }}");
                
                sb.AppendLine($"}}");
                File.WriteAllText(logicPath, commonSb + sb.ToString(), Encoding.UTF8);
            }
            else
            {
                List<string> logicList = new List<string>();
                StringBuilder buttonFunction = new StringBuilder();
                
                logicList.AddRange(File.ReadAllLines(logicPath));
                #region If have button component, than need write new event.

                int endIndex = 0;
                for (int i = logicList.Count - 1; i >= 0; i--)
                {
                    if (!logicList[i].Contains(ButtonEventsEnd)) 
                        continue;
                    
                    endIndex = i;
                    break;
                }

                for (int i = buttonLst.Count - 1; i >= 0; i--)
                {
                    buttonProLst.Insert(0, buttonLst[i]);
                }

                foreach (var btnPro in buttonProLst)
                {
                    bool contain = false;
                    foreach (var str in logicList)
                    {
                        if (!str.Contains($"private void OnClick{btnPro}()")) 
                            continue;
                        
                        contain = true;
                        break;
                    }

                    if (contain) continue;

                    buttonFunction.AppendLine($"        private void OnClick{btnPro}()");
                    buttonFunction.AppendLine($"        {{");
                    buttonFunction.AppendLine($"            D.Log(\"OnClick:  {btnPro}\");");
                    buttonFunction.AppendLine($"        }}");
                    //buttonFunction.AppendLine("");
                    
                    logicList.Insert(endIndex, buttonFunction.ToString());
                    buttonFunction.Clear();
                    endIndex++;
                }
                
                #endregion

                ChangeFileHead(logicList);
                File.WriteAllLines(logicPath, logicList);
                
                logicList.Clear();
                buttonFunction.Clear();
            }
            #endregion

            sb.Clear();
            commonSb.Clear();
            buttonLst.Clear();
            otherNameLst.Clear();
            buttonProLst.Clear();
            buttonNameLst.Clear();
            otherComponent.Clear();
            buttonProNameLst.Clear();
        }

        /// <summary>
        /// Amend head with changed the file.
        /// 更改文件头内容
        /// </summary>
        private void ChangeFileHead(List<string> strList)
        {
            for (int i = 0; i < strList.Count; i++)
            {
                if (strList[i].Contains(StrChangeAuthor))
                {
                    strList[i] = $" {StrChangeAuthor}  {GetAuthorName()}";
                    continue;
                }

                if (!strList[i].Contains(StrChangeTime))
                    continue;
                
                strList[i] = $" {StrChangeTime}    {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                return;
            }
        }

        /// <summary>
        /// Get file head.
        /// 获取文件头内容
        /// </summary>
        private string GetFileHead()
        {
            string authorName = GetAuthorName();
            string createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            string annotationStr =
                "/*\n"
                + " * ================================================\r\n"
                + $" * Describe:      {_builder.Describe}.\r\n"
                + $" * Author:        {authorName}\r\n"
                + $" * CreationTime:  {createTime}\r\n"
                + $" * ModifyAuthor:  {authorName}\r\n"
                + $" * ModifyTime:    {createTime}\r\n"
                + $" * ScriptVersion: {ProjectUtility.Project.ScriptVersion} \r\n"
                + " * ================================================\r\n"
                + " */";
            return annotationStr;
        }

        private string GetAuthorName()
        {
            string configName = ProjectUtility.Project.ScriptAuthor;
            string authorName = EditorPrefs.GetString($"{ProjectUtility.Project.AppConst.AppPrefix}EditorUser");
            return string.IsNullOrEmpty(configName) || configName.Equals("Default") ? authorName : configName;
        }

        #endregion
    }
}