﻿/* 
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
        SerializedProperty m_Scrollbar;
        SerializedProperty m_DockSpeed;
        SerializedProperty m_direction;
        SerializedProperty m_Elasticity;
        SerializedProperty m_AutoDocking;
        SerializedProperty m_HasScrollbar;
        SerializedProperty m_DecelerationRate;

        private void OnEnable()
        {
            m_Pro = (ScrollRectPro)target;
            m_Lines = serializedObject.FindProperty("m_Lines");
            m_Inertia = serializedObject.FindProperty("m_Inertia");
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_MaxCount = serializedObject.FindProperty("m_MaxCount");
            m_Scrollbar = serializedObject.FindProperty("m_Scrollbar");
            m_DockSpeed = serializedObject.FindProperty("m_DockSpeed");
            m_direction = serializedObject.FindProperty("m_direction");
            m_Elasticity = serializedObject.FindProperty("m_Elasticity");
            m_AutoDocking = serializedObject.FindProperty("m_AutoDocking");
            m_HasScrollbar = serializedObject.FindProperty("m_HasScrollbar");
            m_DecelerationRate = serializedObject.FindProperty("m_DecelerationRate");
        }

        public override void OnInspectorGUI()
        {
            m_Pro = target as ScrollRectPro;

            EditorGUILayout.Separator();
            m_Pro.content = (RectTransform)EditorGUILayout.ObjectField(LC.Combine(new Lc[] { Lc.Scrol, Lc.Content }), m_Pro.content, typeof(RectTransform), true);
            m_Pro.Direction = (AxisType)EditorGUILayout.EnumPopup(LC.Combine(new Lc[] { Lc.Scrol, Lc.Direction }), m_Pro.Direction);
            m_Lines.intValue = EditorGUILayout.IntField(LC.Combine(new Lc[] { m_Pro.Direction == AxisType.Vertical ? Lc.Column : Lc.Row, Lc.Count }), m_Lines.intValue);
            m_MaxCount.intValue = EditorGUILayout.IntField(LC.Combine(new Lc[] { Lc.Max, Lc.Element, Lc.Count }), m_MaxCount.intValue);
            EditorGUILayout.Separator();
             
            m_Pro.movementType = (ScrollRectPro.MovementType)EditorGUILayout.EnumPopup(LC.Combine(new Lc[] { Lc.Move, Lc.Type }), m_Pro.movementType);
            m_Elasticity.floatValue = EditorGUILayout.FloatField(LC.Combine(Lc.Elasticity), m_Elasticity.floatValue);
            EditorGUILayout.Separator();

            m_Inertia.boolValue = EditorGUILayout.Toggle(LC.Combine(new Lc[] { Lc.Enable, Lc.Inertia }), m_Inertia.boolValue);
            if (m_Inertia.boolValue)
            {
                m_DecelerationRate.floatValue = EditorGUILayout.FloatField(LC.Combine(new Lc[] { Lc.Deceleration, Lc.Rate} ), m_DecelerationRate.floatValue);
            }
            EditorGUILayout.Separator();

            m_Spacing.vector2IntValue = EditorGUILayout.Vector2IntField(LC.Combine(Lc.Spacing), m_Spacing.vector2IntValue);
            m_Pro.Elemental = (GameObject)EditorGUILayout.ObjectField(LC.Combine(Lc.Element), m_Pro.Elemental, typeof(GameObject), true);
            EditorGUILayout.Separator();

            m_AutoDocking.boolValue = EditorGUILayout.Toggle(LC.Combine(new Lc[] { Lc.Auto, Lc.Dock} ), m_AutoDocking.boolValue);
            if (m_AutoDocking.boolValue)
            {
                m_DockSpeed.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(LC.Combine(new Lc[] { Lc.Dock, Lc.Speed} ), m_DockSpeed.floatValue), 0.1f, float.MaxValue);
            }
            EditorGUILayout.Separator();

            m_HasScrollbar.boolValue = EditorGUILayout.Toggle(LC.Combine(new Lc[] { Lc.Have, Lc.Scrollbar} ), m_HasScrollbar.boolValue);
            if (m_HasScrollbar.boolValue)
            {
                m_Scrollbar.objectReferenceValue = EditorGUILayout.ObjectField(LC.Combine(Lc.Scrollbar), m_Scrollbar.objectReferenceValue, typeof(ScrollbarPro), true);
            }

            if (GUI.changed)
            {
                m_direction.enumValueFlag = (int)m_Pro.Direction;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(m_Pro);
            }
        }
    }
}
