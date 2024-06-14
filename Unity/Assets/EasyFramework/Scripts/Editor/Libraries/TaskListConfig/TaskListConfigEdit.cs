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
        SerializedProperty Mark;
        SerializedProperty Title;
        SerializedProperty Enabled;
        SerializedProperty Progress;
        SerializedProperty TaskCount;
        SerializedProperty Description;

        GUIStyle m_HeadStyle;
        GUIStyle m_DeleteStyle;
        GUIStyle m_TaskTitleStyle;
        GUIStyle m_DescriptionStyle;
        GUIStyle m_TaskTitleMarkStyle;
        GUIStyle m_HighlightMarkStyle;

        readonly Color[] m_ContentColors = new Color[]
        {
            Color.yellow,
            Color.green,
            Color.red,
            Color.gray
        };
        readonly string[] UIPopupContents = new string[]
        {
            LC.Combine("Doing"),
            LC.Combine("Done"),
            LC.Combine("Timeout"),
            LC.Combine("Abandon")
        };

        private void Awake()
        {
            m_HeadStyle = new GUIStyle()
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = new Color(0.659f, 0.659f, 0.659f),
                }
            };

            m_DeleteStyle = new GUIStyle("Button"); 
            m_DeleteStyle.normal.textColor = new Color(0.4f, 0f, 0f);
            m_DeleteStyle.hover.textColor = new Color(0.4f, 0f, 0f);
            m_DeleteStyle.active.textColor = new Color(0.4f, 0f, 0f);
            m_HighlightMarkStyle = new GUIStyle("Button");
            m_HighlightMarkStyle.normal.textColor = new Color(0.5f, 1f, 1f);
            m_HighlightMarkStyle.hover.textColor = new Color(0.5f, 1f, 1f);
            m_HighlightMarkStyle.active.textColor = new Color(0.5f, 1f, 1f);

            m_TaskTitleStyle = new GUIStyle("MiniPopup");
            m_TaskTitleMarkStyle = new GUIStyle("PreviewPackageInUse");
            m_DescriptionStyle = new GUIStyle("TextField")
            {
                wordWrap = true
            };

            //UIPopupContents;
        }

        private void OnEnable()
        {
            Mark = serializedObject.FindProperty("Mark");
            Title = serializedObject.FindProperty("Title");
            Enabled = serializedObject.FindProperty("Enabled");
            Progress = serializedObject.FindProperty("Progress");
            TaskCount = serializedObject.FindProperty("TaskCount");
            Description = serializedObject.FindProperty("Description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(new GUIContent(LC.Combine("Task", "List")), m_HeadStyle);

            ShowListInfo();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            if (GUILayout.Button(LC.Combine("Add", "Task")))
            {
                Mark.InsertArrayElementAtIndex(TaskCount.intValue);
                Title.InsertArrayElementAtIndex(TaskCount.intValue);
                Enabled.InsertArrayElementAtIndex(TaskCount.intValue);
                Progress.InsertArrayElementAtIndex(TaskCount.intValue);
                Description.InsertArrayElementAtIndex(TaskCount.intValue);

                Progress.GetArrayElementAtIndex(TaskCount.intValue).intValue = 0;
                Mark.GetArrayElementAtIndex(TaskCount.intValue).boolValue = false;
                Enabled.GetArrayElementAtIndex(TaskCount.intValue).boolValue = true;
                Title.GetArrayElementAtIndex(TaskCount.intValue).stringValue = LC.Combine("Title");
                Description.GetArrayElementAtIndex(TaskCount.intValue).stringValue = LC.Combine("Task", "Description");

                TaskCount.intValue++;
            }
            EditorGUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(LC.Combine("Remove", "All")))
            {
                if (0 != TaskCount.intValue && EditorUtility.DisplayDialog(LC.Combine("Delete", "Task"), LC.Combine("Remove", "All", "Task"), LC.Combine("Ok")))
                {
                    TaskCount.intValue = 0;
                    Mark.ClearArray();
                    Title.ClearArray();
                    Enabled.ClearArray();
                    Progress.ClearArray();
                    Description.ClearArray();
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
            for (int i = 0; i < TaskCount.intValue; i++)
            {
                EditorGUILayout.Space(6f);
                Enabled.GetArrayElementAtIndex(i).boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(Enabled.GetArrayElementAtIndex(i).boolValue,
                    content: $"    {Title.GetArrayElementAtIndex(i).stringValue}",
                    style: TaskStyle(Progress.GetArrayElementAtIndex(i).intValue, Mark.GetArrayElementAtIndex(i).boolValue)
                    );
                if (Enabled.GetArrayElementAtIndex(i).boolValue)
                {
                    EditorGUILayout.BeginVertical("AvatarMappingBox");

                    EditorGUILayout.BeginHorizontal();
                    Progress.GetArrayElementAtIndex(i).intValue = EditorGUILayout.IntPopup(LC.Combine("Progress"),
                        selectedValue: Progress.GetArrayElementAtIndex(i).intValue,
                        displayedOptions: UIPopupContents,
                        optionValues: new int[] { 0, 1, 2, 3 },
                        style: TaskStyle(Progress.GetArrayElementAtIndex(i).intValue, false)
                        );
                    GUILayout.Space(15f);
                    if (GUILayout.Button(Mark.GetArrayElementAtIndex(i).boolValue ?
                        LC.Combine("Cancel", "Mark") :
                        LC.Combine("Highlight", "Mark"),
                        m_HighlightMarkStyle)
                        )
                    {
                        Mark.GetArrayElementAtIndex(i).boolValue = !Mark.GetArrayElementAtIndex(i).boolValue;
                    }
                    EditorGUILayout.EndHorizontal();
                    Title.GetArrayElementAtIndex(i).stringValue = EditorGUILayout.TextField(Title.GetArrayElementAtIndex(i).stringValue);
                    Description.GetArrayElementAtIndex(i).stringValue = EditorGUILayout.TextArea(Description.GetArrayElementAtIndex(i).stringValue, m_DescriptionStyle);

                    EditorGUILayout.BeginHorizontal();
                    if (i != 0 && GUILayout.Button(LC.Combine("MoveUp")))
                    {
                        Mark.MoveArrayElement(i, i - 1);
                        Enabled.MoveArrayElement(i, i - 1);
                        Progress.MoveArrayElement(i, i - 1);
                        Title.MoveArrayElement(i, i - 1);
                        Description.MoveArrayElement(i, i - 1);
                    }
                    if (i != (TaskCount.intValue - 1) && GUILayout.Button(LC.Combine("MoveDown")))
                    {
                        Mark.MoveArrayElement(i, i + 1);
                        Enabled.MoveArrayElement(i, i + 1);
                        Progress.MoveArrayElement(i, i + 1);
                        Title.MoveArrayElement(i, i + 1);
                        Description.MoveArrayElement(i, i + 1);
                    }
                    if (GUILayout.Button(LC.Combine("Delete", "Task"), m_DeleteStyle))
                    {
                        Mark.DeleteArrayElementAtIndex(i);
                        Enabled.DeleteArrayElementAtIndex(i);
                        Progress.DeleteArrayElementAtIndex(i);
                        Title.DeleteArrayElementAtIndex(i);
                        Description.DeleteArrayElementAtIndex(i);
                        TaskCount.intValue--;
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
            GUIStyle _style = mark ? m_TaskTitleMarkStyle : m_TaskTitleStyle;

            _style.hover.textColor = m_ContentColors[index];
            _style.normal.textColor = m_ContentColors[index];
            _style.active.textColor = m_ContentColors[index];
            _style.focused.textColor = m_ContentColors[index];
            _style.onHover.textColor = m_ContentColors[index];
            _style.onNormal.textColor = m_ContentColors[index];
            _style.onFocused.textColor = m_ContentColors[index];

            return _style;
        }
    }
}
