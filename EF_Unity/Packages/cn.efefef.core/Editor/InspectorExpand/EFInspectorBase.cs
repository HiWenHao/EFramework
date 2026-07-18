/*
 * ================================================
 * Describe:      EF的Inspector视窗基类, 用来规范和整理各个视窗的布局混乱
 * Author:        Alvin8412
 * CreationTime:  2026-05-18 18:25:42
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-18 18:25:42
 * ScriptVersion: 0.1
 * ===============================================
 */

using EasyFramework.Edit.Windows;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// EF的Inspector视窗基类
    /// </summary>
    public abstract class EFInspectorBase<T> : Editor where T : class, ISingleton
    {
        /// <summary>
        /// T类型目标对象
        /// </summary>
        protected T Target;

        /// <summary>
        /// 开启自动刷新
        /// <para>Enable auto-refresh</para>
        /// </summary>
        protected virtual bool AutoRefresh { get; set; } = true;

        /// <summary>
        /// 自动刷新间隔时间
        /// <para>Auto-refresh interval time</para>
        /// </summary>
        protected virtual double RefreshInterval { get; set; } = 0.3f;

        /// <summary>
        /// Inspector面板标题
        /// <para>Title of the Inspector panel</para>
        /// </summary>
        protected abstract string Title { get; }

        private double _nextRefreshTime; // 下次刷新时间

        private void OnEnable()
        {
            Target = target as T;
            OnEditorEnable();
            EditorApplication.update += OnUpdate;
            _nextRefreshTime = EditorApplication.timeSinceStartup + RefreshInterval;
        }

        private void OnUpdate()
        {
            if (!Application.isPlaying) return;
            if (!AutoRefresh || EditorApplication.timeSinceStartup < _nextRefreshTime) return;
            _nextRefreshTime = EditorApplication.timeSinceStartup + RefreshInterval;
            Refresh();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            OnEditorDisable();
        }

        private void Refresh()
        {
            OnEditorUpdate();
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            Target ??= target as T;

            #region Title

            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            var titleRect = GUILayoutUtility.GetRect(GUIContent.none, GUIUtils.InspectorTitle());
            EditorGUI.DrawRect(titleRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            EditorGUI.LabelField(titleRect, Title, GUIUtils.InspectorTitle());

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Non, Lc.Running, Lc.No, Lc.Data), MessageType.Info);
                return;
            }

            #endregion

            EditorGUILayout.Space();
            OnEditorGUI();
            EditorGUILayout.Space();

            #region Refresh

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(Lc.Manually, Lc.Refresh), GUILayout.MaxWidth(150f)))
                Refresh();
            AutoRefresh = EditorGUILayout.ToggleLeft(LC.Combine(Lc.Enable, Lc.Auto, Lc.Refresh), AutoRefresh,
                GUILayout.Width(150f));
            if (AutoRefresh)
            {
                float interval =
                    System.MathF.Round(
                        EditorGUILayout.DelayedFloatField((float)RefreshInterval, GUILayout.MaxWidth(40f)), 2);
                RefreshInterval = Mathf.Clamp(interval, 0.1f, 1.0f);
            }

            EditorGUILayout.EndHorizontal();
            if (AutoRefresh)
                EditorGUILayout.LabelField(LC.Combine(Lc.Auto, Lc.Refresh, Lc.Interval) + $": {RefreshInterval:F2} s",
                    GUIUtils.SmallNote());
            EditorGUILayout.EndVertical();

            #endregion
        }

        /// <summary>
        /// 绘制
        /// </summary>
        protected abstract void OnEditorGUI();

        /// <summary>
        /// 被聚焦
        /// </summary>
        protected virtual void OnEditorEnable()
        {
        }

        /// <summary>
        /// 更新
        /// </summary>
        protected abstract void OnEditorUpdate();

        /// <summary>
        /// 失去聚焦
        /// </summary>
        protected virtual void OnEditorDisable()
        {
        }
    }
}