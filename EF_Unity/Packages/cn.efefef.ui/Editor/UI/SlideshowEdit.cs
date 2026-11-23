/*
 * ================================================
 * Describe:      Custom inspector for Slideshow component.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-07-03 17:10:01
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-23 17:58:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Ui
{
    /// <summary>
    /// Slideshow 组件自定义 Inspector，支持元素尺寸、间距、自动轮播、拖拽与索引事件设置。
    /// <para>Custom inspector for Slideshow: element size, spacing, auto-loop, drag and index event settings.</para>
    /// </summary>
    [CustomEditor(typeof(Slideshow))]
    [CanEditMultipleObjects]
    public class SlideshowEdit : Editor
    {
        //  切换间隔滑块上限（帧）
        private const int MaxSpacingFrames = 60000;
        //  切换间隔最小帧数，避免除零
        private const int MinSpacingFrames = 1;
        //  索引变化事件显示名（新增语言键需重生成语言表，暂用常量）
        private const string IndexChangedLabel = "索引变化事件";

        private SerializedProperty _canDrag;    //  是否允许拖拽
        private SerializedProperty _spacing;    //  元素间距
        private SerializedProperty _autoLoop;   //  是否自动循环
        private SerializedProperty _moveAxis;   //  移动轴
        private SerializedProperty _spacingTime;    //  缓动帧数
        private SerializedProperty _elementSize;    //  元素尺寸
        private SerializedProperty _loopDirection;  //  循环方向
        private SerializedProperty _loopSpaceTime;  //  自动循环间隔
        private SerializedProperty _onIndexChanged; //  索引变化事件

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
            _onIndexChanged = serializedObject.FindProperty("onIndexChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_elementSize, new GUIContent(LC.Combine("Ui.ElementSize")));
            EditorGUILayout.PropertyField(_spacing, new GUIContent(LC.Combine("Spacing")));
            EditorGUILayout.Space();
            _moveAxis.enumValueIndex =
                (int)(AxisType)EditorGUILayout.EnumPopup(LC.Combine("Ui.LoopAxis"),
                    (AxisType)_moveAxis.enumValueIndex);

            EditorGUILayout.Space();
            _autoLoop.boolValue = EditorGUILayout.Toggle(LC.Combine("Ui.AutoLoop"), _autoLoop.boolValue);
            if (_autoLoop.boolValue)
            {
                _loopDirection.enumValueIndex = (int)(LoopDirectionType)EditorGUILayout.EnumPopup(
                    LC.Combine("Ui.LoopDirection"), (LoopDirectionType)_loopDirection.enumValueIndex);
                _loopSpaceTime.floatValue = Mathf.Max(0f,
                    EditorGUILayout.FloatField(LC.Combine("Ui.LoopSpacingTime"), _loopSpaceTime.floatValue));
                _spacingTime.intValue = EditorGUILayout.IntSlider(LC.Combine("Ui.SpacingTime"),
                    _spacingTime.intValue, MinSpacingFrames, MaxSpacingFrames);
            }

            EditorGUILayout.Space();
            _canDrag.boolValue = EditorGUILayout.Toggle(LC.Combine("Ui.CanDrag"), _canDrag.boolValue);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_onIndexChanged, new GUIContent(IndexChangedLabel), true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
