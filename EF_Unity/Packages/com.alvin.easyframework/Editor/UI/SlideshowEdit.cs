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
        Slideshow _slideshow;
        SerializedProperty _canDrag;
        SerializedProperty _spacing;
        SerializedProperty _autoLoop;
        SerializedProperty _moveAxis;
        SerializedProperty _spacingTime;
        SerializedProperty _elementSize;
        SerializedProperty _loopDirection;
        SerializedProperty _loopSpaceTime;

        private void OnEnable()
        {
            _slideshow = target as Slideshow;
            _canDrag = serializedObject.FindProperty("_canDrag");
            _spacing = serializedObject.FindProperty("_spacing");
            _autoLoop = serializedObject.FindProperty("_autoLoop");
            _moveAxis = serializedObject.FindProperty("_moveAxis");
            _spacingTime = serializedObject.FindProperty("_spacingTime");
            _elementSize = serializedObject.FindProperty("_elementSize");
            _loopSpaceTime = serializedObject.FindProperty("_loopSpaceTime");
            _loopDirection = serializedObject.FindProperty("_loopDirection");
        }

        public override void OnInspectorGUI() 
        {
            _elementSize.vector2Value = EditorGUILayout.Vector2Field(LC.Combine(new Lc[] { Lc.Element, Lc.Size }), _elementSize.vector2Value);
            _spacing.vector2Value = EditorGUILayout.Vector2Field(LC.Combine(Lc.Spacing), _spacing.vector2Value);
            EditorGUILayout.Space();
            _moveAxis.enumValueFlag = (int)(AxisType)EditorGUILayout.EnumPopup(LC.Combine(new Lc[] { Lc.Loop, Lc.Axis }), (AxisType)_moveAxis.enumValueFlag);

            EditorGUILayout.Space();
            _autoLoop.boolValue = EditorGUILayout.Toggle(LC.Combine(new Lc[] { Lc.Auto, Lc.Loop }), _autoLoop.boolValue);
            if (_autoLoop.boolValue)
            {
                _loopDirection.enumValueFlag = (int)(LoopDirectionType)EditorGUILayout.EnumPopup(LC.Combine(new Lc[] { Lc.Loop, Lc.Direction}), (LoopDirectionType)_loopDirection.enumValueFlag);
                _loopSpaceTime.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(LC.Combine(new Lc[] { Lc.Loop, Lc.Spacing, Lc.Time }), _loopSpaceTime.floatValue), 0f, float.MaxValue);
                _spacingTime.intValue = EditorGUILayout.IntSlider(LC.Combine(new Lc[] { Lc.Spacing, Lc.Time }), _spacingTime.intValue, 0, 60000);
            }

            EditorGUILayout.Space();
            _canDrag.boolValue = EditorGUILayout.Toggle(LC.Combine(new Lc[] { Lc.Can, Lc.Drag }), _canDrag.boolValue);

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
