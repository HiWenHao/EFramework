/*
 * ================================================
 * Describe:      事件管理器监控面板，在编辑器下快速查看是否有对应的事件被注册
 * Author:        Alvin8412
 * CreationTime:  2026-05-18 14:24:39
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-18 14:24:39
 * ScriptVersion: 0.1
 * ===============================================
 */

#region UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EasyFramework.Edit;

namespace EasyFramework.Managers.Event.Editor
{
    /// <summary>
    /// 事件管理器监控面板
    /// </summary>
    [CustomEditor(typeof(EventManager))]
    public class EventManagerInspector : EFInspectorBase<EventManager>
    {
        /// <summary>
        /// 编辑器内部使用, 事件信息
        /// </summary>
        private struct EventInfo
        {
            public string Name; // 事件结构名
            public int SyncHandlersCount; // 同步句柄数量
            public int AsyncHandlersCount; // 异步句柄数量
        }

        private const float Width = 80f;

        private string _errorMessage;
        private Vector2 _scrollPos;
        private GUIStyle _labelStyle;
        private List<EventInfo> _eventInfos;

        protected override string Title => LC.Combine(Lc.Event, Lc.Data, Lc.Monitor);

        protected override void OnEditorEnable()
        {
            _eventInfos = new List<EventInfo>();

            _labelStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        protected override void OnEditorUpdate()
        {
            var subscriptionsDict = GetSubscriptionsDictionary(Target);
            if (subscriptionsDict == null)
            {
                _errorMessage = LC.Combine(Lc.Reflect, Lc.Error);
                return;
            }

            _eventInfos.Clear();
            foreach (DictionaryEntry kv in subscriptionsDict)
            {
                Type eventType = kv.Key as Type;
                object subs = kv.Value;
                if (eventType == null || subs == null) continue;
                _eventInfos.Add(new EventInfo()
                {
                    Name = eventType.Name,
                    SyncHandlersCount = GetHandlerCount(subs, "SyncHandlers"),
                    AsyncHandlersCount = GetHandlerCount(subs, "AsyncHandlers"),
                });
            }
        }

        protected override void OnEditorDisable()
        {
            _eventInfos.Clear();
            _eventInfos = null;
        }

        protected override void OnEditorGUI()
        {
            EditorGUILayout.Space(10);
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
                return;
            }

            if (_eventInfos.Count == 0)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Current, Lc.No, Lc.Any, Lc.Event, Lc.Subscriptions),
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                EditorGUILayout.LabelField(LC.Combine(Lc.Event, Lc.Type), GUILayout.Width(200));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(LC.Combine(Lc.Sync, Lc.Count), _labelStyle, GUILayout.Width(Width));
                EditorGUILayout.LabelField(LC.Combine(Lc.Async, Lc.Count), _labelStyle, GUILayout.Width(Width));
                EditorGUILayout.EndHorizontal();

                _scrollPos =
                    EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MinHeight(150), GUILayout.MaxHeight(550));
                foreach (var kv in _eventInfos)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(kv.Name, GUILayout.Width(200));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"{kv.SyncHandlersCount}", _labelStyle, GUILayout.Width(Width));
                    EditorGUILayout.LabelField($"{kv.AsyncHandlersCount}", _labelStyle, GUILayout.Width(Width));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox($"{LC.Combine(Lc.Event, Lc.Total, Lc.Is)}: {_eventInfos.Count}",
                    MessageType.None);
            }
        }

        // 获取 _subscriptions 字典
        private IDictionary GetSubscriptionsDictionary(EventManager mgr)
        {
            try
            {
                var field = typeof(EventManager).GetField("_subscriptions",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                    return null;

                return field.GetValue(mgr) as IDictionary;
            }
            catch (Exception e)
            {
                D.Error(e);
                return null;
            }
        }

        // 获取 EventSubscriptions 中指定列表字段的元素个数
        private int GetHandlerCount(object subs, string fieldName)
        {
            try
            {
                var field = subs.GetType().GetField(fieldName,
                    BindingFlags.Public | BindingFlags.Instance);
                if (field == null) return 0;

                var list = field.GetValue(subs) as IList;
                return list?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}

#endregion