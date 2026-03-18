/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-06 16:18:25
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-07-03 15:45:25
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
    [CustomEditor(typeof(RadarMap), true)]
    [CanEditMultipleObjects]
    public class RadarMapEdit : UnityEditor.UI.ImageEditor
    {
        SerializedProperty _vertexCount;
        SerializedProperty _minDistance;
        SerializedProperty _maxDistance;
        SerializedProperty _eachPercent;
        SerializedProperty _initialRadian;

        protected override void OnEnable()
        {
            base.OnEnable();
            _vertexCount = serializedObject.FindProperty("_vertexCount");
            _minDistance = serializedObject.FindProperty("_minDistance");
            _maxDistance = serializedObject.FindProperty("_maxDistance");
            _eachPercent = serializedObject.FindProperty("_eachPercent");
            _initialRadian = serializedObject.FindProperty("_initialRadian");
            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            EditorGUILayout.Space();

            _minDistance.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(LC.Combine(new Lc[] { Lc.Min, Lc.Distance, Lc.Limit }), _minDistance.floatValue), 0f, _maxDistance.floatValue);
            EditorGUILayout.Space();

            _vertexCount.intValue = Mathf.Clamp(EditorGUILayout.IntField(LC.Combine(new Lc[] { Lc.RadarMap, Lc.Vertex, Lc.Count }), _vertexCount.intValue), 3, int.MaxValue);
            EditorGUILayout.Space();

            //EditorGUILayout.PropertyField(m_eachPercent, new GUIContent(LC.Combine("Vertex" ,"To" ,"Center" ,"Of" ,"Distance")), true);
            EditorGUILayout.PropertyField(_eachPercent, new GUIContent(LC.Combine(new Lc[] { Lc.Vertex, Lc.To, Lc.Center, Lc.Of, Lc.Distance })), true);
            EditorGUILayout.Space();

            _initialRadian.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(LC.Combine(Lc.Radian), _initialRadian.floatValue), 0f, 6.2832f);

            if (GUI.changed)
            {
                for (int i = _eachPercent.arraySize - 1; i >= 0; i--)
                {
                    float floatValue = _eachPercent.GetArrayElementAtIndex(i).floatValue;
                    _eachPercent.GetArrayElementAtIndex(i).floatValue = Mathf.Clamp01(floatValue);
                }
                if (_eachPercent.arraySize != _vertexCount.intValue)
                    _eachPercent.arraySize = _vertexCount.intValue;

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty((RadarMap)target);
            }
        }
    }
}
