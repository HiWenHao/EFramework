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
        ScrollRectPro _pro;
        SerializedProperty _lines;
        SerializedProperty _lnertia;
        SerializedProperty _spacing;
        SerializedProperty _maxCount;
        SerializedProperty _scrollbar;
        SerializedProperty _dockSpeed;
        SerializedProperty _direction;
        SerializedProperty _elasticity;
        SerializedProperty _autoDocking;
        SerializedProperty _hasScrollbar;
        SerializedProperty _decelerationRate;

        private void OnEnable()
        {
            _pro = (ScrollRectPro)target;
            _lines = serializedObject.FindProperty("m_Lines");
            _lnertia = serializedObject.FindProperty("m_Inertia");
            _spacing = serializedObject.FindProperty("_spacing");
            _maxCount = serializedObject.FindProperty("m_MaxCount");
            _scrollbar = serializedObject.FindProperty("m_Scrollbar");
            _dockSpeed = serializedObject.FindProperty("m_DockSpeed");
            _direction = serializedObject.FindProperty("m_direction");
            _elasticity = serializedObject.FindProperty("m_Elasticity");
            _autoDocking = serializedObject.FindProperty("m_AutoDocking");
            _hasScrollbar = serializedObject.FindProperty("m_HasScrollbar");
            _decelerationRate = serializedObject.FindProperty("m_DecelerationRate");
        }

        public override void OnInspectorGUI()
        {
            _pro = target as ScrollRectPro;

            EditorGUILayout.Separator();
            _pro.content = (RectTransform)EditorGUILayout.ObjectField(LC.Combine(new Lc[] { Lc.Scrol, Lc.Content }), _pro.content, typeof(RectTransform), true);
            _pro.Direction = (AxisType)EditorGUILayout.EnumPopup(LC.Combine(new Lc[] { Lc.Scrol, Lc.Direction }), _pro.Direction);
            _lines.intValue = EditorGUILayout.IntField(LC.Combine(new Lc[] { _pro.Direction == AxisType.Vertical ? Lc.Column : Lc.Row, Lc.Count }), _lines.intValue);
            _maxCount.intValue = EditorGUILayout.IntField(LC.Combine(new Lc[] { Lc.Max, Lc.Element, Lc.Count }), _maxCount.intValue);
            EditorGUILayout.Separator();
             
            _pro.movementType = (ScrollRectPro.MovementType)EditorGUILayout.EnumPopup(LC.Combine(new Lc[] { Lc.Move, Lc.Type }), _pro.movementType);
            _elasticity.floatValue = EditorGUILayout.FloatField(LC.Combine(Lc.Elasticity), _elasticity.floatValue);
            EditorGUILayout.Separator();

            _lnertia.boolValue = EditorGUILayout.Toggle(LC.Combine(new Lc[] { Lc.Enable, Lc.Inertia }), _lnertia.boolValue);
            if (_lnertia.boolValue)
            {
                _decelerationRate.floatValue = EditorGUILayout.FloatField(LC.Combine(new Lc[] { Lc.Deceleration, Lc.Rate} ), _decelerationRate.floatValue);
            }
            EditorGUILayout.Separator();

            _spacing.vector2IntValue = EditorGUILayout.Vector2IntField(LC.Combine(Lc.Spacing), _spacing.vector2IntValue);
            _pro.Elemental = (GameObject)EditorGUILayout.ObjectField(LC.Combine(Lc.Element), _pro.Elemental, typeof(GameObject), true);
            EditorGUILayout.Separator();

            _autoDocking.boolValue = EditorGUILayout.Toggle(LC.Combine(new Lc[] { Lc.Auto, Lc.Dock} ), _autoDocking.boolValue);
            if (_autoDocking.boolValue)
            {
                _dockSpeed.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(LC.Combine(new Lc[] { Lc.Dock, Lc.Speed} ), _dockSpeed.floatValue), 0.1f, float.MaxValue);
            }
            EditorGUILayout.Separator();

            _hasScrollbar.boolValue = EditorGUILayout.Toggle(LC.Combine(new Lc[] { Lc.Have, Lc.Scrollbar} ), _hasScrollbar.boolValue);
            if (_hasScrollbar.boolValue)
            {
                _scrollbar.objectReferenceValue = EditorGUILayout.ObjectField(LC.Combine(Lc.Scrollbar), _scrollbar.objectReferenceValue, typeof(ScrollbarPro), true);
            }

            if (GUI.changed)
            {
                _direction.enumValueFlag = (int)_pro.Direction;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_pro);
            }
        }
    }
}
