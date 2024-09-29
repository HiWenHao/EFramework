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
        SerializedProperty m_vertexCount;
        SerializedProperty m_minDistance;
        SerializedProperty m_maxDistance;
        SerializedProperty m_eachPercent;
        SerializedProperty m_initialRadian;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_vertexCount = serializedObject.FindProperty("m_vertexCount");
            m_minDistance = serializedObject.FindProperty("m_minDistance");
            m_maxDistance = serializedObject.FindProperty("m_maxDistance");
            m_eachPercent = serializedObject.FindProperty("m_EachPercent");
            m_initialRadian = serializedObject.FindProperty("m_InitialRadian");
            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            m_minDistance.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(LC.Combine(new Lc[] { Lc.Min, Lc.Distance, Lc.Limit }), m_minDistance.floatValue), 0f, m_maxDistance.floatValue);
            EditorGUILayout.Space();

            m_vertexCount.intValue = Mathf.Clamp(EditorGUILayout.IntField(LC.Combine(new Lc[] { Lc.RadarMap, Lc.Vertex, Lc.Count }), m_vertexCount.intValue), 3, int.MaxValue);
            EditorGUILayout.Space();

            //EditorGUILayout.PropertyField(m_eachPercent, new GUIContent(LC.Combine("Vertex" ,"To" ,"Center" ,"Of" ,"Distance")), true);
            EditorGUILayout.PropertyField(m_eachPercent, new GUIContent(LC.Combine(new Lc[] { Lc.Vertex, Lc.To, Lc.Center, Lc.Of, Lc.Distance })), true);
            EditorGUILayout.Space();

            m_initialRadian.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(LC.Combine(Lc.Radian), m_initialRadian.floatValue), 0f, 6.2832f);

            if (GUI.changed)
            {
                for (int i = m_eachPercent.arraySize - 1; i >= 0; i--)
                {
                    float _floatValue = m_eachPercent.GetArrayElementAtIndex(i).floatValue;
                    m_eachPercent.GetArrayElementAtIndex(i).floatValue = Mathf.Clamp01(_floatValue);
                }
                if (m_eachPercent.arraySize != m_vertexCount.intValue)
                    m_eachPercent.arraySize = m_vertexCount.intValue;

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty((RadarMap)target);
            }
        }
    }
}
