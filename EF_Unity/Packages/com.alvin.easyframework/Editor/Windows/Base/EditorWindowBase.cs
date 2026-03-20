/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin.Wang(Wenhao)
 * CreationTime:  2026-03-19 18:19:50
 * ModifyAuthor:  Alvin.Wang(Wenhao)
 * ModifyTime:    2026-03-19 18:19:50
 * ScriptVersion: 0.1
 * ===============================================
*/

using UnityEditor;
using UnityEngine;
using System;

namespace EasyFramework.Windows
{
    /// <summary>
    /// 窗口基类稳定版
    /// </summary>
    public abstract class EditorWindowBase : EditorWindow
    {
        [SerializeField] 
        private bool _isRefreshing;
        private int _stableFrameCount;
        private const int REQUIRED_STABLE_FRAMES = 3;

        #region 生命周期

        protected virtual void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            CheckInitialRefreshState();
        }

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

        private void OnEditorUpdate()
        {
            bool currentlyRefreshing = EditorApplication.isUpdating || EditorApplication.isCompiling;

            // 检测刷新开始
            if (!_isRefreshing && currentlyRefreshing)
            {
                EnterRefreshState();
            }

            // 检测刷新结束
            if (_isRefreshing && !currentlyRefreshing)
            {
                HandleRefreshCompletion();
            }
        }

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

        private void EnterRefreshState()
        {
            _isRefreshing = true;
            _stableFrameCount = 0;
            SaveWindowData();
            OnRefreshStarted();
            Repaint();
        }

        private void HandleRefreshCompletion()
        {
            _stableFrameCount++;

            if (_stableFrameCount < REQUIRED_STABLE_FRAMES) 
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
                DrawErrorOverlay(e);
            }
        }

        private void DrawRefreshOverlay()
        {
            var rect = new Rect(0, 0, position.width, position.height);

            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.95f));

            var style = new GUIStyle(EditorStyles.largeLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 14
            };

            string status = EditorApplication.isCompiling ? "编译中" : "资源刷新中";
            GUI.Label(rect, $"⏳ {status}...", style);

            var progress = (float)(EditorApplication.timeSinceStartup % 1.0);
            var progressRect = new Rect(rect.width / 2 - 100, rect.height / 2 + 20, 200, 20);
            EditorGUI.ProgressBar(progressRect, progress, "请稍候");

            Repaint();
        }

        private void DrawErrorOverlay(Exception e)
        {
            var rect = new Rect(0, 0, position.width, position.height);

            EditorGUI.DrawRect(rect, new Color(0.3f, 0.1f, 0.1f, 0.95f));

            var style = new GUIStyle(EditorStyles.largeLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 14
            };

            GUI.Label(new Rect(0, rect.height / 2 - 30, rect.width, 30), "❌ 绘制错误", style);

            style.fontSize = 10;
            GUI.Label(new Rect(20, rect.height / 2, rect.width - 40, 60), e.Message, style);

            if (GUI.Button(new Rect(rect.width / 2 - 50, rect.height - 40, 100, 25), "重试"))
            {
                LoadWindowData();
                Repaint();
            }
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