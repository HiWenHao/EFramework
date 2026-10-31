/*
 * ================================================
 * Describe:      EditorWindow 稳定基类，提供 Domain Reload 安全防护、刷新状态监控与异常兜底。
 * Author:        Alvin.Wang(Wenhao)
 * CreationTime:  2026-03-19 18:19:50
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-31 22:55:00
 * ScriptVersion: 0.5
 * ===============================================
 */

using UnityEditor;
using UnityEngine;
using System;

namespace EasyFramework.Edit.Windows
{
    /// <summary>
    /// EditorWindow 稳定基类：Domain Reload 安全防护、刷新/编译遮盖、异常兜底覆盖层（含堆栈查看与复制）。
    /// <para>Stable EditorWindow base: domain-reload safety, refresh/compile overlay and an error overlay with stack trace view.</para>
    /// </summary>
    public abstract class EditorWindowBase : EditorWindow
    {
        #region Fields

        private const int RequiredStableTicks = 3;      // 确认刷新结束需累积的稳定更新数（EditorApplication.update 计数，非渲染帧）

        [SerializeField] private bool isRefreshing;     // 是否处于刷新/编译状态（序列化以在域重载后恢复）
        private int _stableTickCount;                   // 刷新结束后的稳定计数，防止短暂恢复造成误判
        private Exception _lastError;                   // 最近捕获的异常，非 null 时显示错误覆盖层（不序列化，域重载后自动清零）
        private Vector2 _errorScrollPos;                // 错误覆盖层堆栈区滚动位置

        #endregion

        #region Properties

        /// <summary>
        /// 当前是否处于刷新/编译中。
        /// <para>Whether the window is in refresh/compile state.</para>
        /// </summary>
        protected bool IsRefreshing => isRefreshing;

        /// <summary>
        /// 是否处于错误状态（OnSmartGUI 或 LoadWindowData 抛出过异常）。
        /// <para>Whether an error was captured from OnSmartGUI or LoadWindowData.</para>
        /// </summary>
        protected bool HasError => _lastError != null;

        #endregion

        #region Methods

        //EditorApplication.update 回调，检测刷新/编译状态变化并驱动进入/退出刷新流程;
        private void OnEditorUpdate()
        {
            bool currentlyRefreshing = EditorApplication.isUpdating || EditorApplication.isCompiling;

            if (!isRefreshing && currentlyRefreshing)
                EnterRefreshState();

            if (isRefreshing && !currentlyRefreshing)
                HandleRefreshCompletion();
        }

        //首次启用时检测当前是否已在刷新中，决定走"等待刷新完成"还是"直接就绪"路径;
        private void CheckInitialRefreshState()
        {
            isRefreshing = EditorApplication.isUpdating || EditorApplication.isCompiling;

            if (isRefreshing)
            {
                OnRefreshStarted();
            }
            else
            {
                if (SafeLoadWindowData())
                {
                    OnWindowReady();
                }
            }
        }

        //进入刷新状态：保存数据、通知子类、强制重绘;
        private void EnterRefreshState()
        {
            isRefreshing = true;
            _stableTickCount = 0;
            SaveWindowData();
            OnRefreshStarted();
            Repaint();
        }

        //处理刷新完成：累积稳定计数防抖，达到阈值后确认结束并以全新数据重载;
        private void HandleRefreshCompletion()
        {
            _stableTickCount++;

            if (_stableTickCount < RequiredStableTicks)
                return;

            isRefreshing = false;
            _lastError = null; //刷新后重置错误状态，基于重载结果重新判定;

            if (SafeLoadWindowData())
            {
                OnRefreshCompleted();
                OnWindowReady();
            }

            Repaint();
        }

        //安全加载窗口数据：LoadWindowData 的异常在此捕获进错误状态，不逃逸调用点;
        private bool SafeLoadWindowData()
        {
            try
            {
                LoadWindowData();
                return true;
            }
            catch (Exception e)
            {
                CaptureError(e);
                return false;
            }
        }

        //记录异常到错误状态并输出日志，错误状态期内 OnGUI 只走覆盖层不重复捕获;
        private void CaptureError(Exception e)
        {
            _lastError = e;
            D.Error(e);
        }

        //错误覆盖层"刷新"按钮：重载窗口数据，成功则清除错误状态回到正常绘制;
        private void HandleErrorRetry()
        {
            if (SafeLoadWindowData())
            {
                _lastError = null;
            }

            Repaint();
        }

        private void OnGUI()
        {
            if (HandleEscapeToClose())
                return;
            if (isRefreshing)
            {
                DrawRefreshOverlay();
                return;
            }

            if (_lastError != null)
            {
                DrawErrorOverlay(_lastError);
                return;
            }

            try
            {
                OnSmartGUI();
            }
            catch (ExitGUIException)
            {
                // Unity 用 ExitGUIException 作为「中止本次 GUI pass」的控制流信号（典型触发：GUILayout 控件数量不匹配）。
                // 必须原样上抛：吞掉它会丢失中止信号，GUI 状态继续错乱并连锁报错；它也不是业务异常，不进错误覆盖层。
                throw;
            }
            catch (Exception e)
            {
                CaptureError(e);
                DrawErrorOverlay(e);
            }
        }

        //绘制刷新/编译遮盖层：深色遮罩 + 状态文字 + 微型进度条，阻塞期间持续 Repaint;
        private void DrawRefreshOverlay()
        {
            var rect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(rect, GUIUtils.OverlayRefreshBg);
            string status = LC.Combine(EditorApplication.isCompiling ? Lc.Compiling : Lc.Refresh) + "....";
            GUI.Label(rect, status, GUIUtils.OverlayLabel);

            var progress = (float)(EditorApplication.timeSinceStartup % 1.0);
            var progressRect = new Rect(rect.width / 2 - 100, rect.height / 2 + 20, 200, 20);
            EditorGUI.ProgressBar(progressRect, progress, LC.Combine(Lc.PleaseWaitMoment));

            Repaint();
        }

        //绘制错误覆盖层：异常类型 + 消息 + 可滚动堆栈 + 复制/重试按钮;
        private void DrawErrorOverlay(Exception e)
        {
            var rect = new Rect(0, 0, position.width, position.height);

            EditorGUI.DrawRect(rect, GUIUtils.OverlayErrorBg);

            GUI.Label(new Rect(0, 10f, rect.width, 30f),
                $"{LC.Combine(Lc.Error)}: {e.GetType().Name}", GUIUtils.OverlayLabel);

            GUI.Label(new Rect(20f, 45f, rect.width - 40f, 25f),
                e.Message, GUIUtils.OverlaySmallLabel);

            GUILayout.BeginArea(new Rect(20f, 75f, rect.width - 40f, rect.height - 130f));
            _errorScrollPos = EditorGUILayout.BeginScrollView(_errorScrollPos);
            EditorGUILayout.TextArea(e.StackTrace, EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            if (GUI.Button(new Rect(rect.width / 2f - 110f, rect.height - 45f, 100f, 25f), LC.Tr("复制错误", "Copy Error")))
            {
                GUIUtility.systemCopyBuffer = e.ToString();
            }

            if (GUI.Button(new Rect(rect.width / 2f + 10f, rect.height - 45f, 100f, 25f), LC.Combine(Lc.Refresh)))
            {
                HandleErrorRetry();
            }
        }

        /// <summary>
        /// 处理 ESC 关闭：窗口聚焦时按下 ESC 关闭本窗口（全部派生窗口统一生效）
        /// <para>Handle ESC-to-close: pressing Escape while the window is focused closes it
        /// (applies to every derived window).</para>
        /// </summary>
        /// <returns>ESC 已消费返回 true，本次 OnGUI 结束<para>True when ESC was consumed and OnGUI should return.</para></returns>
        private bool HandleEscapeToClose()
        {
            var evt = Event.current;
            if (evt is not { type: EventType.KeyDown } || evt.keyCode != KeyCode.Escape)
                return false;
            evt.Use();
            OnEscapePressed();
            return true;
        }

        /// <summary>
        /// ESC 按下时的行为（默认关闭窗口；子类可重写以触发取消回调等清理逻辑）
        /// <para>Behavior on Escape pressed (closes the window by default; override to run
        /// cancel callbacks or other cleanup first).</para>
        /// </summary>
        protected virtual void OnEscapePressed()
        {
            Close();
        }

        protected virtual void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            CheckInitialRefreshState();
        }

        protected virtual void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;

            if (!isRefreshing)
            {
                SaveWindowData();
            }
        }

        /// <summary>
        /// 刷新开始时调用。
        /// <para>Called when a refresh starts.</para>
        /// </summary>
        protected virtual void OnRefreshStarted()
        {
        }

        /// <summary>
        /// 刷新完成后调用（数据重载成功后）。
        /// <para>Called after a refresh completes and data is reloaded.</para>
        /// </summary>
        protected virtual void OnRefreshCompleted()
        {
        }

        /// <summary>
        /// 窗口就绪时调用（首次打开或刷新完成后）。
        /// <para>Called when the window is ready (first open or after refresh).</para>
        /// </summary>
        protected virtual void OnWindowReady()
        {
        }

        /// <summary>
        /// 加载窗口数据（子类初始化绘制所需状态；异常由基类兜底进错误覆盖层）。
        /// <para>Loads window data; exceptions are caught by the base and routed to the error overlay.</para>
        /// </summary>
        protected virtual void LoadWindowData()
        {
        }

        /// <summary>
        /// 保存窗口数据（窗口禁用且非刷新状态时调用）。
        /// <para>Saves window data on disable while not refreshing.</para>
        /// </summary>
        protected virtual void SaveWindowData()
        {
        }

        /// <summary>
        /// 子类实现实际的 GUI 绘制；抛出的异常由基类捕获并显示错误覆盖层。
        /// <para>Draws the actual GUI; exceptions thrown here are caught by the base and shown in the error overlay.</para>
        /// </summary>
        protected abstract void OnSmartGUI();

        /// <summary>
        /// 安全地显示窗口（刷新/编译期间不打开并返回 null）。
        /// <para>Shows the window safely; returns null during refresh/compile.</para>
        /// </summary>
        /// <typeparam name="T">窗口类型<para>Window type</para></typeparam>
        /// <returns>窗口实例，刷新期间为 null<para>Window instance, or null while refreshing</para></returns>
        public new static T GetWindow<T>() where T : EditorWindowBase
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                return null;

            return EditorWindow.GetWindow<T>();
        }

        /// <summary>
        /// 安全地显示窗口（带标题，刷新/编译期间不打开并返回 null）。
        /// <para>Shows the window safely with a title; returns null during refresh/compile.</para>
        /// </summary>
        /// <param name="title">窗口标题<para>Window title</para></param>
        /// <typeparam name="T">窗口类型<para>Window type</para></typeparam>
        /// <returns>窗口实例，刷新期间为 null<para>Window instance, or null while refreshing</para></returns>
        public new static T GetWindow<T>(string title) where T : EditorWindowBase
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                return null;

            return EditorWindow.GetWindow<T>(title);
        }

        #endregion
    }
}
