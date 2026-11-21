/*
 * ================================================
 * Describe:        This script is used to create radar map with iamge.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2023-02-06 16:08:49
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-19 19:06:59
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Ui
{
    /// <summary>
    /// 用 Image 绘制的雷达图，顶点到中心的距离由百分比驱动。
    /// <para>Radar map drawn with Image; each vertex distance is driven by a percentage.</para>
    /// </summary>
    [AddComponentMenu("UI/Radar Map", 104)]
    [RequireComponent(typeof(CanvasRenderer))]
    public class RadarMap : Image
    {
        /// <summary>
        /// 最少顶点数（少于三条边无法构成多边形）。
        /// <para>Minimum vertex count; fewer than three cannot form a polygon.</para>
        /// </summary>
        public const int MinVertexCount = 3;

        // 一整圈的弧度，用于角度钳制与均分
        private const float FullRadian = Mathf.PI * 2f;

        // 半径除数：取较短边的一半
        private const float RadiusDivisor = 2f;

        // 新增顶点的默认占比（撑满到外圈）
        private const float DefaultPercent = 1f;

        private RadarMap()
        {
        }

        // 顶点数量，始终 >= MinVertexCount
        [HeaderPro("基础", "Basic", HeaderProType.Title)] [SerializeField]
        private int _vertexCount = MinVertexCount;

        // 顶点到中心的最小距离（只做下限钳制，上限在绘制时按当前半径收敛）
        [HeaderPro("内圈半径", "Inner Radius", HeaderProType.Title)] [SerializeField]
        private float _minDistance;

        // 每个顶点在内圈与外圈之间的插值比例 [0,1]
        [HeaderPro("顶点占比", "Vertex Ratios", HeaderProType.Title)] [SerializeField]
        private float[] _eachPercent;

        // 起始弧度，决定第一个顶点的角度
        [HeaderPro("起始弧度", "Initial Radian", HeaderProType.Title)] [SerializeField]
        private float _initialRadian;

        // 缓存的 RectTransform，避免每次取半径都 GetComponent
        private RectTransform _selfTransform;

        // 单位方向缓存（cos/sin），仅在顶点数或起始弧度变化时重建
        private Vector2[] _unitDirections;
        private int _cachedDirectionCount = -1;
        private float _cachedInitialRadian = float.NaN;

        /// <summary>
        /// 顶点到中心的最小距离，钳制为不小于 0。
        /// <para>Minimum vertex distance from center, clamped to be non-negative.</para>
        /// </summary>
        public float MinDistance
        {
            get => _minDistance;
            set
            {
                float next = Mathf.Max(0f, value);
                if (Mathf.Approximately(_minDistance, next)) return;
                _minDistance = next;
                ChangedAndUpdate();
            }
        }

        /// <summary>
        /// 最大半径，取 RectTransform 较短边的一半，随尺寸实时变化。
        /// <para>Maximum radius: half of the shorter RectTransform side, updated with size.</para>
        /// </summary>
        public float MaxDistance => GetEffectiveRadius();

        /// <summary>
        /// 顶点数量（始终 >= MinVertexCount）。
        /// <para>Vertex count (always >= MinVertexCount).</para>
        /// </summary>
        public int VertexCount
        {
            get => _vertexCount;
            set
            {
                int next = Mathf.Max(MinVertexCount, value);
                if (_vertexCount == next) return;
                _vertexCount = next;
                EnsurePercentArray();
                ChangedAndUpdate();
            }
        }

        /// <summary>
        /// 取得 RectTransform，首次访问时缓存。
        /// <para>Get the RectTransform, cached on first access.</para>
        /// </summary>
        private RectTransform SelfTransform
        {
            get
            {
                if (_selfTransform == null)
                    _selfTransform = GetComponent<RectTransform>();
                return _selfTransform;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _vertexCount = Mathf.Max(MinVertexCount, _vertexCount);
            EnsurePercentArray();
            for (int i = 0; i < _eachPercent.Length; i++)
                _eachPercent[i] = Mathf.Clamp01(_eachPercent[i]);
            _minDistance = Mathf.Max(0f, _minDistance);
            _initialRadian = Mathf.Clamp(_initialRadian, 0f, FullRadian);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float radius = GetEffectiveRadius();
            float minRadius = Mathf.Min(_minDistance, radius);
            EnsureDirections();

            // 中心顶点采样贴图中心，外圈顶点按方向映射到半径 0.5 的圆，使 sprite/渐变完整铺满雷达形状
            const float Center = 0.5f;
            vh.AddVert(Vector3.zero, color, new Vector2(Center, Center));
            for (int i = 0; i < _vertexCount; i++)
            {
                float distance = Mathf.Lerp(minRadius, radius, _eachPercent[i]);
                Vector2 direction = _unitDirections[i];
                Vector2 uv = new Vector2(Center, Center) + direction * Center;
                vh.AddVert(new Vector3(direction.x * distance, direction.y * distance, 0f), color, uv);
            }

            // 扇形三角化：中心点 0 与各相邻顶点两两成三角形
            for (int i = 0; i < _vertexCount - 1; i++)
                vh.AddTriangle(0, i + 1, i + 2);
            vh.AddTriangle(0, _vertexCount, 1);
        }

        /// <summary>
        /// 按 RectTransform 当前尺寸计算有效半径（较短边的一半）。
        /// <para>Calculate effective radius from current RectTransform size (half of shorter side).</para>
        /// </summary>
        private float GetEffectiveRadius()
        {
            RectTransform rect = SelfTransform;
            if (rect == null) return 0f;
            return Mathf.Min(rect.rect.width, rect.rect.height) / RadiusDivisor;
        }

        /// <summary>
        /// 确保 _eachPercent 长度与顶点数一致，扩容部分填默认值。
        /// <para>Ensure _eachPercent length matches vertex count, filling new slots with default.</para>
        /// </summary>
        private void EnsurePercentArray()
        {
            if (_eachPercent != null && _eachPercent.Length == _vertexCount) return;
            float[] next = new float[_vertexCount];
            int copyCount = _eachPercent == null ? 0 : Mathf.Min(_eachPercent.Length, _vertexCount);
            if (copyCount > 0)
                Array.Copy(_eachPercent, next, copyCount);
            for (int i = copyCount; i < _vertexCount; i++)
                next[i] = DefaultPercent;
            _eachPercent = next;
        }

        /// <summary>
        /// 仅在顶点数或起始弧度变化时重建单位方向缓存，避免每次网格重建都算三角函数。
        /// <para>Rebuild unit-direction cache only when vertex count or initial radian changes.</para>
        /// </summary>
        private void EnsureDirections()
        {
            if (_unitDirections != null && _cachedDirectionCount == _vertexCount &&
                Mathf.Approximately(_cachedInitialRadian, _initialRadian))
                return;

            _unitDirections = new Vector2[_vertexCount];
            float delta = FullRadian / _vertexCount;
            float radian = _initialRadian;
            for (int i = 0; i < _vertexCount; i++)
            {
                _unitDirections[i] = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
                radian += delta;
            }

            _cachedDirectionCount = _vertexCount;
            _cachedInitialRadian = _initialRadian;
        }

        /// <summary>
        /// 设置指定顶点的占比（0=内圈，1=外圈），越界索引直接忽略。
        /// <para>Set one vertex percentage (0=inner, 1=outer); out-of-range index is ignored.</para>
        /// </summary>
        /// <param name="index">顶点索引（0 到 VertexCount-1）<para>Vertex index (0 to VertexCount-1)</para></param>
        /// <param name="value">占比，钳制到 [0,1]<para>Percentage, clamped to [0,1]</para></param>
        /// <param name="forceUpdate">是否立即重建网格<para>Whether to rebuild the mesh immediately</para></param>
        public void ChangedInfoByIndex(int index, float value, bool forceUpdate = false)
        {
            if (index < 0 || index >= _vertexCount) return;
            _eachPercent[index] = Mathf.Clamp01(value);
            if (forceUpdate)
                ChangedAndUpdate();
        }

        /// <summary>
        /// 标记网格为脏以触发重建。
        /// <para>Mark the mesh dirty to trigger a rebuild.</para>
        /// </summary>
        public void ChangedAndUpdate()
        {
            SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _vertexCount = Mathf.Max(MinVertexCount, _vertexCount);
            EnsurePercentArray();
            for (int i = 0; i < _eachPercent.Length; i++)
                _eachPercent[i] = Mathf.Clamp01(_eachPercent[i]);
            _minDistance = Mathf.Max(0f, _minDistance);
            _initialRadian = Mathf.Clamp(_initialRadian, 0f, FullRadian);
            // 尺寸可能刚改动，方向与半径缓存需在下次绘制时刷新
            _cachedDirectionCount = -1;
            SetVerticesDirty();
        }
#endif
    }
}
