/* 
 * ================================================
 * Describe:      This script is used to control the scroll view pro.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-03-24 18:44:52
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-03-24 18:44:52
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.UI;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// The scroll view pro edit code.
    /// </summary>
    [CustomEditor(typeof(ScrollRectPro))]
    [CanEditMultipleObjects]
    public class ScrollRectProEdit : Editor
	{
        ScrollRectPro m_Pro;
        SerializedProperty m_Lines;
        SerializedProperty m_Inertia;
        SerializedProperty m_Spacing;
        SerializedProperty m_MaxCount;
        SerializedProperty m_DockSpeed;
        SerializedProperty m_Elasticity;
        SerializedProperty m_AutoDocking;
        SerializedProperty m_DecelerationRate;
        SerializedProperty m_ScrollSensitivity;

        private void OnEnable()
        {
            m_Pro = (ScrollRectPro)target;
            m_Lines = serializedObject.FindProperty("m_Lines");
            m_Inertia = serializedObject.FindProperty("m_Inertia");
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_MaxCount = serializedObject.FindProperty("m_MaxCount");
            m_DockSpeed = serializedObject.FindProperty("m_DockSpeed");
            m_Elasticity = serializedObject.FindProperty("m_Elasticity");
            m_AutoDocking = serializedObject.FindProperty("m_AutoDocking");
            m_DecelerationRate = serializedObject.FindProperty("m_DecelerationRate");
            m_ScrollSensitivity = serializedObject.FindProperty("m_ScrollSensitivity");
        }

        public override void OnInspectorGUI()
        {
            m_Pro = target as ScrollRectPro;

            EditorGUILayout.LabelField(new GUIContent("Importance Notice", "重要提示"), new GUIContent("Please initialize with code", "请在代码中初始化"));
            EditorGUILayout.Separator();
            m_Pro.content = (RectTransform)EditorGUILayout.ObjectField(LC.Combine("Scrol", "Content"), m_Pro.content, typeof(RectTransform), true);
            m_Pro.direction = (AxisType)EditorGUILayout.EnumPopup(LC.Combine("Scrol", "Direction"), m_Pro.direction);
            m_Lines.intValue = EditorGUILayout.IntField(LC.Combine(m_Pro.direction == AxisType.Vertical ? "Column" : "Row", "Count"), m_Lines.intValue);
            m_MaxCount.intValue = EditorGUILayout.IntField(LC.Combine("Max", "Element", "Count"), m_MaxCount.intValue);
            EditorGUILayout.Separator();
             
            m_Pro.movementType = (ScrollRectPro.MovementType)EditorGUILayout.EnumPopup(LC.Combine("Move", "Type"), m_Pro.movementType);
            m_Elasticity.floatValue = EditorGUILayout.FloatField(LC.Combine("Elasticity"), m_Elasticity.floatValue);
            EditorGUILayout.Separator();

            m_Inertia.boolValue = EditorGUILayout.Toggle(LC.Combine("Enable", "Inertia"), m_Inertia.boolValue);
            if (m_Inertia.boolValue)
            {
                m_DecelerationRate.floatValue = EditorGUILayout.FloatField(LC.Combine("Deceleration", "Rate"), m_DecelerationRate.floatValue);
                m_ScrollSensitivity.floatValue = EditorGUILayout.FloatField(LC.Combine("Mouse", "Scrol", "Sensitivity"), m_ScrollSensitivity.floatValue);
            }
            EditorGUILayout.Separator();

            m_Spacing.vector2IntValue = EditorGUILayout.Vector2IntField(LC.Combine("Spacing"), m_Spacing.vector2IntValue);
            m_Pro.Elemental = (GameObject)EditorGUILayout.ObjectField(LC.Combine("Element"), m_Pro.Elemental, typeof(GameObject), true);
            EditorGUILayout.Separator();

            //m_AutoDocking.boolValue = EditorGUILayout.Toggle(LC.Combine("Auto", "Dock"), m_AutoDocking.boolValue);
            //if (m_AutoDocking.boolValue)
            //{
            //    m_DockSpeed.floatValue = EditorGUILayout.FloatField(LC.Combine("Dock", "Speed"), m_DockSpeed.floatValue);
            //}
            if (GUI.changed)
            { 
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(m_Pro);
            }
        }
    }
}
