/*
 * ================================================
 * Describe:        This script is used to create radar map inspector.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2023-02-06 16:18:25
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-19 19:06:59
 * ScriptVersion:   0.1
 * ================================================
 */

using EasyFramework.Managers.Ui;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.UI
{
    /// <summary>
    /// RadarMap 组件自定义 Inspector，支持顶点数、弧度与逐顶点占比编辑。
    /// <para>Custom inspector for RadarMap: vertex count, radian and per-vertex percentage editing.</para>
    /// </summary>
    /// <remarks>
    /// 数值的合法性钳制统一由 RadarMap.OnValidate 负责，此处只负责呈现字段，避免规则分裂在两处。
    /// <para>Value clamping is centralized in RadarMap.OnValidate; this editor only presents fields.</para>
    /// </remarks>
    [CustomEditor(typeof(RadarMap), true)]
    [CanEditMultipleObjects]
    public class RadarMapEdit : UnityEditor.UI.ImageEditor
    {
        private SerializedProperty _vertexCount;
        private SerializedProperty _minDistance;
        private SerializedProperty _eachPercent;
        private SerializedProperty _initialRadian;

        protected override void OnEnable()
        {
            base.OnEnable();
            _vertexCount = serializedObject.FindProperty("_vertexCount");
            _minDistance = serializedObject.FindProperty("_minDistance");
            _eachPercent = serializedObject.FindProperty("_eachPercent");
            _initialRadian = serializedObject.FindProperty("_initialRadian");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            // 字段上的 HeaderPro 会绘制分组标题，顺序与 RadarMap.cs 字段声明一致
            EditorGUILayout.PropertyField(_vertexCount, new GUIContent(LC.Combine("Ui.RadarMapVertexCount")));
            EditorGUILayout.PropertyField(_minDistance, new GUIContent(LC.Combine("Ui.MinDistanceLimit")));
            EditorGUILayout.PropertyField(_eachPercent, new GUIContent(LC.Combine("Ui.VertexToCenterDistance")), true);
            EditorGUILayout.PropertyField(_initialRadian, new GUIContent(LC.Combine("Radian")));

            if (serializedObject.ApplyModifiedProperties())
                serializedObject.Update();
        }
    }
}
