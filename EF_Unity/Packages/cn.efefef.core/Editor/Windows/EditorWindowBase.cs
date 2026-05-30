/*
 * ================================================
 * Describe:      EditorWindow 稳定基类，提供 Domain Reload 安全防护、刷新状态监控与异常兜底。
 * Author:        Alvin.Wang(Wenhao)
 * CreationTime:  2026-03-19 18:19:50
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-30 15:05:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using UnityEditor;
using UnityEngine;
using System;

namespace EasyFramework.Edit.Windows
{
    /// <summary>
    /// EditorWindow 稳定基类。
    /// <br/>特性：Domain Reload 安全（_isRefreshing 序列化）<br/>刷新/编译时自动遮盖<br/>稳定帧计数防抖<br/>异常兜底覆盖层.
    /// <para>子类实现 <see cref="OnSmartGUI"/> 作为主绘制入口，重写 Load/Save 实现数据持久化。</para>
    /// </summary>
    public abstract class EditorWindowBase : EditorWindow
    {
        private const int RequiredStableFrames = 3; //需要累积的稳定帧数才能确认刷新结束;
        [SerializeField] private bool _isRefreshing; //是否处于刷新/编译状态
        private int _stableFrameCount; //刷新结束后的稳定帧计数，防止短暂恢复造成误判

        #region 生命周期

        /// <summary>
        /// 窗口启用：订阅 EditorApplication.update，检测当前刷新状态。
        /// </summary>
        protected virtual void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            CheckInitialRefreshState();
        }

        /// <summary>
        /// 窗口禁用：退订 update，非刷新状态下保存窗口数据。
        /// </summary>
        protected virtual void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;

            if (!_isRefreshing)
            {
                SaveWindowData();
            }
        }

        #endregion

        #region 刷新状态监控

        /// <summary>
        /// EditorApplication.update 回调。检测刷新/编译状态变化，驱动进入/退出刷新流程。
        /// </summary>
        private void OnEditorUpdate()
        {
            bool currentlyRefreshing = EditorApplication.isUpdating || EditorApplication.isCompiling;

            if (!_isRefreshing && currentlyRefreshing)
                EnterRefreshState();

            if (_isRefreshing && !currentlyRefreshing)
                HandleRefreshCompletion();
        }

        /// <summary>
        /// 首次启用时检测当前是否已在刷新中，决定走"等待刷新完成"还是"直接就绪"路径。
        /// </summary>
        private void CheckInitialRefreshState()
        {
            _isRefreshing = EditorApplication.isUpdating || EditorApplication.isCompiling;

            if (_isRefreshing)
            {
                OnRefreshStarted();
            }
            else
            {
                LoadWindowData();
                OnWindowReady();
            }
        }

        /// <summary>
        /// 进入刷新状态：保存数据、通知子类、强制重绘。
        /// </summary>
        private void EnterRefreshState()
        {
            _isRefreshing = true;
            _stableFrameCount = 0;
            SaveWindowData();
            OnRefreshStarted();
            Repaint();
        }

        /// <summary>
        /// 处理刷新完成。累积稳定帧数防抖，达到阈值后才确认刷新结束，
        /// 加载数据、通知子类、进入就绪状态。
        /// </summary>
        private void HandleRefreshCompletion()
        {
            _stableFrameCount++;

            if (_stableFrameCount < RequiredStableFrames)
                return;

            _isRefreshing = false;
            LoadWindowData();
            OnRefreshCompleted();
            OnWindowReady();
            Repaint();
        }

        #endregion

        #region 可重写方法

        /// <summary>
        /// 刷新开始时调用
        /// </summary>
        protected virtual void OnRefreshStarted()
        {
        }

        /// <summary>
        /// 刷新完成后调用
        /// </summary>
        protected virtual void OnRefreshCompleted()
        {
        }

        /// <summary>
        /// 窗口就绪时调用（首次打开或刷新完成后）
        /// </summary>
        protected virtual void OnWindowReady()
        {
        }

        /// <summary>
        /// 加载窗口数据
        /// </summary>
        protected virtual void LoadWindowData()
        {
        }

        /// <summary>
        /// 保存窗口数据
        /// </summary>
        protected virtual void SaveWindowData()
        {
        }

        /// <summary>
        /// 子类实现实际的GUI绘制
        /// </summary>
        protected abstract void OnSmartGUI();

        #endregion

        #region GUI控制

        /// <summary>
        /// Unity OnGUI 入口。刷新中显示遮盖层，否则调用子类 <see cref="OnSmartGUI"/>，
        /// 异常时显示错误覆盖层。
        /// </summary>
        private void OnGUI()
        {
            if (_isRefreshing)
            {
                DrawRefreshOverlay();
                return;
            }

            try
            {
                OnSmartGUI();
            }
            catch (Exception e)
            {
                D.Error(e);
                DrawErrorOverlay(e);
            }
        }

        /// <summary>
        /// 绘制刷新/编译遮盖层：深色遮罩 + 状态文字 + 微型进度条，阻塞期间持续 Repaint。
        /// </summary>
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

        /// <summary>
        /// 绘制 GUI 异常覆盖层：红色遮罩 + 错误信息 + 重试按钮。
        /// </summary>
        private void DrawErrorOverlay(Exception e)
        {
            var rect = new Rect(0, 0, position.width, position.height);

            EditorGUI.DrawRect(rect, GUIUtils.OverlayErrorBg);

            GUI.Label(new Rect(0, rect.height / 2 - 30, rect.width, 30),
                $"{LC.Combine(Lc.Error)}: GUI Error", GUIUtils.OverlayLabel);

            GUI.Label(new Rect(20, rect.height / 2, rect.width - 40, 60),
                e.Message, GUIUtils.OverlaySmallLabel);

            if (!GUI.Button(new Rect(rect.width / 2 - 50, rect.height - 40, 100, 25), LC.Combine(Lc.Refresh))) return;
            LoadWindowData();
            Repaint();
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 当前是否在刷新中
        /// </summary>
        protected bool IsRefreshing => _isRefreshing;

        /// <summary>
        /// 安全地显示窗口（刷新时不打开）
        /// </summary>
        public new static T GetWindow<T>() where T : EditorWindowBase
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                return null;

            return EditorWindow.GetWindow<T>();
        }

        /// <summary>
        /// 安全地显示窗口（带标题）
        /// </summary>
        public new static T GetWindow<T>(string title) where T : EditorWindowBase
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                return null;

            return EditorWindow.GetWindow<T>(title);
        }

        #endregion
    }
}