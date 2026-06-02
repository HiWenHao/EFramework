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

using EasyFramework.Managers.Ui;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.UI
{
    /// <summary>
    /// Custom inspector for Slideshow component. Provides element size, spacing, auto-loop and drag settings.
    /// Slideshow 组件自定义 Inspector，支持元素尺寸、间距、自动轮播和拖拽设置。
    /// </summary>
    [CustomEditor(typeof(Slideshow))]
    [CanEditMultipleObjects]
    public class SlideshowEdit : Editor
    {
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
            _elementSize.vector2Value = EditorGUILayout.Vector2Field(LC.Combine(Lc.Element, Lc.Size), _elementSize.vector2Value);
            _spacing.vector2Value = EditorGUILayout.Vector2Field(LC.Combine(Lc.Spacing), _spacing.vector2Value);
            EditorGUILayout.Space();
            _moveAxis.enumValueIndex =
                (int)(AxisType)EditorGUILayout.EnumPopup(LC.Combine(Lc.Loop, Lc.Axis),
                    (AxisType)_moveAxis.enumValueIndex);

            EditorGUILayout.Space();
            _autoLoop.boolValue = EditorGUILayout.Toggle(LC.Combine(Lc.Auto, Lc.Loop), _autoLoop.boolValue);
            if (_autoLoop.boolValue)
            {
                _loopDirection.enumValueIndex = (int)(LoopDirectionType)EditorGUILayout.EnumPopup(
                    LC.Combine(Lc.Loop, Lc.Direction), (LoopDirectionType)_loopDirection.enumValueIndex);
                _loopSpaceTime.floatValue =
                    Mathf.Clamp(
                        EditorGUILayout.FloatField(LC.Combine(Lc.Loop, Lc.Spacing, Lc.Time),
                            _loopSpaceTime.floatValue), 0f, float.MaxValue);
                _spacingTime.intValue = EditorGUILayout.IntSlider(LC.Combine(Lc.Spacing, Lc.Time),
                    _spacingTime.intValue, 0, 60000);
            }

            EditorGUILayout.Space();
            _canDrag.boolValue = EditorGUILayout.Toggle(LC.Combine(Lc.Can, Lc.Drag), _canDrag.boolValue);

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}