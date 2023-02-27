/* 
 * ================================================
 * Describe:      This script is used to builder with editor. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-13 16:46:15
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-13 16:46:15
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using XHTools;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Ui builder with editor.
    /// </summary>
    [CustomEditor(typeof(UiBind))]
    public class UiBuilderEditor : Editor
    {
        UiBind m_Builder;

        private SerializedProperty m_BindDatas;
        private SerializedProperty m_BindComs;
        private SerializedProperty m_Namespace;
        private SerializedProperty m_ComCodePath;
        private SerializedProperty m_PrefabPath;
        private SerializedProperty m_CreatePrefab;
        private SerializedProperty m_DeleteScript;

        private AutoBindSetting m_Setting;
        private List<string> m_TempFiledNames = new List<string>();
        private List<string> m_TempComponentTypeNames = new List<string>();

        private void OnEnable()
        {
            m_Builder = (UiBind)target;
            m_Setting = AutoBindSetting.GetAutoBindSetting();
            m_BindComs = serializedObject.FindProperty("m_BindComs");
            m_BindDatas = serializedObject.FindProperty("BindDatas");
            m_Namespace = serializedObject.FindProperty("m_Namespace");
            m_PrefabPath = serializedObject.FindProperty("m_PrefabPath");
            m_ComCodePath = serializedObject.FindProperty("m_ComCodePath");
            m_CreatePrefab = serializedObject.FindProperty("m_CreatePrefab");
            m_DeleteScript = serializedObject.FindProperty("m_DeleteScript");

            m_Namespace.stringValue = string.IsNullOrEmpty(m_Namespace.stringValue) ? m_Setting.Namespace : m_Namespace.stringValue;
            m_PrefabPath.stringValue = string.IsNullOrEmpty(m_PrefabPath.stringValue) ? m_Setting.PrefabPath : m_PrefabPath.stringValue;
            m_ComCodePath.stringValue = string.IsNullOrEmpty(m_ComCodePath.stringValue) ? m_Setting.ComCodePath : m_ComCodePath.stringValue;

            serializedObject.ApplyModifiedProperties();
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
            m_Namespace.stringValue = EditorGUILayout.TextField(new GUIContent("命名空间："), m_Namespace.stringValue);
            if (GUILayout.Button("默认设置"))
            {
                m_Namespace.stringValue = m_Setting.Namespace;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(new GUIContent($"类名： {m_Builder.gameObject.name}   (自动与当前对象名保持一致)"));

            EditorGUILayout.Space(12f, true);

            m_CreatePrefab.boolValue = GUILayout.Toggle(m_CreatePrefab.boolValue, m_CreatePrefab.boolValue ? "  UI预制件的保存路径：" : "  同时生成UI的预制件，如果已存在则会修改");
            if (m_CreatePrefab.boolValue)
            {
                EditorGUILayout.LabelField(m_PrefabPath.stringValue);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("选择UI预制件保存路径"))
                {
                    string folder = Path.Combine(Application.dataPath, m_PrefabPath.stringValue);
                    if (!Directory.Exists(folder))
                    {
                        folder = Application.dataPath;
                    }
                    string path = EditorUtility.OpenFolderPanel("选择UI预制件保存路径", folder, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        m_PrefabPath.stringValue = path.Replace(Application.dataPath + "/", "");
                    }
                }
                if (GUILayout.Button("默认设置"))
                {
                    m_PrefabPath.stringValue = m_Setting.PrefabPath;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(12f, true);

            EditorGUILayout.LabelField("自动生成代码保存路径：");
            EditorGUILayout.LabelField(m_ComCodePath.stringValue);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("选择组件代码路径"))
            {
                string folder = Path.Combine(Application.dataPath, m_ComCodePath.stringValue);
                if (!Directory.Exists(folder))
                {
                    folder = Application.dataPath;
                }
                string path = EditorUtility.OpenFolderPanel("选择组件代码保存路径", folder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    m_ComCodePath.stringValue = path.Replace(Application.dataPath + "/", "");
                }
            }
            if (GUILayout.Button("默认设置"))
            {
                m_ComCodePath.stringValue = m_Setting.ComCodePath;
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
            if (GUILayout.Button("自动绑定组件"))
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

            EditorGUILayout.BeginVertical();
            SerializedProperty property;

            for (int i = 0; i < m_BindDatas.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(25));
                property = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
                property.stringValue = EditorGUILayout.TextField(property.stringValue, GUILayout.Width(150));
                property = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindCom");
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
                m_BindDatas.DeleteArrayElementAtIndex(needDeleteIndex);
                SyncBindComs();
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
            m_DeleteScript.boolValue = GUILayout.Toggle(m_DeleteScript.boolValue, "  生成UI后卸载该脚本");

            GUILayout.Space(12f);
            if (GUILayout.Button("确定生成", GUILayout.Height(25.0f)))
            {
                GenAutoBindCode();
                if (m_CreatePrefab.boolValue)
                {
                    CreateOrModifyPrefab();
                }
                if (m_DeleteScript.boolValue)
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
            m_BindDatas.ClearArray();

            Transform[] childs = m_Builder.gameObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in childs)
            {
                m_TempFiledNames.Clear();
                m_TempComponentTypeNames.Clear();
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
                if (AutoBindSetting.IsValidBind(child, m_TempFiledNames, m_TempComponentTypeNames))
                {
                    for (int i = 0; i < m_TempFiledNames.Count; i++)
                    {
                        Component com = child.GetComponent(m_TempComponentTypeNames[i]);
                        if (com == null)
                        {
                            Debug.LogError($"{child.name}上不存在{m_TempComponentTypeNames[i]}的组件");
                        }
                        else
                        {
                            string newFiledName = m_TempFiledNames[i].Replace("#", "");
                            AddBindData(newFiledName, child.GetComponent(m_TempComponentTypeNames[i]));
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
            m_BindComs.ClearArray();

            for (int i = 0; i < m_BindDatas.arraySize; i++)
            {
                SerializedProperty property = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindCom");
                m_BindComs.InsertArrayElementAtIndex(i);
                m_BindComs.GetArrayElementAtIndex(i).objectReferenceValue = property.objectReferenceValue;
            }
        }

        /// <summary>
        /// A得得得 bind data.
        /// 添加绑定数据
        /// </summary>
        private void AddBindData(string name, Component bindCom)
        {
            for (int i = 0; i < m_BindDatas.arraySize; i++)
            {
                SerializedProperty elementData = m_BindDatas.GetArrayElementAtIndex(i);
                if (elementData.FindPropertyRelative("Name").stringValue == name)
                {
                    Debug.LogError($"有重复名字！请检查后重新生成！Name:{name}");
                    return;
                }
            }
            int index = m_BindDatas.arraySize;
            m_BindDatas.InsertArrayElementAtIndex(index);
            SerializedProperty element = m_BindDatas.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Name").stringValue = name;
            element.FindPropertyRelative("BindCom").objectReferenceValue = bindCom;

        }
        #endregion

        #region Create prefab file. 生成预制件文件
        private void CreateOrModifyPrefab()
        {
            string _path;
            if (Application.dataPath == m_PrefabPath.stringValue)
                _path = $"{m_PrefabPath.stringValue}/{m_Builder.name}.prefab";
            else
                _path = $"{Application.dataPath}/{m_PrefabPath.stringValue}/{m_Builder.name}.prefab";

            if (File.Exists(_path))
            {
                File.Delete(_path);
                File.Delete(_path + ".meta");
            }

            GameObject _obj = PrefabUtility.SaveAsPrefabAsset(m_Builder.gameObject, _path);
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
        /// Gen auto bind code on the basis of special name.
        /// 基于特殊名称生成自动绑定代码
        /// </summary>
        private void GenAutoBindCode()
        {
            string codePath = !string.IsNullOrEmpty(m_Builder.ComCodePath) ? m_Builder.ComCodePath : m_Setting.ComCodePath;
            codePath = Path.Combine(Application.dataPath, codePath);
            if (!Directory.Exists(codePath))
            {
                D.Error($"生成{m_Builder.name}的代码保存路径{codePath}无效");
                return;
            }
            codePath = $"{codePath}/{m_Builder.name}";
            if (!Directory.Exists(codePath))
            {
                Directory.CreateDirectory(codePath);
            }

            bool _hasButton = false;
            bool _hasOtherComs = false;
            List<string> _ButtonLst = new List<string>();
            List<string> _ButtonProLst = new List<string>();
            Dictionary<string, string> _otherComponent = new Dictionary<string, string>();
            for (int i = 0; i < m_Builder.BindDatas.Count; i++)
            {
                Type _type = m_Builder.BindDatas[i].BindCom.GetType();
                if (_type == typeof(ButtonPro))
                {
                    _hasButton = true;
                    _ButtonProLst.Add(m_Builder.BindDatas[i].Name);
                }
                else if (_type == typeof(UnityEngine.UI.Button))
                {
                    _hasButton = true;
                    _ButtonLst.Add(m_Builder.BindDatas[i].Name);
                }
                else
                {
                    _hasOtherComs = true;
                    _otherComponent.Add(m_Builder.BindDatas[i].Name, m_Builder.BindDatas[i].BindCom.GetType().Name);
                }
            }

            string filePath = $"{codePath}/{m_Builder.name}.cs";
            if (!File.Exists(filePath))
            {
                using StreamWriter sw = new StreamWriter(filePath);
                sw.WriteLine(GetFileHead());

                sw.WriteLine("using EasyFramework.UI;");
                sw.WriteLine("using System.Collections.Generic;");
                sw.WriteLine("using UnityEngine;");
                sw.WriteLine("using UnityEngine.UI;");
                sw.WriteLine("using XHTools;");

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
                    {
                        sw.WriteLine($"\t\t\t{item.Key} = EF.Tool.RecursiveSearch<{item.Value}>(obj.transform, \"{item.Key}\") ;");
                    }
                }
                if (_hasButton)
                {
                    foreach (var btn in _ButtonLst)
                    {
                        sw.WriteLine($"\t\t\tEF.Tool.RecursiveSearch<Button>(obj.transform, \"{btn}\").RegisterInListAndBindEvent(OnClick{btn}, ref m_AllButtons);");
                    }
                    foreach (var btnPro in _ButtonProLst)
                    {
                        sw.WriteLine($"\t\t\tEF.Tool.RecursiveSearch<ButtonPro>(obj.transform, \"{btnPro}\").RegisterInListAndBindEvent(OnClick{btnPro}, ref m_AllButtonPros);");
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
                    Type _type = m_Builder.BindDatas[i].BindCom.GetType();
                    if (_type == typeof(ButtonPro))
                    {
                        sw.WriteLine($"\t\tvoid OnClick{m_Builder.BindDatas[i].Name}() " + "\n\t\t{" + "\n\t\t\tD.Log(\"OnClick:  " + m_Builder.BindDatas[i].Name + "\");\n\t\t}");
                    }
                    else if (_type == typeof(UnityEngine.UI.Button))
                    {
                        sw.WriteLine($"\t\tvoid OnClick{m_Builder.BindDatas[i].Name}() " + "\n\t\t{" + "\n\t\t\tD.Log(\"OnClick:  " + m_Builder.BindDatas[i].Name + "\");\n\t\t}");
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
                        if(_isFirstNameSpace)
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
                        foreach (KeyValuePair<string, string> item in _otherComponent)
                        {
                            _strList.Add($"\t\t\t{_TabNumber}{item.Key} = EF.Tool.RecursiveSearch<{item.Value}>(obj.transform, \"{item.Key}\") ;");
                        }
                        foreach (var btn in _ButtonLst)
                        {
                            _strList.Add($"\t\t\t{_TabNumber}EF.Tool.RecursiveSearch<Button>(obj.transform, \"{btn}\").RegisterInListAndBindEvent(OnClick{btn}, ref m_AllButtons);");
                        }
                        foreach (var btnPro in _ButtonProLst)
                        {
                            _strList.Add($"\t\t\t{_TabNumber}EF.Tool.RecursiveSearch<ButtonPro>(obj.transform, \"{btnPro}\").RegisterInListAndBindEvent(OnClick{btnPro}, ref m_AllButtonPros);");
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

            ModifyFileFormat(filePath);
        }

        /// <summary>
        /// Modifythe file format.
        /// 修改文件格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
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
                    strList[i] = $" {m_StrChangeAuthor}  {EFProjectSettingsUtils.FrameworkGlobalSetting.ScriptAuthor}";
                }
                if (strList[i].Contains(m_StrChangeTime))
                {
                    strList[i] = $" {m_StrChangeTime}    {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";
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
                System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            annotationStr = annotationStr.Replace("#ChangeTime#",
                System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            //把#Author# 替换
            annotationStr = annotationStr.Replace("#Author#",
                EFProjectSettingsUtils.FrameworkGlobalSetting.ScriptAuthor);
            //把#ChangeAuthor# 替换
            annotationStr = annotationStr.Replace("#ChangeAuthor#",
                EFProjectSettingsUtils.FrameworkGlobalSetting.ScriptAuthor);
            //把#Version# 替换
            annotationStr = annotationStr.Replace("#Version#",
                EFProjectSettingsUtils.FrameworkGlobalSetting.ScriptVersion);
            return annotationStr;
        }
        #endregion
    }
}
