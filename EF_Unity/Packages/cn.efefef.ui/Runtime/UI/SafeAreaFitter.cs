/*
 * ================================================
 * Describe:        Adapts a RectTransform to the device safe area.
 * Author:          Alvin5100
 * CreationTime:    2026-11-23 18:45:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-23 18:51:52
 * ScriptVersion:   0.1
 * ===============================================
 */

using UnityEngine;

namespace EasyFramework.Ui
{
    /// <summary>
    /// 安全区适配器：把 RectTransform 的锚点限制在 Screen.safeArea 内，避开刘海、挖孔与手势条。
    /// <para>Fits a RectTransform into Screen.safeArea to avoid notches, cutouts and gesture bars.</para>
    /// </summary>
    /// <remarks>
    /// 只改锚点与偏移，不改缩放，因此子节点布局会自动跟随。
    /// 屏幕尺寸或安全区变化时（旋转屏幕、分屏）在 Update 中自动重新计算。
    /// <para>Only anchors and offsets are changed, so child layouts follow automatically.</para>
    /// </remarks>
    [AddComponentMenu("UI/Safe Area Fitter", 105)]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        /// <summary>是否随屏幕变化自动刷新 <para>Whether to refresh automatically on screen changes</para></summary>
        [SerializeField] private bool autoRefresh = true;

        //  全屏基准锚点：禁用或安全区无效时恢复为全屏拉伸
        private static readonly Vector2 FullMinAnchor = Vector2.zero;
        private static readonly Vector2 FullMaxAnchor = Vector2.one;

        private RectTransform _rect;
        private Vector2 _lastScreenSize;
        private Rect _lastSafeArea;
        private bool _applied; //  当前是否已应用安全区

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnDisable()
        {
            Restore();
        }

        private void Update()
        {
            if (!autoRefresh)
                return;

            //  仅在屏幕尺寸或安全区变化时重算，避免每帧写 Transform
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            if (screen == _lastScreenSize && Screen.safeArea == _lastSafeArea)
                return;

            Apply();
        }

        /// <summary>
        /// 立即按当前安全区刷新锚点
        /// <para>Immediately refreshes anchors according to the current safe area</para>
        /// </summary>
        public void Apply()
        {
            if (_rect == null)
                _rect = (RectTransform)transform;

            Rect safe = Screen.safeArea;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            _lastSafeArea = safe;
            _lastScreenSize = screen;

            //  非全屏或零尺寸时退回原始拉伸，避免除零
            if (screen.x <= 0f || screen.y <= 0f || safe.width <= 0f || safe.height <= 0f)
            {
                Restore();
                return;
            }

            Vector2 min = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
            Vector2 max = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
            _applied = true;
        }

        /// <summary>
        /// 恢复为启用前的全屏锚点与偏移
        /// <para>Restores the original full-screen anchors and offsets</para>
        /// </summary>
        public void Restore()
        {
            if (_rect == null || !_applied)
                return;

            _rect.anchorMin = FullMinAnchor;
            _rect.anchorMax = FullMaxAnchor;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
            _applied = false;
        }
    }
}