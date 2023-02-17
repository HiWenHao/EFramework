/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-08-25 15:08:37
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-08-25 15:08:37
 * Version:       0.1
 * ===============================================
*/
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using XHTools;

namespace EasyFramework.Windows
{
    /// <summary>
    /// Visualize PlayerPrefs
    /// </summary>
    public class PlayerPrefsWindow : EditorWindow
    {
        static EditorWindow m_Window;
        
        private bool AutoRef = true;
        private string FindStr = "";
        private Vector2 listPos;
        private ValueType Default, Select;
        private List<PlayerPrefsData> Datas = new List<PlayerPrefsData>();

        //[MenuItem("XHFramework/PlayerPrefs")]
        public static void OnOpen()
        {
            m_Window = GetWindow<PlayerPrefsWindow>("PlayerPrefs");
            m_Window.maxSize = new Vector2(3000f, 3000f);
            m_Window.minSize = new Vector2(200.0f, 200.0f);
        }

        private void OnEnable()
        {

        }
        private void OnDestroy()
        {
            Save();
        }

        private void OnGUI()
        {
            GUILayout.Space(10.0f);
            GUILayout.BeginVertical();

            #region 自动刷新   查找
            GUILayout.BeginHorizontal();
            GUILayout.Space(5.0f);
            AutoRef = GUILayout.Toggle(AutoRef, "Auto Refresh", EditorStyles.radioButton, GUILayout.Width(80f));
            GUILayout.Space(10.0f);
            FindStr = GUILayout.TextField(FindStr, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("×",GUILayout.Width(20.0f)))
            {
                FindStr = null;
            }
            GUILayout.EndHorizontal();
            #endregion

            #region 刷新  增加       
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
            {
                foreach (var item in GetAllKeysInWindows())
                {
                    D.Correct(item);
                    Datas.Add(new PlayerPrefsData()
                    {
                        Key = item,
                    });
                }
                D.Log("刷新");
            }
            if (GUILayout.Button("Add＋", GUILayout.Width(80f)))
            {
                D.Log("增加"); 
                Datas.Add(new PlayerPrefsData()
                {
                    Key = AppConst.AppPrefix,
                    DataType = Default,
                });
                switch (Default)
                {
                    default:
                    case ValueType.String:
                        Datas[Datas.Count - 1].Value = "";
                        break;
                    case ValueType.Int:
                        Datas[Datas.Count - 1].Value = 0;
                        break;
                    case ValueType.Float:
                        Datas[Datas.Count - 1].Value = 0.0f;
                        break;
                }
            }
            GUILayout.Label("Default");
            Default = (ValueType)EditorGUILayout.EnumPopup(Default);
            GUILayout.EndHorizontal();
            GUILayout.Space(5.0f);
            #endregion

            #region 数据列表展示
            ScrollList(true);
            #endregion

            #region 底部展示            
            if (GUILayout.Button("Save", GUILayout.Height(30f)))
            {
                Save();
                D.Log("保存全部");
            }
            GUILayout.BeginHorizontal();
            GUILayout.Space(10.0f);
            if (GUILayout.Button("Delete all", GUILayout.Width(100f)))
            {
                for (int i = Datas.Count - 1; i >= 0; i--)
                {
                    Datas.RemoveAt(i);
                }
            }
            GUILayout.Label($"A total of {Datas.Count}", GUILayout.Width(160)); //
            GUILayout.EndHorizontal();
            #endregion

            GUILayout.EndVertical();
        }

        private void Save()
        {
            for (int i = Datas.Count - 1; i >= 0; i--)
            {
                Datas[i].SaveChangeInfo();
            }
        }

        public void ScrollList(bool vertical)
        {
            if (Datas.Count == 0)
            {
                GUILayout.Label("  No data at present.");
            }

            //列表
            listPos = GUILayout.BeginScrollView(listPos, GUILayout.MaxHeight(2500), GUILayout.MinHeight(18));

            var endIndex = Datas.Count;

            //====================================================Begin
            if (vertical)
                GUILayout.BeginVertical();
            else
                GUILayout.BeginHorizontal();

            #region 单个数据渲染
            for (int i = endIndex - 1; i >= 0; i--)
            {
                GUILayout.Space(15.0f);
                GUILayout.BeginHorizontal(GUI.skin.box);
                {
                    GUILayout.BeginVertical();
                    {
                        #region Key
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Key", GUILayout.Width(60.0f));
                        Datas[i].Key = GUILayout.TextField(Datas[i].Key);
                        GUILayout.EndHorizontal();
                        #endregion

                        #region Value
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Value", GUILayout.Width(60.0f));
                        switch (Datas[i].DataType)
                        {
                            default:
                            case ValueType.String:
                                Datas[i].Value = EditorGUILayout.TextField((string)Datas[i].Value);
                                break;
                            case ValueType.Int:
                                Datas[i].Value = EditorGUILayout.IntField((int)Datas[i].Value);
                                break;
                            case ValueType.Float:
                                Datas[i].Value = EditorGUILayout.FloatField((float)Datas[i].Value);
                                break;
                        }
                        GUILayout.EndHorizontal();
                        #endregion

                        #region Type
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Type", GUILayout.Width(60.0f));
                        Select = (ValueType)EditorGUILayout.EnumPopup(Datas[i].DataType);
                        if (Select != Datas[i].DataType)
                        {
                            switch (Select)
                            {
                                default:
                                case ValueType.String:
                                    Datas[i].Value = Datas[i].Value.ToString();
                                    break;
                                case ValueType.Int:
                                    Datas[i].Value = 0;
                                    break;
                                case ValueType.Float:
                                    Datas[i].Value = 0.0f;
                                    break;
                            };
                            Datas[i].DataType = Select;
                        }
                        GUILayout.EndHorizontal();
                        #endregion
                        GUILayout.EndVertical();
                    }

                    #region 删除
                    if (GUILayout.Button("×", GUILayout.Height(58.0f), GUILayout.Width(60f)))
                    {
                        D.Log("删除单个");
                        Datas.RemoveAt(i);
                    }
                    #endregion
                    GUILayout.EndHorizontal();
                }
            }
            #endregion

            //====================================================End
            if (vertical)
                GUILayout.EndVertical();
            else
                GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
        }
        class PlayerPrefsData
        {
            private string m_key;
            private object m_value;
            private ValueType m_valueType;

            public string Key
            {
                get
                {
                    return m_key;
                }
                set
                {
                    m_key = value;
                    GetDataInfoByKey();
                }
            }
            public object Value
            {
                get { return m_value; }
                set
                {
                    m_value = value;
                }
            }
            public ValueType DataType
            {
                get { return m_valueType; }
                set
                {
                    m_valueType = value;
                }
            }

            public void SaveChangeInfo()
            {
                switch (DataType)
                {
                    default:
                    case ValueType.String:
                        PlayerPrefs.SetString(m_key, (string)m_value);
                        break;
                    case ValueType.Int:
                        PlayerPrefs.SetInt(m_key, (int)m_value);
                        break;
                    case ValueType.Float:
                        PlayerPrefs.SetFloat(m_key, (float)m_value);
                        break;
                }
                PlayerPrefs.Save();
            }

            public void GetDataInfoByKey()
            {
                if (PlayerPrefs.GetString(m_key,"qwer1212") != "qwer1212")
                {
                    m_valueType = ValueType.String;
                    m_value = PlayerPrefs.GetString(m_key);
                }
                else if (PlayerPrefs.GetFloat(m_key, 0.0f) != 0.0f)
                {
                    m_valueType = ValueType.Float;
                    m_value = PlayerPrefs.GetFloat(m_key);
                }
                else if (PlayerPrefs.GetInt(m_key, 0) != 0)
                {
                    m_valueType = ValueType.Int;
                    m_value = PlayerPrefs.GetInt(m_key);
                }
            }
        }
        enum ValueType
        {
            String,
            Int,
            Float,
        }



        #region Get all keys in computer
        private string[] GetAllKeys()
        {
            List<string> result = new List<string>();

            if (Application.platform == RuntimePlatform.WindowsEditor)
                result.AddRange(GetAllKeysInWindows());
            else if (Application.platform == RuntimePlatform.OSXEditor)
                result.AddRange(GetAllKeysInMac());
            else
            {
                Debug.LogError("Unsupported platform detected, please contact support@rejected-games.com and let us know.");
            }

            //Remove UnityGraphicsQuality, thats something Unity always saves in your PlayerPrefs, apparently
            //if (result.Contains(UNITY_GRAPHICS_QUALITY))
            //    result.Remove(UNITY_GRAPHICS_QUALITY);

            return result.ToArray();
        }

        /// <summary>
        /// On Mac OS X PlayerPrefs are stored in ~/Library/Preferences folder, in a file named unity.[company name].[product name].plist, where company and product names are the names set up in Project Settings. The same .plist file is used for both Projects run in the Editor and standalone players. 
        /// </summary>
        private string[] GetAllKeysInMac()
        {
            return new string[0];
            //FileInfo _fileInfo = new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Library/Preferences/unity." + PlayerSettings.companyName + "." + PlayerSettings.productName + ".plist");
            //Dictionary<string, object> plist = (Dictionary<string, object>)PlistCS.Plist.readPlist(_fileInfo.FullName);
            //
            //string[] keys = new string[plist.Count];
            //plist.Keys.CopyTo(keys, 0);
            //
            //return keys;
        }
        private string[] GetAllKeysInWindows()
        {
            RegistryKey _user = Registry.CurrentUser;
            RegistryKey _registry = _user.CreateSubKey($"Software\\Unity\\UnityEditor\\{PlayerSettings.companyName}\\{PlayerSettings.productName}");
            //D.Correct(_unityRegedit);
            string[] _names = _registry.GetValueNames();
            for (int i = 0; i < _names.Length; i++)
            {
                _names[i] = _names[i].Substring(0, _names[i].LastIndexOf("_"));
            }
            return _names;
        }
        #endregion
    }
}
