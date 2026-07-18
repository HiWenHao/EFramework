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

using EasyFramework.Managers.Ui;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.UI
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
        SerializedProperty _content;
        SerializedProperty _movementType;
        SerializedProperty _elemental;

        private void OnEnable()
        {
            _pro = (ScrollRectPro)target;
            _lines = serializedObject.FindProperty("_lines");
            _lnertia = serializedObject.FindProperty("_inertia");
            _spacing = serializedObject.FindProperty("_spacing");
            _maxCount = serializedObject.FindProperty("_maxCount");
            _scrollbar = serializedObject.FindProperty("_scrollbar");
            _dockSpeed = serializedObject.FindProperty("_dockSpeed");
            _direction = serializedObject.FindProperty("_direction");
            _elasticity = serializedObject.FindProperty("_elasticity");
            _autoDocking = serializedObject.FindProperty("_autoDocking");
            _hasScrollbar = serializedObject.FindProperty("_hasScrollbar");
            _decelerationRate = serializedObject.FindProperty("_decelerationRate");
            _content = serializedObject.FindProperty("Content");
            _movementType = serializedObject.FindProperty("movementType");
            _elemental = serializedObject.FindProperty("Elemental");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(_content, new GUIContent(LC.Combine(new Lc[] { Lc.Scroll, Lc.Content })));
            EditorGUILayout.PropertyField(_direction, new GUIContent(LC.Combine(new Lc[] { Lc.Scroll, Lc.Direction })));
            _lines.intValue = EditorGUILayout.IntField(LC.Combine(new Lc[] { (AxisType)_direction.enumValueIndex == AxisType.Vertical ? Lc.Column : Lc.Row, Lc.Count }), _lines.intValue);
            _maxCount.intValue = EditorGUILayout.IntField(LC.Combine(new Lc[] { Lc.Max, Lc.Element, Lc.Count }), _maxCount.intValue);
            EditorGUILayout.Separator();
             
            EditorGUILayout.PropertyField(_movementType, new GUIContent(LC.Combine(new Lc[] { Lc.Move, Lc.Type })));
            _elasticity.floatValue = EditorGUILayout.FloatField(LC.Combine(Lc.Elasticity), _elasticity.floatValue);
            EditorGUILayout.Separator();

            _lnertia.boolValue = EditorGUILayout.Toggle(LC.Combine(new Lc[] { Lc.Enable, Lc.Inertia }), _lnertia.boolValue);
            if (_lnertia.boolValue)
            {
                _decelerationRate.floatValue = EditorGUILayout.FloatField(LC.Combine(new Lc[] { Lc.Deceleration, Lc.Rate} ), _decelerationRate.floatValue);
            }
            EditorGUILayout.Separator();

            _spacing.vector2IntValue = EditorGUILayout.Vector2IntField(LC.Combine(Lc.Spacing), _spacing.vector2IntValue);
            EditorGUILayout.PropertyField(_elemental, new GUIContent(LC.Combine(Lc.Element)));
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

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
