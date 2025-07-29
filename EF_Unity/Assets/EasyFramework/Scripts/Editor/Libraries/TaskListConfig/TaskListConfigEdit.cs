/* 
 * ================================================
 * Describe:      This script is used to show the user need to do list. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-15 16:22:01
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-07 11:29:07
 * ScriptVersion: 0.2
 * ===============================================
*/
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.TaskList
{
    /// <summary>
    /// Show the user need to do list.
    /// </summary>
    [CustomEditor(typeof(TaskListConfig))]
    public class TaskListConfigEdit : Editor
    {
        SerializedProperty _mark;
        SerializedProperty _title;
        SerializedProperty _enabled;
        SerializedProperty _progress;
        SerializedProperty _taskCount;
        SerializedProperty _description;

        GUIStyle _headStyle;
        GUIStyle _deleteStyle;
        GUIStyle _taskTitleStyle;
        GUIStyle _descriptionStyle;
        GUIStyle _taskTitleMarkStyle;
        GUIStyle _highlightMarkStyle;

        Color[] _contentColors;
        string[] _uiPopupContents;

        private void Awake()
        {
            _headStyle = new GUIStyle()
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = new Color(0.659f, 0.659f, 0.659f),
                }
            };

            _deleteStyle = new GUIStyle("Button"); 
            _deleteStyle.normal.textColor = new Color(0.4f, 0f, 0f);
            _deleteStyle.hover.textColor = new Color(0.4f, 0f, 0f);
            _deleteStyle.active.textColor = new Color(0.4f, 0f, 0f);
            _highlightMarkStyle = new GUIStyle("Button");
            _highlightMarkStyle.normal.textColor = new Color(0.5f, 1f, 1f);
            _highlightMarkStyle.hover.textColor = new Color(0.5f, 1f, 1f);
            _highlightMarkStyle.active.textColor = new Color(0.5f, 1f, 1f);

            _taskTitleStyle = new GUIStyle("MiniPopup");
            _taskTitleMarkStyle = new GUIStyle("PreviewPackageInUse");
            _descriptionStyle = new GUIStyle("TextField")
            {
                wordWrap = true
            };

            _uiPopupContents = new string[]
            {
                LC.Combine(Lc.Doing),
                LC.Combine(Lc.Done),
                LC.Combine(Lc.Timeout),
                LC.Combine(Lc.Abandon)
            };
            _contentColors = new Color[]
            {
                Color.yellow,
                Color.green,
                Color.red,
                Color.gray
            };
        }

        private void OnEnable()
        {
            _mark = serializedObject.FindProperty("Mark");
            _title = serializedObject.FindProperty("Title");
            _enabled = serializedObject.FindProperty("Enabled");
            _progress = serializedObject.FindProperty("Progress");
            _taskCount = serializedObject.FindProperty("TaskCount");
            _description = serializedObject.FindProperty("Description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(new GUIContent(LC.Combine(new Lc[] { Lc.Task, Lc.List })), _headStyle);

            ShowListInfo();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Add, Lc.Task })))
            {
                _mark.InsertArrayElementAtIndex(_taskCount.intValue);
                _title.InsertArrayElementAtIndex(_taskCount.intValue);
                _enabled.InsertArrayElementAtIndex(_taskCount.intValue);
                _progress.InsertArrayElementAtIndex(_taskCount.intValue);
                _description.InsertArrayElementAtIndex(_taskCount.intValue);

                _progress.GetArrayElementAtIndex(_taskCount.intValue).intValue = 0;
                _mark.GetArrayElementAtIndex(_taskCount.intValue).boolValue = false;
                _enabled.GetArrayElementAtIndex(_taskCount.intValue).boolValue = true;
                _title.GetArrayElementAtIndex(_taskCount.intValue).stringValue = LC.Combine(Lc.Title);
                _description.GetArrayElementAtIndex(_taskCount.intValue).stringValue = LC.Combine(new Lc[] { Lc.Task, Lc.Description });

                _taskCount.intValue++;
            }
            EditorGUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Remove, Lc.All })))
            {
                if (0 != _taskCount.intValue && EditorUtility.DisplayDialog(LC.Combine(new Lc[] { Lc.Delete, Lc.Task }), LC.Combine(new Lc[] { Lc.Remove, Lc.All, Lc.Task }), LC.Combine(Lc.Ok)))
                {
                    _taskCount.intValue = 0;
                    _mark.ClearArray();
                    _title.ClearArray();
                    _enabled.ClearArray();
                    _progress.ClearArray();
                    _description.ClearArray();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        void ShowListInfo()
        {
            for (int i = 0; i < _taskCount.intValue; i++)
            {
                EditorGUILayout.Space(6f);
                _enabled.GetArrayElementAtIndex(i).boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(_enabled.GetArrayElementAtIndex(i).boolValue,
                    content: $"    {_title.GetArrayElementAtIndex(i).stringValue}",
                    style: TaskStyle(_progress.GetArrayElementAtIndex(i).intValue, _mark.GetArrayElementAtIndex(i).boolValue)
                    );
                if (_enabled.GetArrayElementAtIndex(i).boolValue)
                {
                    EditorGUILayout.BeginVertical("AvatarMappingBox");

                    EditorGUILayout.BeginHorizontal();
                    _progress.GetArrayElementAtIndex(i).intValue = EditorGUILayout.IntPopup(LC.Combine(Lc.Progress),
                        selectedValue: _progress.GetArrayElementAtIndex(i).intValue,
                        displayedOptions: _uiPopupContents,
                        optionValues: new int[] { 0, 1, 2, 3 },
                        style: TaskStyle(_progress.GetArrayElementAtIndex(i).intValue, false)
                        );
                    GUILayout.Space(15f);
                    if (GUILayout.Button(_mark.GetArrayElementAtIndex(i).boolValue ?
                        LC.Combine(new Lc[] { Lc.Cancel, Lc.Mark }) :
                        LC.Combine(new Lc[] { Lc.Highlight, Lc.Mark }),
                        _highlightMarkStyle)
                        )
                    {
                        _mark.GetArrayElementAtIndex(i).boolValue = !_mark.GetArrayElementAtIndex(i).boolValue;
                    }
                    EditorGUILayout.EndHorizontal();
                    _title.GetArrayElementAtIndex(i).stringValue = EditorGUILayout.TextField(_title.GetArrayElementAtIndex(i).stringValue);
                    _description.GetArrayElementAtIndex(i).stringValue = EditorGUILayout.TextArea(_description.GetArrayElementAtIndex(i).stringValue, _descriptionStyle);

                    EditorGUILayout.BeginHorizontal();
                    if (i != 0 && GUILayout.Button(LC.Combine(Lc.MoveUp)))
                    {
                        _mark.MoveArrayElement(i, i - 1);
                        _enabled.MoveArrayElement(i, i - 1);
                        _progress.MoveArrayElement(i, i - 1);
                        _title.MoveArrayElement(i, i - 1);
                        _description.MoveArrayElement(i, i - 1);
                    }
                    if (i != (_taskCount.intValue - 1) && GUILayout.Button(LC.Combine(Lc.MoveDown)))
                    {
                        _mark.MoveArrayElement(i, i + 1);
                        _enabled.MoveArrayElement(i, i + 1);
                        _progress.MoveArrayElement(i, i + 1);
                        _title.MoveArrayElement(i, i + 1);
                        _description.MoveArrayElement(i, i + 1);
                    }
                    if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Delete, Lc.Task }), _deleteStyle))
                    {
                        _mark.DeleteArrayElementAtIndex(i);
                        _enabled.DeleteArrayElementAtIndex(i);
                        _progress.DeleteArrayElementAtIndex(i);
                        _title.DeleteArrayElementAtIndex(i);
                        _description.DeleteArrayElementAtIndex(i);
                        _taskCount.intValue--;
                        continue;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        GUIStyle TaskStyle(int index, bool mark)
        {
            GUIStyle _style = mark ? _taskTitleMarkStyle : _taskTitleStyle;

            _style.hover.textColor = _contentColors[index];
            _style.normal.textColor = _contentColors[index];
            _style.active.textColor = _contentColors[index];
            _style.focused.textColor = _contentColors[index];
            _style.onHover.textColor = _contentColors[index];
            _style.onNormal.textColor = _contentColors[index];
            _style.onFocused.textColor = _contentColors[index];

            return _style;
        }
    }
}
