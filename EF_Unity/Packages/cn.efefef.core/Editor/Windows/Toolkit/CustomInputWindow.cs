/*
 * ================================================
 * Describe:      自定义内容输入窗口
 * Author:        Alvin8412
 * CreationTime:  2026-04-29 14:30:29
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-29 14:30:29
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;
using UnityEditor;
using System;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 自定义内容输入窗口
    /// </summary>
    public class CustomInputWindow : EditorWindow
    {
        private int _argsCount;
        private string[] _inputTips;
        private string[] _inputTextArray;
        private Action<string> _onConfirm1;
        private Action<string, string> _onConfirm2;
        private Action<string, string, string> _onConfirm3;
        private Action<string, string, string, string> _onConfirm4;
        private static CustomInputWindow _window;

        private static void ShowSelf(string title, int count)
        {
            _window = GetWindow<CustomInputWindow>(true, title);
            _window.titleContent = new GUIContent(title);
            _window.ShowUtility();
            Vector2 size = new Vector2(300, 50 + count * 40f);
            _window.minSize = size;
            _window.maxSize = size;
            Rect rect = _window.position;
            rect.position = GUIUtility.GUIToScreenPoint(new Vector2(600f, 200f));
            _window.position = rect;
            _window.Focus();
        }

        private void OnDestroy()
        {
            _onConfirm1 = null;
            _onConfirm2 = null;
            _onConfirm3 = null;
            _onConfirm4 = null;
            _inputTips = null;
            _inputTextArray = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            for (int i = 0; i < _argsCount; i++)
            {
                EditorGUILayout.LabelField(_inputTips[i], EditorStyles.boldLabel);
                _inputTextArray[i] = EditorGUILayout.TextField(_inputTextArray[i]);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(LC.Combine(Lc.Confirm), GUILayout.Width(80)))
            {
                Confirm1Callback();
                Close();
            }

            if (GUILayout.Button(LC.Combine(Lc.Cancel), GUILayout.Width(80)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void Confirm1Callback()
        {
            switch (_argsCount)
            {
                case 1:
                    _onConfirm1?.Invoke(_inputTextArray[0]);
                    break;
                case 2:
                    _onConfirm2?.Invoke(_inputTextArray[0], _inputTextArray[1]);
                    break;
                case 3:
                    _onConfirm3?.Invoke(_inputTextArray[0], _inputTextArray[1], _inputTextArray[2]);
                    break;
                case 4:
                    _onConfirm4?.Invoke(_inputTextArray[0], _inputTextArray[1], _inputTextArray[2], _inputTextArray[3]);
                    break;
            }
        }

        private void Init(int argsCount, string[] inputTips)
        {
            _onConfirm1 = null;
            _onConfirm2 = null;
            _onConfirm3 = null;
            _onConfirm4 = null;
            _argsCount = argsCount;
            _inputTips = new string[_argsCount];
            _inputTextArray = new string[_argsCount];

            int maxCount = _argsCount < inputTips.Length ? _argsCount : inputTips.Length;
            for (int i = 0; i < maxCount; i++)
            {
                _inputTips[i] = inputTips[i];
            }
        }

        public static void ShowWindow(string title, string[] tips, Action<string> onConfirmCallback)
        {
            ShowSelf(title, 1);
            _window.Init(1, tips);
            _window._onConfirm1 = onConfirmCallback;
        }
        public static void ShowWindow(string title, string[] tips, Action<string, string> onConfirmCallback)
        {
            ShowSelf(title, 2);
            _window.Init(2, tips);
            _window._onConfirm2 = onConfirmCallback;
        }
        public static void ShowWindow(string title, string[] tips, Action<string, string, string> onConfirmCallback)
        {
            ShowSelf(title, 3);
            _window.Init(3, tips);
            _window._onConfirm3 = onConfirmCallback;
        }
        public static void ShowWindow(string title, string[] tips, Action<string, string, string, string> onConfirmCallback)
        {
            ShowSelf(title, 4);
            _window.Init(4, tips);
            _window._onConfirm4 = onConfirmCallback;
        }
    }
}