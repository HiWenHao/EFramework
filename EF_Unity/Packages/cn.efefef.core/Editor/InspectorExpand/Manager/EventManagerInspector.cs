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
using System.Reflection;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows;

namespace EasyFramework.Managers.Event.Editor
{
    /// <summary>
    /// 事件管理器监控面板
    /// </summary>
    [CustomEditor(typeof(EventManager))]
    public class EventManagerInspector : UnityEditor.Editor
    {
        private Vector2 _scrollPos;
        private bool _autoRefresh;
        private GUIStyle _labelStyle;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            var titleRect = GUILayoutUtility.GetRect(GUIContent.none, GUIUtils.InspectorTitle());
            EditorGUI.DrawRect(titleRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            EditorGUI.LabelField(titleRect, LC.Combine(Lc.Event, Lc.Data, Lc.Monitor ), GUIUtils.InspectorTitle());

            
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox( LC.Combine(Lc.Non, Lc.Running, Lc.Not, Lc.Data), MessageType.Info);
                return;
            }

            var eventManager = (EventManager)target;
            EditorGUILayout.Space(10);
            // 尝试获取订阅数据
            var subscriptionsDict = GetSubscriptionsDictionary(eventManager);
            if (subscriptionsDict == null)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Reflect, Lc.Error), MessageType.Error);
                return;
            }

            if (subscriptionsDict.Count == 0)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Current, Lc.No, Lc.Any, Lc.Event, Lc.Subscriptions), MessageType.Info);
            }
            else
            {
                _labelStyle ??= new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                EditorGUILayout.LabelField(LC.Combine(Lc.Event, Lc.Type), GUILayout.Width(200));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(LC.Combine(Lc.Sync, Lc.Count), _labelStyle, GUILayout.Width(70));
                EditorGUILayout.LabelField(LC.Combine(Lc.Async, Lc.Count), _labelStyle, GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MinHeight(150), GUILayout.MaxHeight(550));
                foreach (DictionaryEntry kv in subscriptionsDict)
                {
                    Type eventType = kv.Key as Type;
                    object subs = kv.Value;
                    if (eventType == null || subs == null) continue;

                    int syncCount = GetHandlerCount(subs, "SyncHandlers");
                    int asyncCount = GetHandlerCount(subs, "AsyncHandlers");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(eventType.Name, GUILayout.Width(200));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(syncCount.ToString(), _labelStyle, GUILayout.Width(70));
                    EditorGUILayout.LabelField(asyncCount.ToString(), _labelStyle, GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox($"{LC.Combine(Lc.Event, Lc.Total, Lc.Is)}: {subscriptionsDict.Count}", MessageType.None);
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
                
                var value = field.GetValue(mgr);
                return value as IDictionary;
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