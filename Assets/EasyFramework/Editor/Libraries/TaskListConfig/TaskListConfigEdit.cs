/* 
 * ================================================
 * Describe:      This script is used to show the user need to do list. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-15 16:22:01
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-15 16:22:01
 * ScriptVersion: 0.1
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
        SerializedProperty TaskCount;
        SerializedProperty Enabled;
        SerializedProperty Progress;
        SerializedProperty Title;
        SerializedProperty Description;

        GUIStyle m_HeadStyle;
        GUIStyle m_TaskTitleStyle;
        GUIStyle m_DeleteStyle;
        GUIContent[] UIContents;
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

            m_TaskTitleStyle = new GUIStyle("MiniPopup");//1.Titlebar Foldout      4.MiniPopup

            UIContents = new GUIContent[] { new GUIContent("Doing", "正在做"), new GUIContent("Done", "已完成"), new GUIContent("Timeout", "超时"), new GUIContent("Abandon", "遗弃") };
        }

        private void OnEnable()
        {
            TaskCount = serializedObject.FindProperty("TaskCount");
            Enabled = serializedObject.FindProperty("Enabled");
            Progress = serializedObject.FindProperty("Progress");
            Title = serializedObject.FindProperty("Title");
            Description = serializedObject.FindProperty("Description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(new GUIContent("Task List", "任务清单"), m_HeadStyle);

            ShowListInfo();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            if (GUILayout.Button(new GUIContent("+Add Task", "增加任务")))
            {
                Enabled.InsertArrayElementAtIndex(TaskCount.intValue);
                Progress.InsertArrayElementAtIndex(TaskCount.intValue);
                Title.InsertArrayElementAtIndex(TaskCount.intValue);
                Description.InsertArrayElementAtIndex(TaskCount.intValue);
                Enabled.GetArrayElementAtIndex(TaskCount.intValue).boolValue = true;
                Progress.GetArrayElementAtIndex(TaskCount.intValue).intValue = 0;
                Title.GetArrayElementAtIndex(TaskCount.intValue).stringValue = "Title 标题";
                Description.GetArrayElementAtIndex(TaskCount.intValue).stringValue = "Please fill in the task description.请填写任务描述";

                TaskCount.intValue++;
            }
            EditorGUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Remove All", "删除所有任务")))
            {
                if (0 != TaskCount.intValue && EditorUtility.DisplayDialog("Delete Task", "Remove all tasks.\n删除全部任务", "OK"))
                {
                    TaskCount.intValue = 0;
                    Enabled.ClearArray();
                    Title.ClearArray();
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
                Enabled.GetArrayElementAtIndex(i).boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(Enabled.GetArrayElementAtIndex(i).boolValue, $"    {Title.GetArrayElementAtIndex(i).stringValue}", TaskStyle(Progress.GetArrayElementAtIndex(i).intValue));
                if (Enabled.GetArrayElementAtIndex(i).boolValue)
                {
                    EditorGUILayout.BeginVertical("AvatarMappingBox");

                    EditorGUILayout.BeginHorizontal();
                    Progress.GetArrayElementAtIndex(i).intValue = EditorGUILayout.IntPopup(new GUIContent("Progress", "任务进度"), Progress.GetArrayElementAtIndex(i).intValue, UIContents, new int[] { 0, 1, 2, 3 }, TaskStyle(Progress.GetArrayElementAtIndex(i).intValue));
                    GUILayout.Space(15f);
                    if (GUILayout.Button(new GUIContent("Delete Task", "删除任务"), m_DeleteStyle))
                    {
                        Enabled.DeleteArrayElementAtIndex(i);
                        Progress.DeleteArrayElementAtIndex(i);
                        Title.DeleteArrayElementAtIndex(i);
                        Description.DeleteArrayElementAtIndex(i);
                        TaskCount.intValue--;
                        continue;
                    }
                    EditorGUILayout.EndHorizontal();
                    Title.GetArrayElementAtIndex(i).stringValue = EditorGUILayout.TextField(Title.GetArrayElementAtIndex(i).stringValue);
                    Description.GetArrayElementAtIndex(i).stringValue = EditorGUILayout.TextField(Description.GetArrayElementAtIndex(i).stringValue);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        GUIStyle TaskStyle(int index)
        {
            switch (index)
            {
                case 0:
                    m_TaskTitleStyle.normal.textColor = new Color(1f, 0.6f, 0f);
                    break;
                case 1:
                    m_TaskTitleStyle.normal.textColor = Color.green;
                    break;
                case 2:
                    m_TaskTitleStyle.normal.textColor = Color.red;
                    break;
                case 3:
                    m_TaskTitleStyle.normal.textColor = Color.gray;
                    break;
                default:
                    break;
            }
            return m_TaskTitleStyle;
        }
    }
}
