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
        ScrollRectPro m_Pro;
        public override void OnInspectorGUI()
        {
            m_Pro = target as ScrollRectPro;

            EditorGUILayout.LabelField(new GUIContent("Importance Notice", "重要提示"), new GUIContent("Please initialize with code", "请在代码中初始化"));
            EditorGUILayout.Space(6f);
            m_Pro.content = (RectTransform)EditorGUILayout.ObjectField(new GUIContent("Content", "滑动元素"), m_Pro.content, typeof(RectTransform), true);
            m_Pro.direction = (ScrollRectPro.Direction)EditorGUILayout.EnumPopup(new GUIContent("Direction", "滚动方向"), m_Pro.direction);
            m_Pro.Lines = EditorGUILayout.IntField(new GUIContent(m_Pro.direction == ScrollRectPro.Direction.Vertical ? "Column Count" : "Row Count", "数量"), m_Pro.Lines);
            m_Pro.movementType = (ScrollRectPro.MovementType)EditorGUILayout.EnumPopup(new GUIContent("Movement Type", "当内容超出其容器的限制时要使用的行为的设置"), m_Pro.movementType);
            m_Pro.Elasticity = EditorGUILayout.FloatField(new GUIContent("Elasticity", "当内容超出滚动矩形时使用的弹性量"), m_Pro.Elasticity);
            m_Pro.Inertia = EditorGUILayout.Toggle(new GUIContent("Inertia", "移动惯性"), m_Pro.Inertia);
            m_Pro.DecelerationRate = EditorGUILayout.FloatField(new GUIContent("Deceleration Rate", "运动减慢的速度\n仅在启用惯性时使用"), m_Pro.DecelerationRate);
            m_Pro.ScrollSensitivity = EditorGUILayout.FloatField(new GUIContent("Scroll Sensitivity", "滚轮和跟踪垫滚轮事件的灵敏度"), m_Pro.ScrollSensitivity);
            m_Pro.Spacing = EditorGUILayout.Vector2IntField(new GUIContent("Spacing", "水平和垂直间距"), m_Pro.Spacing);
            m_Pro.Elemental = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Elemental", "可以滚动的内容元素"), m_Pro.Elemental, typeof(GameObject), true);
        }
    }
}
