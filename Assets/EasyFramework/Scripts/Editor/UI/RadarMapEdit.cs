/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-06 16:18:25
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-06 16:18:25
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
        SerializedProperty _sideCount;
        SerializedProperty _minDistance;
        SerializedProperty _eachPercent;
        SerializedProperty _initialRadian;

        protected override void OnEnable()
        {
            base.OnEnable();
            _sideCount = serializedObject.FindProperty("SideCount");
            _minDistance = serializedObject.FindProperty("MinDistance");
            _eachPercent = serializedObject.FindProperty("EachPercent");
            _initialRadian = serializedObject.FindProperty("InitialRadian");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.PropertyField(_sideCount);
            EditorGUILayout.PropertyField(_minDistance);
            EditorGUILayout.PropertyField(_eachPercent, true);
            EditorGUILayout.PropertyField(_initialRadian);

            RadarMap radar = target as RadarMap;


            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
