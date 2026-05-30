/*
 * ================================================
 * Describe:        自定义进度窗口，用于展示批量操作的执行进度。
 * Author:          Alvin8412
 * CreationTime:    2026-04-15 18:02:04
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-05-30 12:00:00
 * ScriptVersion:   0.6
 * ===============================================
 */

using System;
using EasyFramework.Edit.Windows;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 自定义进度窗口，展示批量操作的执行进度。
    /// 绿色段 = 已确认完成，蓝色段 = 呼吸中的处理尾巴。
    /// <para>用法：<see cref="ShowWindow"/> 打开 → <see cref="UpdateInfo"/> 更新进度 → <see cref="CloseWindow"/> 关闭。</para>
    /// <para>同时只允许存在一个进度窗口；支持取消按钮回调。</para>
    /// </summary>
    public class CustomProgressWindow : EditorWindow
    {
        #region 静态字段

        /// <summary>静态单例引用，同时只允许一个进度窗口。</summary>
        private static CustomProgressWindow _window;

        private const float PingPongSpeed = 0.55f; // 呼吸动画速度（往返周期约 3.6s）
        private const float LerpCatchSpeed = 6f; // 显示进度追真实进度的 lerp 速度
        private const float TailAmplitude = 0.16f; // 固定呼吸振幅（条宽的 16%）
        private const float ConfirmSpeed = 0.5f; // 绿色确认追赶速度（units/sec）
        private const float PingPongMin = 0.04f; // 纯呼吸模式最低位置
        private const float PingPongMax = 0.98f; // 纯呼吸模式最高位置

        private static readonly string[] AnimDots =
        {
            "*-------",
            "-*------",
            "--*-----",
            "---*----",
            "----*---",
            "-----*--",
            "------*-",
            "-------*",
            "------*-",
            "-----*--",
            "----*---",
            "---*----",
            "--*-----",
            "-*------",
        };

        #endregion

        #region 实例字段

        private int _totalCount; // 总任务数
        private int _currentCount; // 当前已完成数（由 UpdateInfo 写入）
        private float _realProgress; // 真实进度（currentCount / totalCount）
        private float _confirmedProgress; // 绿色段视觉位置（平滑追赶 _realProgress）
        private float _displayProgress; // 蓝色尾巴右边缘位置
        private bool _isProcessing; // 是否正在处理中
        private bool _hasRealProgress; // 是否有真实进度上报

        private double _lastRepaintTime; // 上一帧时间戳
        private double _animStartTime; // 动画起始时间戳
        private float _breathAlpha; // 蓝色尾巴当前 alpha（0.45↔1.0 脉冲）
        private Action<bool> _cancelCallback; // 取消按钮回调

        #endregion

        #region 生命周期

        /// <summary>
        /// 每帧 Tick
        /// 蓝尾在 [confirmed, confirmed+amplitude] 间摆动，空间压缩时等比例加速保持视觉速度恒定。
        /// </summary>
        private void OnEditorTick()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastRepaintTime);
            _lastRepaintTime = now;
            if (dt > 0.1f) dt = 0.1f;

            if (_hasRealProgress)
            {
                // 绿色段平滑追赶真实进度
                float confirmStep = ConfirmSpeed * dt;
                _confirmedProgress = Mathf.MoveTowards(_confirmedProgress, _realProgress, confirmStep);

                // 蓝尾可用空间 & 有效振幅
                float available = 1f - _confirmedProgress;
                float effectiveAmplitude = Mathf.Min(TailAmplitude, available);

                // 速度补偿：空间压缩时等比例加速 PingPong 频率
                float speedScale = TailAmplitude / Mathf.Max(effectiveAmplitude, 0.01f);
                float adjustedSpeed = PingPongSpeed * speedScale;
                float breath = Mathf.PingPong((float)(now * adjustedSpeed), 1f);

                // 蓝尾右边缘（永不超过 1.0）
                float target = _confirmedProgress + breath * effectiveAmplitude;

                float step = Mathf.Max(LerpCatchSpeed * dt, 0.01f);
                _displayProgress = Mathf.MoveTowards(_displayProgress, target, step);

                // alpha 脉冲：振幅充足时正常呼吸，压缩时减弱避免闪烁
                float breathForAlpha = Mathf.PingPong((float)(now * PingPongSpeed), 1f);
                float alphaScale = Mathf.Clamp01(effectiveAmplitude / TailAmplitude);
                _breathAlpha = Mathf.Lerp(0.45f, 0.45f + 0.55f * alphaScale, breathForAlpha);
            }
            else
            {
                // 纯呼吸模式：全蓝在 4%~92% 间摆动
                float breath = Mathf.PingPong((float)(now * PingPongSpeed), 1f);
                float target = Mathf.Lerp(PingPongMin, PingPongMax, breath);
                float step = Mathf.Max(LerpCatchSpeed * dt, 0.01f);
                _displayProgress = Mathf.MoveTowards(_displayProgress, target, step);
                _breathAlpha = Mathf.Lerp(0.45f, 1f, breath);
            }

            Repaint();
        }

        /// <summary>
        /// 窗口销毁时触发状态清理与事件退订。
        /// </summary>
        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region GUI 绘制

        /// <summary>
        /// 绘制状态行：本地化文本 + 计数 + 动画点序列。
        /// </summary>
        private void DrawStatusLine()
        {
            double elapsed = EditorApplication.timeSinceStartup - _animStartTime;
            int dotIdx = (int)(elapsed * 6.0) % AnimDots.Length;

            string status;
            if (_hasRealProgress && _totalCount > 0)
                status = $"  {LC.Combine(Lc.PleaseWaitMoment)}  {_currentCount} / {_totalCount} {AnimDots[dotIdx]}";
            else
                status = $"  {LC.Combine(Lc.PleaseWaitMoment)}  {AnimDots[dotIdx]}";

            EditorGUILayout.LabelField(status, GUIUtils.ProgressStatusLabel, GUILayout.Height(20));
        }

        /// <summary>
        /// 绘制双色进度条：绿色段 = 已确认进度，蓝色段 = 呼吸尾巴（alpha 脉冲），无分隔线。
        /// 当确认进度接近完成（≥99.5%）时直接绘制全绿条，消除尾巴残影。
        /// </summary>
        private void DrawProgressBar()
        {
            Rect r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(18));
            float barW = r.width;

            EditorGUI.DrawRect(r, GUIUtils.ProgressBg);

            float confirmedDisplay = Mathf.Clamp01(_confirmedProgress);

            // 近完成 → 全绿，无蓝尾
            if (_hasRealProgress && confirmedDisplay >= 0.995f)
            {
                EditorGUI.DrawRect(new Rect(r.x, r.y, barW, r.height), GUIUtils.ProgressGreen);
                EditorGUI.DrawRect(new Rect(r.x, r.y, barW, 1.5f), GUIUtils.ProgressGreenGlow);
                EditorGUI.DrawRect(new Rect(r.x, r.y, barW, 1.5f), GUIUtils.ProgressHighlight);
                goto drawBorder;
            }

            float filledW = barW * Mathf.Clamp01(_displayProgress);
            if (filledW <= 0f) goto drawBorder;

            float confirmedW = _hasRealProgress ? barW * confirmedDisplay : 0f;

            // 绿色段
            if (confirmedW > 0f)
            {
                EditorGUI.DrawRect(new Rect(r.x, r.y, confirmedW, r.height), GUIUtils.ProgressGreen);
                EditorGUI.DrawRect(new Rect(r.x, r.y, confirmedW, 1.5f), GUIUtils.ProgressGreenGlow);
            }

            // 蓝色段
            float tailW = filledW - confirmedW;
            if (tailW > 0f)
            {
                Color tailColor = GUIUtils.ProgressBlue;
                tailColor.a = _breathAlpha;
                EditorGUI.DrawRect(new Rect(r.x + confirmedW, r.y, tailW, r.height), tailColor);

                Color hlColor = GUIUtils.ProgressBlueGlow;
                hlColor.a = _breathAlpha * 0.6f;
                EditorGUI.DrawRect(new Rect(r.x + confirmedW, r.y, tailW, 1.5f), hlColor);
            }

            // 整条顶部高光
            EditorGUI.DrawRect(new Rect(r.x, r.y, filledW, 1.5f), GUIUtils.ProgressHighlight);

            // 边框
            drawBorder:
            EditorGUI.DrawRect(new Rect(r.x, r.y, barW, 1f), GUIUtils.ProgressBorder);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, barW, 1f), GUIUtils.ProgressBorder);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), GUIUtils.ProgressBorder);
            EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), GUIUtils.ProgressBorder);
        }

        /// <summary>
        /// Unity EditorWindow 主 GUI 入口，绘制状态行、进度条和取消按钮。
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Space(18);

            DrawStatusLine();

            GUILayout.Space(18);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            DrawProgressBar();
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(12);

            if (_cancelCallback != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(LC.Combine(Lc.Cancel), GUILayout.Width(100), GUILayout.Height(24)))
                    CloseWindow();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.FlexibleSpace();
        }

        #endregion

        #region 清理

        /// <summary>
        /// 重置所有状态、触发取消回调、退订 EditorApplication.update。
        /// 窗口关闭 / 外部调用 CloseWindow 时触达。
        /// </summary>
        private void Cleanup()
        {
            if (!_isProcessing)
                return;

            _displayProgress = 0f;
            _confirmedProgress = 0f;
            _realProgress = 0f;
            _hasRealProgress = false;
            _isProcessing = false;
            _cancelCallback?.Invoke(false);
            _cancelCallback = null;
            EditorApplication.update -= OnEditorTick;
        }

        #endregion

        #region 公开 API

        /// <summary>
        /// 打开进度窗口。同时只允许存在一个，已有窗口在运行中时返回 false。
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="cancelCallback">取消按钮回调，用户取消时触发（参数始终为 false）</param>
        /// <returns>true 表示成功打开；false 表示已有窗口运行中或创建失败</returns>
        public static bool ShowWindow(string title, Action<bool> cancelCallback)
        {
            _window = GetWindow<CustomProgressWindow>(true, title);

            if (_window == null)
                return false;

            if (_window._isProcessing)
                return false;

            EditorApplication.update += _window.OnEditorTick;
            _window._cancelCallback = cancelCallback;
            _window._isProcessing = true;
            _window._realProgress = 0f;
            _window._confirmedProgress = 0f;
            _window._displayProgress = 0f;
            _window._hasRealProgress = false;
            _window._lastRepaintTime = EditorApplication.timeSinceStartup;
            _window._animStartTime = EditorApplication.timeSinceStartup;

            // 居中偏上
            var rect = _window.position;
            rect.x = (Screen.width - rect.width) * 0.5f;
            rect.y = (Screen.height - rect.height) * 0.3f;
            _window.position = rect;
            _window.maxSize = new Vector2(450f, 160f);
            _window.minSize = new Vector2(380f, 130f);
            _window.ShowUtility();
            return true;
        }

        /// <summary>
        /// 更新进度信息。调用方在每次任务完成时调用，可多次调用。
        /// </summary>
        /// <param name="currentCount">当前已完成数量，超过 totalCount 时自动截断</param>
        /// <param name="totalCount">全部任务数量，0 表示切回纯呼吸模式</param>
        public static void UpdateInfo(int currentCount, int totalCount)
        {
            if (_window == null)
                return;

            _window._totalCount = totalCount;
            _window._currentCount = currentCount > totalCount ? totalCount : currentCount;
            _window._realProgress = totalCount > 0 ? (float)currentCount / totalCount : 0f;
            _window._hasRealProgress = totalCount > 0;
        }

        /// <summary>
        /// 关闭进度窗口，触发取消回调后释放。
        /// </summary>
        public static void CloseWindow()
        {
            if (_window == null)
                return;

            _window.Close();
            _window = null;
        }

        #endregion
    }
}