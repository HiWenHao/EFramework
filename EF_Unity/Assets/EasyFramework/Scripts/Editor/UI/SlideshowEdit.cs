/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-07-03 17:10:01
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-07-03 17:10:01
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.UI;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    [CustomEditor(typeof(Slideshow))]
    [CanEditMultipleObjects]
    public class SlideshowEdit : Editor
    {
        Slideshow m_slideshow;
        SerializedProperty m_CanDrag;
        SerializedProperty m_Spacing;
        SerializedProperty m_AutoLoop;
        SerializedProperty m_MoveAxis;
        SerializedProperty m_SpacingTime;
        SerializedProperty m_ElementSize;
        SerializedProperty m_LoopDirection;
        SerializedProperty m_LoopSpaceTime;

        private void OnEnable()
        {
            m_slideshow = target as Slideshow;
            m_CanDrag = serializedObject.FindProperty("m_CanDrag");
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_AutoLoop = serializedObject.FindProperty("m_AutoLoop");
            m_MoveAxis = serializedObject.FindProperty("m_MoveAxis");
            m_SpacingTime = serializedObject.FindProperty("m_SpacingTime");
            m_ElementSize = serializedObject.FindProperty("m_ElementSize");
            m_LoopSpaceTime = serializedObject.FindProperty("m_LoopSpaceTime");
            m_LoopDirection = serializedObject.FindProperty("m_LoopDirection");
        }

        public override void OnInspectorGUI() 
        {
            m_ElementSize.vector2Value = EditorGUILayout.Vector2Field(LC.Combine("Element", "Size"), m_ElementSize.vector2Value);
            m_Spacing.vector2Value = EditorGUILayout.Vector2Field(LC.Combine("Spacing"), m_Spacing.vector2Value);
            EditorGUILayout.Space();
            m_MoveAxis.enumValueFlag = (int)(AxisType)EditorGUILayout.EnumPopup(LC.Combine("Loop", "Axis"), (AxisType)m_MoveAxis.enumValueFlag);

            EditorGUILayout.Space();
            m_AutoLoop.boolValue = EditorGUILayout.Toggle(LC.Combine("Auto", "Loop"), m_AutoLoop.boolValue);
            if (m_AutoLoop.boolValue)
            {
                m_LoopDirection.enumValueFlag = (int)(LoopDirectionType)EditorGUILayout.EnumPopup(LC.Combine("Loop", "Direction"), (LoopDirectionType)m_LoopDirection.enumValueFlag);
                m_LoopSpaceTime.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(LC.Combine("Loop", "Spacing", "Time"), m_LoopSpaceTime.floatValue), 0f, float.MaxValue);
                m_SpacingTime.intValue = EditorGUILayout.IntSlider(LC.Combine("Spacing", "Time"), m_SpacingTime.intValue, 0, 60000);
            }

            EditorGUILayout.Space();
            m_CanDrag.boolValue = EditorGUILayout.Toggle(LC.Combine("Can", "Drag"), m_CanDrag.boolValue);

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
