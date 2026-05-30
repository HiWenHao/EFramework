/*
 * ================================================
 * Describe:        自定义内容输入窗口
 * Author:          Alvin8412
 * CreationTime:    2026-04-29 14:30:29
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-05-30 09:55:00
 * ScriptVersion:   1.0
 * ===============================================
 */

using UnityEngine;
using UnityEditor;
using System;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 自定义内容输入窗口，通过静态方法 <see cref="ShowWindow"/> 打开 <br/>支持 Enter与Escape的快捷操作。
    /// </summary>
    public class CustomInputWindow : EditorWindow
    {
        private const float MinWidth = 300f;        // 窗口最小宽度
        private const float RowHeight = 40f;        // 单个输入行高度
        private const float HeaderHeight = 50f;     // 顶部 + 按钮区固定高度

        private int _argCount;                      // 当前窗口参数个数
        private string[] _inputTips;                // 各输入框提示文本
        private string[] _inputTextArray;           // 各输入框当前内容
        private Action<string[]> _onConfirm;        // 确认回调（统一为字符串数组）
        private static CustomInputWindow _window;   // 单例引用，同时只允许一个输入窗口

        #region 初始化

        // 创建并显示窗口，设置尺寸和位置
        private static void ShowSelf(string title, int count)
        {
            _window = GetWindow<CustomInputWindow>(true, title);
            _window.titleContent = new GUIContent(title);
            _window.ShowUtility();

            var size = new Vector2(MinWidth, HeaderHeight + count * RowHeight);
            _window.minSize = size;
            _window.maxSize = size;

            Rect rect = _window.position;
            rect.position = GUIUtility.GUIToScreenPoint(new Vector2(600, 200f));
            _window.position = rect;
            _window.Focus();
        }

        // 初始化输入框数量和提示文本
        private void Init(int argCount, string[] inputTips)
        {
            _onConfirm = null;
            _argCount = argCount;
            _inputTips = new string[_argCount];
            _inputTextArray = new string[_argCount];

            for (int i = 0; i < _argCount; i++)
            {
                _inputTips[i] = inputTips != null && i < inputTips.Length ? inputTips[i] : string.Empty;
            }

            CustomProgressWindow.ShowWindow("Test", (bol) =>
            {
                
            });
        }

        #endregion

        #region GUI

        private int totle = 10;
        private int current = 0;
        // 绘制输入框、确认/取消按钮，处理键盘快捷键
        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Test"))
            {
                CustomProgressWindow.UpdateInfo(++this.current, totle);
            }

            for (int i = 0; i < _argCount; i++)
            {
                EditorGUILayout.LabelField(_inputTips[i], EditorStyles.boldLabel);
                _inputTextArray[i] = EditorGUILayout.TextField(_inputTextArray[i]);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(LC.Combine(Lc.Confirm), GUILayout.Width(80)))
            {
                Confirm();
            }

            if (GUILayout.Button(LC.Combine(Lc.Cancel), GUILayout.Width(80)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();

            var current = Event.current;
            if (current.type != EventType.KeyDown) return;
            switch (current.keyCode)
            {
                case KeyCode.Return or KeyCode.KeypadEnter:
                    current.Use();
                    Confirm();
                    break;
                case KeyCode.Escape:
                    current.Use();
                    Close();
                    break;
            }
        }

        // 触发确认回调并关闭窗口
        private void Confirm()
        {
            _onConfirm?.Invoke(_inputTextArray);
            Close();
        }

        #endregion

        #region 静态入口

        /// <summary>
        /// 打开单参数输入窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="tips">输入框提示文本数组</param>
        /// <param name="onConfirm">确认回调，参数为用户输入的第一个字符串</param>
        public static void ShowWindow(string title, string[] tips, Action<string> onConfirm)
        {
            ShowSelf(title, 1);
            _window.Init(1, tips);
            _window._onConfirm = args => onConfirm?.Invoke(args[0]);
        }

        /// <summary>
        /// 打开双参数输入窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="tips">输入框提示文本数组</param>
        /// <param name="onConfirm">确认回调，参数为用户输入的两个字符串</param>
        public static void ShowWindow(string title, string[] tips, Action<string, string> onConfirm)
        {
            ShowSelf(title, 2);
            _window.Init(2, tips);
            _window._onConfirm = args => onConfirm?.Invoke(args[0], args[1]);
        }

        /// <summary>
        /// 打开三参数输入窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="tips">输入框提示文本数组</param>
        /// <param name="onConfirm">确认回调，参数为用户输入的三个字符串</param>
        public static void ShowWindow(string title, string[] tips, Action<string, string, string> onConfirm)
        {
            ShowSelf(title, 3);
            _window.Init(3, tips);
            _window._onConfirm = args => onConfirm?.Invoke(args[0], args[1], args[2]);
        }

        /// <summary>
        /// 打开四参数输入窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="tips">输入框提示文本数组</param>
        /// <param name="onConfirm">确认回调，参数为用户输入的四个字符串</param>
        public static void ShowWindow(string title, string[] tips, Action<string, string, string, string> onConfirm)
        {
            ShowSelf(title, 4);
            _window.Init(4, tips);
            _window._onConfirm = args => onConfirm?.Invoke(args[0], args[1], args[2], args[3]);
        }

        #endregion

        #region 清理

        // 窗口销毁时释放所有引用，避免内存泄漏
        private void OnDestroy()
        {
            _onConfirm = null;
            _inputTips = null;
            _inputTextArray = null;
        }

        #endregion
    }
}