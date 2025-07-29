/* 
 * ================================================
 * Describe:      This script is used to builder with editor. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-13 16:46:15
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-04-26 16:11:44
 * ScriptVersion: 0.2
 * ===============================================
*/
using EasyFramework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.AutoBind
{
    /// <summary>
    /// Ui builder with editor.
    /// </summary>
    [CustomEditor(typeof(UiBind))]
    public class UiBuilderEditor : Editor
    {
        UiBind m_Builder;

        private SerializedProperty _bindDatas;
        private SerializedProperty _bindComs;
        private SerializedProperty _sortByType;
        private SerializedProperty _sortByNameLength;
        private SerializedProperty _namespace;
        private SerializedProperty _comCodePath;
        private SerializedProperty _prefabPath;
        private SerializedProperty _createPrefab;
        private SerializedProperty _deleteScript;
        private SerializedProperty _packUpBindList;

        private AutoBindSetting _setting;

        private bool _sortByTypes;
        private bool _sortByNamesLength;

        private List<string> _tempFiledNames;
        private List<string> _tempComponentTypeNames;
        private Dictionary<string, int> _componentsName;

        private void OnEnable()
        {
            m_Builder = (UiBind)target;
            _setting = EditorUtils.LoadSettingAtPath<AutoBindSetting>();
            _bindComs = serializedObject.FindProperty("BindComs");
            _bindDatas = serializedObject.FindProperty("BindDatas");
            _namespace = serializedObject.FindProperty("_namespace");
            _sortByType = serializedObject.FindProperty("_sortByType");
            _prefabPath = serializedObject.FindProperty("_prefabPath");
            _comCodePath = serializedObject.FindProperty("_comCodePath");
            _createPrefab = serializedObject.FindProperty("_createPrefab");
            _deleteScript = serializedObject.FindProperty("_deleteScript");
            _packUpBindList = serializedObject.FindProperty("_packUpBindList");
            _sortByNameLength = serializedObject.FindProperty("_sortByNameLength");

            _namespace.stringValue = string.IsNullOrEmpty(_namespace.stringValue) ? _setting.Namespace : _namespace.stringValue;
            _prefabPath.stringValue = string.IsNullOrEmpty(_prefabPath.stringValue) ? ProjectUtility.Path.UIPrefabPath : _prefabPath.stringValue;
            _comCodePath.stringValue = string.IsNullOrEmpty(_comCodePath.stringValue) ? ProjectUtility.Path.UICodePath : _comCodePath.stringValue;

            _tempFiledNames = new List<string>();
            _tempComponentTypeNames = new List<string>();
            _componentsName = new Dictionary<string, int>();

            _sortByTypes = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "UiBindSortType", 1) == 1;
            _sortByNamesLength = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "UiBindSortName", 1) == 1;
            _sortByType.boolValue = _sortByTypes;
            _sortByNameLength.boolValue = _sortByNamesLength;
            _packUpBindList.boolValue = true;

            _comCodePath.stringValue = ProjectUtility.Path.UICodePath;
            _prefabPath.stringValue = ProjectUtility.Path.UIPrefabPath;

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
            _namespace.stringValue = EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Script, Lc.Namespace }), _namespace.stringValue);
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Default, Lc.Settings })))
            {
                _namespace.stringValue = _setting.Namespace;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Script, Lc.Class, Lc.Name }), m_Builder.gameObject.name);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(12f, true);

            _createPrefab.boolValue = GUILayout.Toggle(_createPrefab.boolValue, _createPrefab.boolValue ?
                LC.Combine(Lc.Create) + " UI " + LC.Combine(Lc.Prefab) : 
                LC.Combine(new Lc[] { Lc.No, Lc.Create }) + " UI " + LC.Combine(Lc.Prefab))
                ;
            if (_createPrefab.boolValue)
            {
                EditorGUILayout.LabelField(_prefabPath.stringValue);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Select, Lc.Path })))
                {
                    string folder = Path.Combine(Application.dataPath, _prefabPath.stringValue);
                    if (!Directory.Exists(folder))
                    {
                        folder = Application.dataPath;
                    }
                    string path = EditorUtility.OpenFolderPanel(LC.Combine(new Lc[] { Lc.Select, Lc.Path }), folder, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        _prefabPath.stringValue = path.Replace(Application.dataPath + "/", "Assets/") + "/";
                    }
                }
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Default, Lc.Settings })))
                {
                    _prefabPath.stringValue = ProjectUtility.Path.UIPrefabPath;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(12f, true);

            EditorGUILayout.LabelField(LC.Combine(Lc.Default) + " UI " + LC.Combine(new Lc[] { Lc.Code, Lc.Save, Lc.Path }));
            EditorGUILayout.LabelField(_comCodePath.stringValue);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Select, Lc.Path })))
            {
                string folder = Application.dataPath + "/" + _comCodePath.stringValue;
                if (!Directory.Exists(folder))
                {
                    folder = Application.dataPath;
                }
                string path = EditorUtility.OpenFolderPanel(LC.Combine(new Lc[] { Lc.Select, Lc.Path }), folder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _comCodePath.stringValue = path.Replace(Application.dataPath + "/", "Assets/") + "/";
                }
            }
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Default, Lc.Settings })))
            {
                _comCodePath.stringValue = ProjectUtility.Path.UICodePath;
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw auto binding
        /// 绘制绑定按钮
        /// </summary>
        private void DrawAutoBind()
        {
            GUILayout.Space(12.0f);
            GUILayout.BeginHorizontal();
            _sortByTypes = GUILayout.Toggle(_sortByTypes, LC.Combine(new Lc[] { Lc.By, Lc.Type, Lc.Sort }));
            if (_sortByTypes != _sortByType.boolValue)
            {
                _sortByType.boolValue = _sortByTypes;
                EditorPrefs.SetInt(ProjectUtility.Project.AppConst.AppPrefix + "UiBindSortType", _sortByTypes ? 1 : 0);
            }
            _sortByNamesLength = GUILayout.Toggle(_sortByNamesLength, LC.Combine(new Lc[] { Lc.By, Lc.Name, Lc.Length, Lc.Sort }));
            if (_sortByNamesLength != _sortByNameLength.boolValue)
            {
                _sortByNameLength.boolValue = _sortByNamesLength;
                EditorPrefs.SetInt(ProjectUtility.Project.AppConst.AppPrefix + "UiBindSortName", _sortByNamesLength ? 1 : 0);
            }
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
            int needDeleteIndex = -1;

            _packUpBindList.boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(_packUpBindList.boolValue, _packUpBindList.boolValue ? LC.Combine(new Lc[] { Lc.Close, Lc.List }) : LC.Combine(new Lc[] { Lc.Open, Lc.List }));
            if (_packUpBindList.boolValue)
            {
                EditorGUILayout.BeginVertical();
                SerializedProperty property;

                for (int i = 0; i < _bindDatas.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(25));
                    property = _bindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("ScriptName");
                    EditorGUILayout.PrefixLabel(property.stringValue);
                    property = _bindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindCom");
                    property.objectReferenceValue = EditorGUILayout.ObjectField(property.objectReferenceValue, typeof(Component), true);

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
                    _bindDatas.DeleteArrayElementAtIndex(needDeleteIndex);
                    SyncBindComs();
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Draw start bind button
        /// 绘制开始绑定按钮
        /// </summary>
        private void DrawStartBind()
        {
            GUILayout.Space(24f);
            _deleteScript.boolValue = GUILayout.Toggle(_deleteScript.boolValue, LC.Combine(new Lc[] { Lc.Unload, Lc.Script }));

            GUILayout.Space(12f);
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Bind, Lc.Create }), GUILayout.Height(25.0f)))
            {
                GenAutoBindCode();
                if (_createPrefab.boolValue)
                {
                    CreateOrModifyPrefab();
                }
                if (_deleteScript.boolValue)
                {
                    DestroyImmediate(m_Builder);
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
            _bindDatas.ClearArray();
            _componentsName.Clear();
            Transform[] childs = m_Builder.gameObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in childs)
            {
                _tempFiledNames.Clear();
                _tempComponentTypeNames.Clear();
                if (child == m_Builder.transform)
                {
                    continue;
                }
                UiBind componentAuto1 = child.gameObject.GetComponent<UiBind>();
                UiBind componentAuto = child.gameObject.GetComponentInParent<UiBind>(true);
                if (componentAuto1 == null)
                {
                    if (componentAuto != null && componentAuto != m_Builder)
                    {
                        continue;
                    }
                }
                if (IsValidBind(child, _tempFiledNames, _tempComponentTypeNames))
                {
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

            SyncBindComs();
        }

        /// <summary>
        /// Sync bind components.
        /// 同步绑定数据
        /// </summary>
        private void SyncBindComs()
        {
            _bindComs.ClearArray();

            for (int i = 0; i < _bindDatas.arraySize; i++)
            {
                SerializedProperty property = _bindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindCom");
                _bindComs.InsertArrayElementAtIndex(i);
                _bindComs.GetArrayElementAtIndex(i).objectReferenceValue = property.objectReferenceValue;
            }
        }

        /// <summary>
        /// Add bind data.
        /// 添加绑定数据
        /// </summary>
        private void AddBindData(string realName, string scriptName, Component bindCom)
        {
            int _index = _bindDatas.arraySize;
            List<string> _nameList = new List<string>();
            for (int i = 0; i < _bindDatas.arraySize; i++)
            {
                SerializedProperty elementData = _bindDatas.GetArrayElementAtIndex(i);
                string _elementName = elementData.FindPropertyRelative("ScriptName").stringValue;
                if (_elementName == scriptName)
                {
                    D.Error($"有重复名字！请检查后重新生成！Name:{realName}");
                    _nameList.Clear();
                    return;
                }
                else
                    _nameList.Add(_elementName);
            }

            string _componentsName = bindCom.GetType().Name;
            if (_sortByType.boolValue && _sortByNameLength.boolValue)
            {
                if (!this._componentsName.ContainsKey(_componentsName))
                    this._componentsName.Add(_componentsName, _index);
                else
                {
                    List<string> _comNameList = new List<string>();
                    foreach (var item in this._componentsName.Keys)
                        _comNameList.Add(item);

                    int _indexOf = _comNameList.IndexOf(_componentsName);
                    for (int i = _indexOf + 1; i < _comNameList.Count; i++)
                        ++this._componentsName[_comNameList[i]];

                    int _endIndex = _indexOf + 1 >= _comNameList.Count ? _bindDatas.arraySize : this._componentsName[_comNameList[_indexOf + 1]] - 1;
                    _index = EditorUtils.GetIndexWithLengthSort(scriptName.Length, _nameList, this._componentsName[_comNameList[_indexOf]], _endIndex);
                    _comNameList.Clear();
                }
            }
            else if (_sortByType.boolValue && !_sortByNameLength.boolValue)
            {
                if (!this._componentsName.ContainsKey(_componentsName))
                    this._componentsName.Add(_componentsName, _index);
                else
                {
                    _index = ++this._componentsName[_componentsName];
                    List<string> _comNameList = new List<string>();
                    foreach (var item in this._componentsName.Keys)
                        _comNameList.Add(item);
                    for (int i = _comNameList.IndexOf(_componentsName) + 1; i < _comNameList.Count; i++)
                        ++this._componentsName[_comNameList[i]];
                    _comNameList.Clear();
                }
            }
            else if (!_sortByType.boolValue && _sortByNameLength.boolValue)
                _index = EditorUtils.GetIndexWithLengthSort(scriptName.Length, _nameList, 0, _nameList.Count);
            else
                _index = _nameList.Count;

            InsertArrayElementAtIndex(_index, realName, scriptName, bindCom);
            _nameList.Clear();
        }

        /// <summary>
        /// 插入元素
        /// </summary>
        /// <param scriptName="index">插入位置</param>
        /// <param scriptName="scriptName">元素名</param>
        /// <param scriptName="bindCom">对应组件</param>
        private void InsertArrayElementAtIndex(int index, string realName, string scriptName, Component bindCom)
        {
            //D.Warning($"InsertArrayElementAtIndex()\tindex = {index}\tname = {scriptName}\tbindCom = {bindCom}");
            _bindDatas.InsertArrayElementAtIndex(index);
            SerializedProperty element = _bindDatas.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("RealName").stringValue = realName;
            element.FindPropertyRelative("ScriptName").stringValue = scriptName;
            element.FindPropertyRelative("BindCom").objectReferenceValue = bindCom;
        }

        /// <summary>
        /// 查找是否为有效绑定
        /// </summary>
        /// <param scriptName="target">目标</param>
        /// <param scriptName="filedNames">对象名</param>
        /// <param scriptName="componentTypeNames">对象组件</param>
        /// <returns>是否有效</returns>
        private bool IsValidBind(Transform target, List<string> filedNames, List<string> componentTypeNames)
        {
            string[] _strArray = target.name.Split('_');

            if (_strArray.Length == 1)
            {
                return false;
            }

            bool isFind = false;
            string filedName = _strArray[^1];
            filedName = EditorUtils.RemovePunctuation(filedName).Trim();

            List<RulePrefixe> _PrefixesDict = EditorUtils.LoadSettingAtPath<AutoBindSetting>().RulePrefixes;
            for (int i = 0; i < _strArray.Length - 1; i++)
            {
                string _prefixe = _strArray[i];
                string _comName;
                bool isFindComponent = false;
                foreach (RulePrefixe autoBindRulePrefix in _PrefixesDict)
                {
                    if (autoBindRulePrefix.Prefixe.Equals(_prefixe))
                    {
                        _comName = autoBindRulePrefix.FullContent;
                        filedNames.Add($"{_prefixe}_{filedName}");
                        componentTypeNames.Add(_comName);
                        isFind = true;
                        isFindComponent = true;
                        break;
                    }
                }
                if (!isFindComponent)
                {
                    D.Warning($"{target.name}的命名中{_prefixe}不存在对应的组件类型，绑定失败");
                }
            }
            return isFind;
        }
        #endregion

        #region Create prefab file. 生成预制件文件
        private void CreateOrModifyPrefab()
        {
            string path;
            if (Application.dataPath.Equals(_prefabPath.stringValue))
                path = $"{_prefabPath.stringValue}/{m_Builder.name}.prefab";
            else
                path = $"{Application.dataPath}/{_prefabPath.stringValue}/{m_Builder.name}.prefab";
            if (path.Contains("Assets/Assets"))
                path = path.Replace("Assets/Assets", "Assets");

            if (File.Exists(path))
            {
                File.Delete(path);
                File.Delete(path + ".meta");
            }

            GameObject _obj = PrefabUtility.SaveAsPrefabAsset(m_Builder.gameObject, path);
            DestroyImmediate(_obj.GetComponent<UiBind>(), true);

            AssetDatabase.SaveAssets();
        }
        #endregion

        #region Create code file. 生成代码文件
        readonly string m_AnnotationCSStr =
        "/*\n"
        + " * ================================================\r\n"
        + " * Describe:      This script is used to .\r\n"
        + " * Author:        #Author#\r\n"
        + " * CreationTime:  #CreatTime#\r\n"
        + " * ModifyAuthor:  #ChangeAuthor#\r\n"
        + " * ModifyTime:    #ChangeTime#\r\n"
        + " * ScriptVersion: #Version# \r\n"
        + " * ================================================\r\n"
        + "*/";
        readonly string m_StrChangeTime = "* ModifyTime:";
        readonly string m_StrChangeAuthor = "* ModifyAuthor:";

        readonly string ComponentsStart = "#region Components.可使用组件 -- Auto";
        readonly string ComponentsEnd = "#endregion Components -- Auto";
        readonly string QuitComponentsStart = "#region Quit Buttons.按钮 -- Auto";
        readonly string QuitComponentsEnd = "#endregion Buttons.按钮 -- Auto";
        readonly string FindComsStart = "#region Find components and register button event. 查找组件并且注册按钮事件 -- Auto";
        readonly string FindComsEnd = "#endregion  Find components end. -- Auto";
        readonly string ButtonEventsStart = "#region Button event in game ui page.";
        readonly string ButtonEventsEnd = "#endregion button event.  Do not change here.不要更改这行 -- Auto";
        readonly string ScriptEnd = "\t}\n}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto";

        /// <summary>
        /// Gen auto bind code on the basis of special scriptName.
        /// 基于特殊名称生成自动绑定代码
        /// </summary>
        private void GenAutoBindCode()
        {
            string codePath = !string.IsNullOrEmpty(m_Builder.ComCodePath) ? m_Builder.ComCodePath : ProjectUtility.Path.UICodePath;
            //codePath = Path.Combine(Application.dataPath, codePath);
            if (!Directory.Exists(codePath))
            {
                D.Exception($"生成{m_Builder.name}的代码保存路径{codePath}无效");
                return;
            }
            codePath = $"{codePath}{m_Builder.name}";
            if (!Directory.Exists(codePath))
            {
                Directory.CreateDirectory(codePath);
            }

            int _btnIndex = -1;
            bool _hasButton = false;
            bool _hasOtherComs = false;
            List<string> _namespace = new List<string>();
            List<string> _otherNameLst = new List<string>();
            List<string> _ButtonNameLst = new List<string>();
            List<string> _ButtonProNameLst = new List<string>();
            List<string> _ButtonLst = new List<string>();
            List<string> _ButtonProLst = new List<string>();
            Dictionary<string, string> _otherComponent = new Dictionary<string, string>();
            for (int i = 0; i < m_Builder.BindDatas.Count; i++)
            {
                Type type = m_Builder.BindDatas[i].BindCom.GetType();

                string ns = type.Namespace;
                if (!_namespace.Contains(ns))
                    _namespace.Add(ns);

                if (type == typeof(ButtonPro))
                {
                    _hasButton = true;
                    _ButtonProNameLst.Add(m_Builder.BindDatas[i].RealName);
                    _ButtonProLst.Add(m_Builder.BindDatas[i].ScriptName);
                }
                else if (type == typeof(UnityEngine.UI.Button))
                {
                    _hasButton = true;
                    _ButtonNameLst.Add(m_Builder.BindDatas[i].RealName);
                    _ButtonLst.Add(m_Builder.BindDatas[i].ScriptName);
                }
                else
                {
                    _hasOtherComs = true;
                    _otherNameLst.Add(m_Builder.BindDatas[i].RealName);
                    _otherComponent.Add(m_Builder.BindDatas[i].ScriptName, m_Builder.BindDatas[i].BindCom.GetType().Name);
                }
            }

            string filePath = $"{codePath}/{m_Builder.name}.cs";
            if (!File.Exists(filePath))
            {
                using StreamWriter sw = new StreamWriter(filePath);
                sw.WriteLine(GetFileHead());

                sw.WriteLine("\nusing EasyFramework;");
                if (!_namespace.Contains("EasyFramework.UI"))
                    sw.WriteLine("using EasyFramework.UI;");

                if (_hasButton)
                    sw.WriteLine("using System.Collections.Generic;");

                if (!_namespace.Contains("UnityEngine"))
                    sw.WriteLine("using UnityEngine;");

                for (int i = 0; i < _namespace.Count; i++)
                    sw.WriteLine($"using {_namespace[i]};");

                if (!string.IsNullOrEmpty(m_Builder.Namespace))
                    sw.WriteLine($"\nnamespace {m_Builder.Namespace}" + "\n{");
                else
                    sw.WriteLine("\nnamespace PleaseAmendNamespace\n{");

                sw.WriteLine($"\t/// <summary>\n\t/// Please modify the description.\n\t/// </summary>");
                sw.WriteLine("\tpublic class " + m_Builder.name + " : UIPageBase\n\t{");

                #region override Awake 
                sw.WriteLine("\t\t/* ---------- Do not change anything with an ' -- Auto' ending. 不要对以 -- Auto 结尾的内容做更改 ---------- */");
                sw.WriteLine("\t\t" + ComponentsStart);

                if (_hasOtherComs)
                {
                    foreach (KeyValuePair<string, string> item in _otherComponent)
                    {
                        sw.WriteLine($"\t\tprivate {item.Value} {item.Key};");
                    }
                }
                if (_hasButton)
                {
                    //sw.WriteLine("\t\t/// <summary>\n\t\t/// Please dont changed this content, system will be proces. \n\t\t/// 请不要更改此内容，系统将会处理 \n\t\t/// </summary>");
                    sw.WriteLine("\t\tprivate List<Button> m_AllButtons;");
                    sw.WriteLine("\t\tprivate List<ButtonPro> m_AllButtonPros;");
                }

                sw.WriteLine("\t\t" + ComponentsEnd);
                sw.WriteLine("\n\t\tpublic override void Awake(GameObject obj, params object[] args)\n\t\t{");

                sw.WriteLine("\t\t\t" + FindComsStart);

                if (_hasOtherComs)
                {
                    foreach (KeyValuePair<string, string> item in _otherComponent)
                        sw.WriteLine($"\t\t\t{item.Key} = EF.Tool.Find<{item.Value}>(obj.transform, \"{_otherNameLst[++_btnIndex]}\");");
                }
                if (_hasButton)
                {
                    _btnIndex = -1;
                    foreach (var btn in _ButtonLst)
                    {
                        sw.WriteLine($"\t\t\tEF.Tool.Find<Button>(obj.transform, \"{_ButtonNameLst[++_btnIndex]}\").RegisterInListAndBindEvent(OnClick{btn}, ref m_AllButtons);");
                    }
                    _btnIndex = -1;
                    foreach (var btnPro in _ButtonProLst)
                    {
                        sw.WriteLine($"\t\t\tEF.Tool.Find<ButtonPro>(obj.transform, \"{_ButtonProNameLst[++_btnIndex]}\").RegisterInListAndBindEvent(OnClick{btnPro}, ref m_AllButtonPros);");
                    }
                }
                sw.WriteLine("\t\t\t" + FindComsEnd);
                sw.WriteLine("\t\t}\n");
                #endregion

                #region override Quit
                sw.WriteLine("\t\tpublic override void Quit()\n\t\t{");
                if (_hasButton)
                {
                    sw.WriteLine("\t\t\t" + QuitComponentsStart);
                    sw.WriteLine("\t\t\tm_AllButtons.ReleaseAndRemoveEvent();");
                    sw.WriteLine("\t\t\tm_AllButtons = null;");
                    sw.WriteLine("\t\t\tm_AllButtonPros.ReleaseAndRemoveEvent();");
                    sw.WriteLine("\t\t\tm_AllButtonPros = null;");
                    sw.WriteLine("\t\t\t" + QuitComponentsEnd);
                }
                sw.WriteLine("\t\t}\n");
                #endregion

                #region ButtonEvent
                sw.WriteLine("\t\t" + ButtonEventsStart);
                for (int i = 0; i < m_Builder.BindDatas.Count; i++)
                {
                    Type type = m_Builder.BindDatas[i].BindCom.GetType();
                    if (type == typeof(ButtonPro))
                    {
                        sw.WriteLine($"\t\tvoid OnClick{m_Builder.BindDatas[i].ScriptName}() " + "\n\t\t{" + "\n\t\t\tD.Log(\"OnClick:  " + m_Builder.BindDatas[i].RealName + "\");\n\t\t}");
                    }
                    else if (type == typeof(UnityEngine.UI.Button))
                    {
                        sw.WriteLine($"\t\tvoid OnClick{m_Builder.BindDatas[i].ScriptName}() " + "\n\t\t{" + "\n\t\t\tD.Log(\"OnClick:  " + m_Builder.BindDatas[i].RealName + "\");\n\t\t}");
                    }
                }
                sw.WriteLine("\t\t" + ButtonEventsEnd);
                #endregion

                sw.WriteLine(ScriptEnd);
                sw.Close();
            }
            else
            {
                string _TabNumber = "";
                bool _isFirstNameSpace = true;
                bool _canOverwrite = true;
                string[] _strArr = File.ReadAllLines(filePath);
                List<string> _strList = new List<string>();

                for (int i = 0; i < _strArr.Length; i++)
                {
                    string str = _strArr[i];
                    if (str.Contains(ScriptEnd))
                        break;

                    if (str.Contains("namespace"))
                    {
                        if (_isFirstNameSpace)
                            _isFirstNameSpace = false;
                        else
                        {
                            _TabNumber += "\t";
                        }
                    }

                    #region Components
                    if (str.Contains(ComponentsStart))
                    {
                        _canOverwrite = false;
                        _strList.Add("\t\t" + _TabNumber + ComponentsStart);
                        foreach (KeyValuePair<string, string> item in _otherComponent)
                        {
                            _strList.Add($"\t\t{_TabNumber}private {item.Value} {item.Key};");
                        }
                        if (_hasButton)
                        {
                            _strList.Add(_TabNumber + "\t\tprivate List<Button> m_AllButtons;");
                            _strList.Add(_TabNumber + "\t\tprivate List<ButtonPro> m_AllButtonPros;");
                        }
                        continue;
                    }

                    if (str.Contains(ComponentsEnd))
                    {
                        _canOverwrite = true;
                    }
                    #endregion

                    #region override Awake
                    if (str.Contains(FindComsStart))
                    {
                        _canOverwrite = false;
                        _strList.Add("\t\t\t" + _TabNumber + FindComsStart);
                        continue;
                    }

                    if (str.Contains(FindComsEnd))
                    {
                        _btnIndex = -1;
                        foreach (KeyValuePair<string, string> item in _otherComponent)
                        {
                            _strList.Add($"\t\t\t{_TabNumber}{item.Key} = EF.Tool.Find<{item.Value}>(obj.transform, \"{_otherNameLst[++_btnIndex]}\");");
                        }
                        _btnIndex = -1;
                        foreach (var btn in _ButtonLst)
                        {
                            _strList.Add($"\t\t\t{_TabNumber}EF.Tool.Find<Button>(obj.transform, \"{_ButtonNameLst[++_btnIndex]}\").RegisterInListAndBindEvent(OnClick{btn}, ref m_AllButtons);");
                        }
                        _btnIndex = -1;
                        foreach (var btnPro in _ButtonProLst)
                        {
                            _strList.Add($"\t\t\t{_TabNumber}EF.Tool.Find<ButtonPro>(obj.transform, \"{_ButtonProNameLst[++_btnIndex]}\").RegisterInListAndBindEvent(OnClick{btnPro}, ref m_AllButtonPros);");
                        }
                        //_strList.Add("\t\t\t" + FindComsEnd);
                        _canOverwrite = true;
                    }
                    #endregion

                    #region override Quit
                    if (!_hasButton)
                    {
                        if (_canOverwrite && str.Contains(QuitComponentsStart))
                        {
                            _strList.Add(str);
                            _canOverwrite = false;
                            continue;
                        }
                        if (!_canOverwrite && str.Contains(QuitComponentsEnd))
                        {
                            _strList.Add(str);
                            _canOverwrite = true;
                            continue;
                        }
                    }
                    else
                    {
                        if (str.Contains(QuitComponentsStart))
                        {
                            _strList.Add(str);
                            if (_strArr[i + 1].Contains(QuitComponentsEnd))
                            {
                                _strList.Add(_TabNumber + "\t\t\tm_AllButtons.ReleaseAndRemoveEvent();");
                                _strList.Add(_TabNumber + "\t\t\tm_AllButtons = null;");
                                _strList.Add(_TabNumber + "\t\t\tm_AllButtonPros.ReleaseAndRemoveEvent();");
                                _strList.Add(_TabNumber + "\t\t\tm_AllButtonPros = null;");
                            }
                            continue;
                        }
                    }
                    #endregion

                    if (_canOverwrite)
                    {
                        _strList.Add(str);
                    }
                }


                #region If have button component, than need write new event.
                int _endIndex = 0;
                for (int idx = 0; idx < _strList.Count; idx++)
                {
                    if (_strList[idx].Contains(ButtonEventsEnd))
                    {
                        _endIndex = idx;
                        break;
                    }
                }

                for (int i = _ButtonLst.Count - 1; i >= 0; i--)
                {
                    _ButtonProLst.Insert(0, _ButtonLst[i]);
                }
                foreach (var btnPro in _ButtonProLst)
                {
                    bool contain = false;
                    for (int kIndex = 0; kIndex < _strList.Count; kIndex++)
                    {
                        string str = _strList[kIndex];

                        if (str.Contains($"void OnClick{btnPro}()"))
                        {
                            contain = true;
                            break;
                        }
                    }
                    if (!contain)
                    {
                        _strList.Insert(_endIndex, $"\t\tvoid OnClick{btnPro}() " + "\n\t\t{" + "\n\t\t\tD.Log(\"OnClick:  " + btnPro + "\");\n\t\t}");
                        _endIndex++;
                    }
                }

                #endregion
                ChangeFileHead(_strList);
                File.WriteAllLines(filePath, _strList.ToArray());
            }

            _otherNameLst.Clear();
            _otherComponent.Clear();
            _ButtonLst.Clear();
            _ButtonNameLst.Clear();
            _ButtonProLst.Clear();
            _ButtonProNameLst.Clear();

            ModifyFileFormat(filePath);
        }

        /// <summary>
        /// Modifythe file format.
        /// 修改文件格式
        /// </summary>
        /// <param scriptName="filePath">文件路径</param>
        private void ModifyFileFormat(string filePath)
        {
            string text = "";
            using (StreamReader read = new StreamReader(filePath))
            {
                string oldtext = read.ReadToEnd();
                text = oldtext;
                text = text.Replace("\n", "\r\n");
                text = text.Replace("\r\r\n", "\r\n"); // 防止替换了正常的换行符      
                if (oldtext.Length == text.Length)
                {
                    // 如果没有变化就退出
                }
            }
            File.WriteAllText(filePath, text, Encoding.UTF8); //utf-8格式保存，防止乱码
        }

        /// <summary>
        /// Amend head with changed the file.
        /// 更改文件头内容
        /// </summary>
        private void ChangeFileHead(List<string> strList)
        {
            for (int i = 0; i < strList.Count; i++)
            {
                if (strList[i].Contains(m_StrChangeAuthor))
                {
                    strList[i] = $" {m_StrChangeAuthor}  {ProjectUtility.Project.ScriptAuthor}";
                }
                if (strList[i].Contains(m_StrChangeTime))
                {
                    strList[i] = $" {m_StrChangeTime}    {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    return;
                }
            }
        }

        /// <summary>
        /// Get file head.
        /// 获取文件头内容
        /// </summary>
        private string GetFileHead()
        {
            string annotationStr = m_AnnotationCSStr;
            //把#CreateTime#替换成具体创建的时间
            annotationStr = annotationStr.Replace("#CreatTime#",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            annotationStr = annotationStr.Replace("#ChangeTime#",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            //把#Author# 替换
            annotationStr = annotationStr.Replace("#Author#",
                ProjectUtility.Project.ScriptAuthor);
            //把#ChangeAuthor# 替换
            annotationStr = annotationStr.Replace("#ChangeAuthor#",
                ProjectUtility.Project.ScriptAuthor);
            //把#Version# 替换
            annotationStr = annotationStr.Replace("#Version#",
                ProjectUtility.Project.ScriptVersion);
            return annotationStr;
        }
        #endregion
    }
}
