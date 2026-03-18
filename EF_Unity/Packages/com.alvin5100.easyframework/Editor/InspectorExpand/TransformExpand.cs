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
        private const float _buttonSize = 22.0f;

        private static GUIContent _wrold;
        private static GUIContent _refreshBtn;
        private static GUIStyle _refreshBtnStyle;

        private SerializedProperty _position;
        private SerializedProperty _rotation;
        private SerializedProperty _scale;
        public TransformExpand(): base("TransformInspector")
        { }

        public void OnEnable()
        {
            _position = serializedObject.FindProperty("m_LocalPosition");
            _rotation = serializedObject.FindProperty("m_LocalRotation");
            _scale = serializedObject.FindProperty("m_LocalScale");
        }

        public override void OnInspectorGUI()
        {
            if (_refreshBtn == null)
            {
                _wrold = EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow");
                _wrold.tooltip = "The world position\nUnalterable";
                _refreshBtn = EditorGUIUtility.IconContent("Refresh");
                _refreshBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset()
                };            
            }

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(_buttonSize));
                _refreshBtn.tooltip = "Reset local position";
                if (GUILayout.Button(_refreshBtn, _refreshBtnStyle, GUILayout.Width(_buttonSize), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    _position.vector3Value = Vector3.zero;
                _refreshBtn.tooltip = "Reset local rotation";
                if (GUILayout.Button(_refreshBtn, _refreshBtnStyle, GUILayout.Width(_buttonSize), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    _rotation.quaternionValue = Quaternion.identity;
                _refreshBtn.tooltip = "Reset local scale";
                if (GUILayout.Button(_refreshBtn, _refreshBtnStyle, GUILayout.Width(_buttonSize), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    _scale.vector3Value = Vector3.one;
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
                EditorGUILayout.LabelField(_wrold, _refreshBtnStyle, GUILayout.Width(_buttonSize), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.Vector3Field(new GUIContent("World Position")
                {
                    tooltip = "The world position of this GameObject.\nThis value unalterable"
                }, ((Transform)target).position); 
                GUILayout.EndHorizontal();
            }
        }
    }
}
