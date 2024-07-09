/* 
 * ================================================
 * Describe:      This script is used to create radar map with iamge. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-06 16:08:49
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-07-03 15:43:49
 * ScriptVersion: 0.2
 * ===============================================
*/
using System;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.UI
{
    /// <summary>
    /// Radar map with image.
    /// </summary>
    [AddComponentMenu("UI/Radar Map", 104)]
    [RequireComponent(typeof(Mask))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class RadarMap : Image
    {
        private RadarMap() { }

        /// <summary>
        /// The min distance of a vertex from the center.
        /// <para>顶点到中心的最小距离</para>
        /// </summary>
        public float MinDistance
        {
            get
            {
                return m_minDistance;
            }
            set
            {
                if (m_minDistance != value)
                {
                    m_minDistance = Mathf.Clamp(value, 0.0f, m_maxDistance);
                    ChangedAndUpdate();
                }
            }
        }

        /// <summary>
        /// The max distance of a vertex from the center.
        /// <para>顶点到中心的最大距离</para>
        /// </summary>
        public float MaxDistance => m_maxDistance;

        /// <summary>
        /// Update the number of vertices of the radar map.
        /// <para>雷达图的顶点数</para>
        /// </summary>
        public int VertexCount 
        { 
            get
            {
                return m_vertexCount;
            }
            set
            {
                if (m_vertexCount != value)
                {
                    float[] _tempArray = new float[value];
                    if (m_vertexCount > value)
                    {
                        Array.Copy(m_EachPercent, _tempArray, value);
                        m_EachPercent = _tempArray;
                    }
                    else
                    {
                        Array.Copy(m_EachPercent, _tempArray, m_vertexCount);
                        for (int i = m_vertexCount; i < value; i++)
                            _tempArray[i] = 0;

                        m_EachPercent = _tempArray;
                    }
                    m_vertexCount = value;
                    ChangedAndUpdate();
                }
            }
        }

        [SerializeField]
        private int m_vertexCount = 3;
        [SerializeField]
        private float m_minDistance;
        [SerializeField]
        private float m_maxDistance;
        [SerializeField]
        private float m_InitialRadian = 0;
        [SerializeField]
        private float[] m_EachPercent;

        private Vector3[] m_innerPositions;//雷达图最内圈的点
        private Vector3[] m_exteriorPositions;//雷达图最外圈的点
        RectTransform m_selfTransform;
        protected override void Awake()
        {
            if (null == m_EachPercent)
            {
                m_EachPercent = new float[m_vertexCount];
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();//清除原信息

            if (null == m_selfTransform)
            {
                m_selfTransform = GetComponent<RectTransform>();
                m_maxDistance = (m_selfTransform.sizeDelta.x <= m_selfTransform.sizeDelta.y ? m_selfTransform.sizeDelta.x : m_selfTransform.sizeDelta.y) / 2.0f;
            }

            m_innerPositions = new Vector3[m_vertexCount];
            m_exteriorPositions = new Vector3[m_vertexCount];
            float _tempRadian = m_InitialRadian;
            float _radiamDelta = 2 * Mathf.PI / m_vertexCount;

            vh.AddVert(Vector3.zero, color, Vector2.zero);
            for (int i = 0; i < m_vertexCount; i++)
            {
                m_innerPositions[i] = new Vector3(m_minDistance * Mathf.Cos(_tempRadian), m_minDistance * Mathf.Sin(_tempRadian), 0);
                m_exteriorPositions[i] = new Vector3(m_maxDistance * Mathf.Cos(_tempRadian), m_maxDistance * Mathf.Sin(_tempRadian), 0);
                _tempRadian += _radiamDelta;

                //通过在最内点和最外点间差值得到雷达图顶点实际位置，并添加到为vh的顶点。由于并没有图案，最后一项的uv坐标就随便填了。
                vh.AddVert(Vector3.Lerp(m_innerPositions[i], m_exteriorPositions[i], m_EachPercent[i]), color, Vector2.zero);
            }

            for (int i = 0; i < m_vertexCount - 1; i++)
            {
                vh.AddTriangle(0, i + 1, i + 2);
            }
            vh.AddTriangle(0, m_vertexCount, 1);
        }

        /// <summary>
        /// Set the value by index.
        /// <para>根据索引更改具体值</para>
        /// </summary>
        /// <param name="index">索引值</param>
        /// <param name="value">具体值</param>
        /// <param name="forceUpdate">Force update, if the one-time change information is more, it is recommended to call the UpdateRadarMap function actively to update.
        /// <para>强制更新，如果一次性更改信息较多，推荐主动调用 UpdateRadarMap 函数更新</para></param>
        public void ChangedInfoByIndex(int index, float value, bool forceUpdate = false)
        {
            if (index >= m_vertexCount)
                return;
            m_EachPercent[index] = value;

            if (forceUpdate)
                ChangedAndUpdate();
        }

        /// <summary>
        /// Update the radar map;
        /// <para>更新雷达图</para>
        /// </summary>
        public void ChangedAndUpdate()
        {
            SetVerticesDirty();
        }
    }
}
