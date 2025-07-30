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
                return _minDistance;
            }
            set
            {
                if (_minDistance != value)
                {
                    _minDistance = Mathf.Clamp(value, 0.0f, _maxDistance);
                    ChangedAndUpdate();
                }
            }
        }

        /// <summary>
        /// The max distance of a vertex from the center.
        /// <para>顶点到中心的最大距离</para>
        /// </summary>
        public float MaxDistance => _maxDistance;

        /// <summary>
        /// Update the number of vertices of the radar map.
        /// <para>雷达图的顶点数</para>
        /// </summary>
        public int VertexCount 
        { 
            get
            {
                return _vertexCount;
            }
            set
            {
                if (_vertexCount != value)
                {
                    float[] tempArray = new float[value];
                    if (_vertexCount > value)
                    {
                        Array.Copy(_eachPercent, tempArray, value);
                        _eachPercent = tempArray;
                    }
                    else
                    {
                        Array.Copy(_eachPercent, tempArray, _vertexCount);
                        for (int i = _vertexCount; i < value; i++)
                            tempArray[i] = 0;

                        _eachPercent = tempArray;
                    }
                    _vertexCount = value;
                    ChangedAndUpdate();
                }
            }
        }

        [SerializeField]
        private int _vertexCount = 3;
        [SerializeField]
        private float _minDistance;
        [SerializeField]
        private float _maxDistance;
        [SerializeField]
        private float _initialRadian = 0;
        [SerializeField]
        private float[] _eachPercent;

        private Vector3[] _innerPositions;//雷达图最内圈的点
        private Vector3[] _exteriorPositions;//雷达图最外圈的点
        RectTransform _selfTransform;
        protected override void Awake()
        {
            if (null == _eachPercent || _eachPercent.Length != _vertexCount)
            {
                _eachPercent = new float[_vertexCount];
                for (int i = 0; i < _vertexCount; i++)
                {
                    _eachPercent[i] = 1.0f;
                }
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();//清除原信息

            if (null == _selfTransform)
            {
                _selfTransform = GetComponent<RectTransform>();
                _maxDistance = (_selfTransform.sizeDelta.x <= _selfTransform.sizeDelta.y ? _selfTransform.sizeDelta.x : _selfTransform.sizeDelta.y) / 2.0f;
            }

            _innerPositions = new Vector3[_vertexCount];
            _exteriorPositions = new Vector3[_vertexCount];
            float tempRadian = _initialRadian;
            float radiamDelta = 2 * Mathf.PI / _vertexCount;

            vh.AddVert(Vector3.zero, color, Vector2.zero);
            for (int i = 0; i < _vertexCount; i++)
            {
                _innerPositions[i] = new Vector3(_minDistance * Mathf.Cos(tempRadian), _minDistance * Mathf.Sin(tempRadian), 0);
                _exteriorPositions[i] = new Vector3(_maxDistance * Mathf.Cos(tempRadian), _maxDistance * Mathf.Sin(tempRadian), 0);
                tempRadian += radiamDelta;

                //通过在最内点和最外点间差值得到雷达图顶点实际位置，并添加到为vh的顶点。由于并没有图案，最后一项的uv坐标就随便填了。
                vh.AddVert(Vector3.Lerp(_innerPositions[i], _exteriorPositions[i], _eachPercent[i]), color, Vector2.zero);
            }

            for (int i = 0; i < _vertexCount - 1; i++)
            {
                vh.AddTriangle(0, i + 1, i + 2);
            }
            vh.AddTriangle(0, _vertexCount, 1);
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
            if (index >= _vertexCount)
                return;
            _eachPercent[index] = value;

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
