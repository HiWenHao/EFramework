/* 
 * ================================================
 * Describe:      This script is used to expand the transform inspector panel.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-15 09:35:46
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-15 09:35:46
 * ScriptVersion: 0.1
 * ===============================================
*/
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.InspectorExpand
{
    /// <summary>
    /// Expand the transform inspector panel
    /// </summary>
    [CustomEditor(typeof(Transform))]
    public class TransformExpand : DecoratorEditorBase
    {
        private const float m_ButtonSize = 22.0f;

        private static GUIContent m_Wrold;
        private static GUIContent m_RefreshBtn;
        private static GUIStyle m_RefreshBtnStyle;

        private SerializedProperty m_position;
        private SerializedProperty m_rotation;
        private SerializedProperty m_scale;
        public TransformExpand(): base("TransformInspector")
        { }

        public void OnEnable()
        {
            m_position = serializedObject.FindProperty("m_LocalPosition");
            m_rotation = serializedObject.FindProperty("m_LocalRotation");
            m_scale = serializedObject.FindProperty("m_LocalScale");
        }

        public override void OnInspectorGUI()
        {
            if (m_RefreshBtn == null)
            {
                m_Wrold = EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow");
                m_Wrold.tooltip = "The world position\nUnalterable";
                m_RefreshBtn = EditorGUIUtility.IconContent("Refresh");
                m_RefreshBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset()
                };            
            }

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(m_ButtonSize));
                m_RefreshBtn.tooltip = "Reset local position";
                if (GUILayout.Button(m_RefreshBtn, m_RefreshBtnStyle, GUILayout.Width(m_ButtonSize), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    m_position.vector3Value = Vector3.zero;
                m_RefreshBtn.tooltip = "Reset local rotation";
                if (GUILayout.Button(m_RefreshBtn, m_RefreshBtnStyle, GUILayout.Width(m_ButtonSize), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    m_rotation.quaternionValue = Quaternion.identity;
                m_RefreshBtn.tooltip = "Reset local scale";
                if (GUILayout.Button(m_RefreshBtn, m_RefreshBtnStyle, GUILayout.Width(m_ButtonSize), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    m_scale.vector3Value = Vector3.one;
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                base.OnInspectorGUI();
                EditorGUILayout.EndVertical();

                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(-3);
            if (((Transform)target).parent)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(m_Wrold, m_RefreshBtnStyle, GUILayout.Width(m_ButtonSize), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.Vector3Field(new GUIContent("World Position")
                {
                    tooltip = "The world position of this GameObject.\nThis value unalterable"
                }, ((Transform)target).position); 
                GUILayout.EndHorizontal();
            }
        }
    }
}
