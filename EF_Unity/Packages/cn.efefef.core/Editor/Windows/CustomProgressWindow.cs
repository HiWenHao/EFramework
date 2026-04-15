/*
 * ================================================
 * Describe:      管理项目相关包资产.
 * Author:        Alvin8412
 * CreationTime:  2026-04-15 18:02:04
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-15 18:02:04
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    public class CustomProgressWindow : EditorWindow
    {
        private int _totalCount;
        private int _currentCount;
        private float _progress;
        private float _currentProgress;
        private bool _isProcessing;

        private int _animIndex;
        private string[] _animator;
        
        private Action<bool> _cancelCallback;
        private static CustomProgressWindow _window;

        /// <summary>
        /// 打开进度窗口
        /// </summary>
        /// <param name="title">本次进度条标题显示内容</param>
        /// <param name="cancelCallback">取消按钮回调</param>
        public static bool ShowWindow(string title, Action<bool> cancelCallback)
        {
            _window = GetWindow<CustomProgressWindow>(true, title);

            if (_window._isProcessing)
                return false;

            EditorApplication.update += _window.UpdateTask;
            _window._cancelCallback = cancelCallback;
            _window._isProcessing = true;
            _window._progress = 0.1f;
            _window._currentProgress = 0.001f;
            _window._animator = new []
            {
                "·.....",
                ".·....",
                "..·...",
                "...·..",
                "....·.",
                ".....·",
            };
            
            Rect rect = _window.position;
            rect.position = GUIUtility.GUIToScreenPoint(new Vector2(300f, 200f));
            _window.position = rect;
            _window.maxSize = new Vector2(450f, 150f);
            _window.ShowUtility();
            return true;
        }

        /// <summary>
        /// 更新进度信息
        /// </summary>
        /// <param name="currentCount">当前已完成</param>
        /// <param name="totalCount">全部数量</param>
        public static void UpdateInfo(int currentCount, int totalCount)
        {
            _window._totalCount = totalCount;
            _window._currentCount = currentCount + 1;
        }

        /// <summary>
        /// 关闭进度窗口
        /// </summary>
        public static void CloseWindow()
        {
            if (null == _window)
                return;

            _window.Quit();
            _window.Close();
            _window = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"{LC.Combine(Lc.PleaseWaitMoment)} {_animator[_animIndex++ / 500]} {_currentProgress * 100:F2}%", EditorStyles.wordWrappedLabel);
            if (_animIndex >= _animator.Length * 500)
                _animIndex = 0;
            EditorGUILayout.Space(5);
            
            Rect rect = GUILayoutUtility.GetRect(200, 18, "TextField");
            EditorGUI.ProgressBar(rect, _progress, $"{_progress:F2}%");
            
            EditorGUILayout.Space(10);
            
            if (null == _cancelCallback)
                return;
            if (GUILayout.Button(LC.Combine(Lc.Cancel), GUILayout.Height(25)))
                CloseWindow();
        }

        private void OnDestroy()
        {
            if (null == _window)
                return;

            Quit();
        }

        private void UpdateTask()
        {
            if ((_progress += 0.0001f) >= (float)_currentCount / _totalCount)
                _progress = 0.0001f * ((float)_totalCount / _currentCount);
            float cur = (_progress + _currentCount - 1) / _totalCount;
            _currentProgress = cur > _currentProgress ? cur : _currentProgress;

            Repaint();
        }

        private void Quit()
        {
            if (!_window._isProcessing)
                return;
            
            _progress = 0.1f;
            _currentProgress = 0.1f;
            _animator = null;
            _isProcessing = false;
            _cancelCallback?.Invoke(false);
            _cancelCallback = null;
            EditorApplication.update -= UpdateTask;
        }
    }
}