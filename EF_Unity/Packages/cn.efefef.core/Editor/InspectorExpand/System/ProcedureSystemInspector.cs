/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-11 16:18:56
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-11 16:18:56
 * ScriptVersion: 0.1
 * ===============================================
 */

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Systems.Procedure.Editor
{
    [CustomEditor(typeof(ProcedureSystem))]
    public class ProcedureSystemInspector : UnityEditor.Editor
    {
        private ProcedureSystem _targetSystem;
        
        // 反射字段
        private FieldInfo _stackField;
        private FieldInfo _pendingExitsField;

        // ProcedureInstance 反射信息
        private Type _procedureInstanceType;
        private FieldInfo _uidField;
        private FieldInfo _stateField;
        private FieldInfo _depthField;
        private FieldInfo _paramsField;
        private FieldInfo _exitStateField;
        private FieldInfo _parentUidField;
        private FieldInfo _exitQueuedField;
        private FieldInfo _exitReasonField;
        private FieldInfo _procedureTypeField;
        private FieldInfo _exitExceptionField;
        private FieldInfo _runtimeVersionField;

        private double _lastRepaintTime;
        
        private bool _reflectionReady;
        private bool _showActivityTree = true;
        private bool _showPendingQueue = true;
        private string _reflectionError;
        private readonly Dictionary<long, bool> _foldoutStates = new();
        private const double RepaintInterval = 0.3f; // 降低刷新频率
        private Vector2 _treeScrollPos; // 活动流程树滚动位置

        private void OnEnable()
        {
            _targetSystem = (ProcedureSystem)target;
            CacheReflectionInfo();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying) return;
            if (_targetSystem == null) return; // 目标失效检查

            if (!(EditorApplication.timeSinceStartup - _lastRepaintTime >= RepaintInterval)) return;
            _lastRepaintTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void CacheReflectionInfo()
        {
            try
            {
                var sysType = typeof(ProcedureSystem);
                _stackField = sysType.GetField("_instanceStack", BindingFlags.NonPublic | BindingFlags.Instance);
                _pendingExitsField = sysType.GetField("_pendingExits", BindingFlags.NonPublic | BindingFlags.Instance);

                // 获取 internal 类型
                _procedureInstanceType = sysType.Assembly.GetType("EasyFramework.Systems.Procedure.ProcedureInstance");
                if (_procedureInstanceType == null)
                    throw new Exception("无法获取 ProcedureInstance 类型");

                _uidField = GetInstanceField("Uid");
                _parentUidField = GetInstanceField("ParentUid");
                _depthField = GetInstanceField("Depth");
                _runtimeVersionField = GetInstanceField("RuntimeVersion");
                _exitQueuedField = GetInstanceField("ExitQueued");
                _exitStateField = GetInstanceField("ExitState");
                _stateField = GetInstanceField("State");
                _procedureTypeField = GetInstanceField("ProcedureType");
                _exitExceptionField = GetInstanceField("ExitException");
                _exitReasonField = GetInstanceField("ExitReason");
                _paramsField = GetInstanceField("Params");

                if (_stackField == null || _pendingExitsField == null || _procedureInstanceType == null ||
                    _uidField == null || _parentUidField == null || _depthField == null)
                    throw new Exception("关键反射字段缺失，请检查 ProcedureSystem 和 ProcedureInstance 的字段定义");

                _reflectionReady = true;
                _reflectionError = null;
            }
            catch (Exception e)
            {
                _reflectionReady = false;
                _reflectionError = e.Message;
                Debug.LogError($"[ProcedureSystemInspector] 反射初始化失败: {e}");
            }

            FieldInfo GetInstanceField(string instanceName) => _procedureInstanceType?.GetField(instanceName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10);
            var titleRect = GUILayoutUtility.GetRect(GUIContent.none, GUIUtils.InspectorTitle());
            EditorGUI.DrawRect(titleRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            EditorGUI.LabelField(titleRect, LC.Combine(Lc.Procedure, Lc.Running, Lc.Monitor ), GUIUtils.InspectorTitle());


            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox( LC.Combine(Lc.Non, Lc.Running, Lc.Not, Lc.Data), MessageType.Info);
                return;
            }

            if (!_reflectionReady)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Reflect, Lc.Error, Lc.Please, Lc.Check, Lc.Code), MessageType.Error);
                return;
            }

            DrawRuntimeMonitor();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(Lc.Manually, Lc.Refresh), GUILayout.Width(80)))
                Repaint();
            EditorGUILayout.LabelField(LC.Combine(Lc.Auto, Lc.Refresh, Lc.Interval) + $": {RepaintInterval:F1} s", GUIUtils.SmallNote());
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRuntimeMonitor()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.Space();
            
            // 获取当前活动实例并构建树（同时清理无用的 foldout 键）
            var activeInstances = GetActiveInstancesFromStack();
            CleanupFoldoutStates(activeInstances);
            var rootInstances = BuildTree(activeInstances);
            _showActivityTree = EditorGUILayout.Foldout(_showActivityTree,
                LC.Combine(Lc.Activity,Lc.Procedure, Lc.Tree, Lc.Total, Lc.Is) + $": {activeInstances.Count}",
                true);
            if (_showActivityTree)
            {
                _treeScrollPos = EditorGUILayout.BeginScrollView(_treeScrollPos, 
                    GUILayout.MaxHeight(500f), 
                    GUILayout.ExpandWidth(true));
    
                if (rootInstances.Count == 0)
                    EditorGUILayout.LabelField(LC.Combine(Lc.No,Lc.Procedure, Lc.Exist), EditorStyles.centeredGreyMiniLabel);
                else
                {
                    var childMap = BuildChildMap(activeInstances);
                    EditorGUI.indentLevel++;
                    foreach (var root in rootInstances)
                        DrawProcedureNode(root, 0, childMap);
                    EditorGUI.indentLevel--;
                }
    
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.Space(12);
            
            var pendingList = GetPendingExitInstances();
            _showPendingQueue = EditorGUILayout.Foldout(_showPendingQueue, LC.Combine(Lc.Pending,Lc.Exist, Lc.List, Lc.Total, Lc.Is) + $": {pendingList.Count}", true);
            if (_showPendingQueue)
            {
                if (pendingList.Count == 0)
                    EditorGUILayout.LabelField(LC.Combine(Lc.List, Lc.Is, Lc.Empty), EditorStyles.centeredGreyMiniLabel);
                else
                {
                    EditorGUI.indentLevel++;
                    foreach (var inst in pendingList)
                        DrawPendingItem(inst);
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        #region 数据获取（反射）

        private object GetFieldValue(FieldInfo field) => field?.GetValue(_targetSystem) ?? "N/A";

        private List<object> GetActiveInstancesFromStack()
        {
            var list = new List<object>();
            if (_stackField == null) return list;
            var stackObj = _stackField.GetValue(_targetSystem);
            if (stackObj == null) return list;

            // 通过 IEnumerable 遍历泛型 Stack<ProcedureInstance>
            if (stackObj is not IEnumerable enumerable) return list;

            foreach (var inst in enumerable)
            {
                if (inst == null) continue;
                // 获取 ExitState 字段值（int 类型），0 表示未退出
                var exitStateValue = _exitStateField?.GetValue(inst);
                int exitState = exitStateValue != null ? Convert.ToInt32(exitStateValue) : 0;
                if (exitState == 0)
                    list.Add(inst);
            }
            return list;
        }

        private List<object> GetPendingExitInstances()
        {
            var list = new List<object>();
            if (_pendingExitsField == null) return list;
            var queueObj = _pendingExitsField.GetValue(_targetSystem);
            if (queueObj == null) return list;
            var method = queueObj.GetType().GetMethod("ToArray");
            if (method == null || method.Invoke(queueObj, null) is not Array arr)
                return list;
            
            foreach (var item in arr)
            {
                list.Add(item);
            }
            return list;
        }
        private long GetUid(object inst) => (long)(_uidField?.GetValue(inst) ?? 0L);
        private long GetParentUid(object inst) => (long)(_parentUidField?.GetValue(inst) ?? 0L);
        private int GetDepth(object inst) => (int)(_depthField?.GetValue(inst) ?? 0);
        private uint GetRuntimeVersion(object inst) => (uint)(_runtimeVersionField?.GetValue(inst) ?? 0U);
        private int GetExitQueued(object inst) => (int)(_exitQueuedField?.GetValue(inst) ?? 0);
        private ProcedureState GetState(object inst) => (ProcedureState)(_stateField?.GetValue(inst) ?? ProcedureState.None);
        private Type GetProcedureType(object inst) => _procedureTypeField?.GetValue(inst) as Type;
        private Exception GetExitException(object inst) => _exitExceptionField?.GetValue(inst) as Exception;
        private ProcedureExitType GetExitReason(object inst) => (ProcedureExitType)(_exitReasonField?.GetValue(inst) ?? ProcedureExitType.Completed);
        private IDictionary GetParams(object inst) => _paramsField?.GetValue(inst) as IDictionary;

        private Dictionary<long, List<object>> BuildChildMap(List<object> instances)
        {
            var map = new Dictionary<long, List<object>>();
            foreach (var inst in instances)
            {
                long parent = GetParentUid(inst);
                if (!map.ContainsKey(parent))
                    map[parent] = new List<object>();
                map[parent].Add(inst);
            }
            // 对每个子列表按深度排序
            foreach (var kv in map)
                kv.Value.Sort((a, b) => GetDepth(a).CompareTo(GetDepth(b)));
            return map;
        }

        private List<object> BuildTree(List<object> instances)
        {
            var roots = new List<object>();
            foreach (var inst in instances)
            {
                if (GetParentUid(inst) == 0)
                    roots.Add(inst);
            }
            roots.Sort((a, b) => GetDepth(a).CompareTo(GetDepth(b)));
            return roots;
        }

        private void CleanupFoldoutStates(List<object> activeInstances)
        {
            var validUids = new HashSet<long>(activeInstances.Select(GetUid));
            var keysToRemove = _foldoutStates.Keys.Where(uid => !validUids.Contains(uid)).ToList();
            foreach (var key in keysToRemove)
                _foldoutStates.Remove(key);
        }

        #endregion

        #region UI 绘制

        private void DrawProcedureNode(object inst, int indentLevel, Dictionary<long, List<object>> childMap)
        {
            if (inst == null) return;
            const int indentStep = 20;
            int actualIndent = Mathf.Min(indentLevel * indentStep);
            
            GUILayout.BeginHorizontal();
            GUILayout.Space(actualIndent);
            
            DrawStateLabel(GetState(inst));
            long uid = GetUid(inst);
            var children = childMap.TryGetValue(uid, out var childList) ? childList : new List<object>();
            
            string displayName = $"{GetProcedureType(inst).Name} - [ UID:{uid} ]";
            
            if (children.Count > 0)
            {
                _foldoutStates.TryAdd(uid, true);
                _foldoutStates[uid] = EditorGUILayout.Foldout(_foldoutStates[uid], displayName, true, GUIUtils.ColorText(textColor:GUIUtils.LightYellow));
            }
            else
            {
                EditorGUILayout.LabelField(displayName, GUIUtils.ColorText(textColor:GUIUtils.LightYellow),GUILayout.MinWidth(200));
            }

            GUILayout.EndHorizontal();

            // 详细信息行
            GUILayout.BeginHorizontal();
            GUILayout.Space(actualIndent + indentStep);
            string contents = LC.Combine(Lc.Depth) + $": {GetDepth(inst)} |";
            contents += LC.Combine(Lc.Version) + $": {GetRuntimeVersion(inst)} | ";
            contents += LC.Combine(Lc.Parent) + $"UID: {GetParentUid(inst)} | ";
            contents += LC.Combine(Lc.Await, Lc.Exit) + $": {(GetExitQueued(inst) == 1 ? "√" : "X")}";
            EditorGUILayout.LabelField(contents, EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            // 参数折叠显示
            var paramsDict = GetParams(inst);
            if (paramsDict != null && paramsDict.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(actualIndent + indentStep);

                _foldoutStates.TryAdd(uid, true);
                _foldoutStates[uid] = EditorGUILayout.Foldout(_foldoutStates[uid], LC.Combine(Lc.Param) + $" ({paramsDict.Count})", true);
                GUILayout.EndHorizontal();
                
                if (_foldoutStates[uid])
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(actualIndent + indentStep * 2);
                    GUILayout.BeginVertical();
                    foreach (DictionaryEntry kv in paramsDict)
                    {
                        EditorGUILayout.LabelField($"{kv.Key}: {kv.Value}", EditorStyles.miniLabel);
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
            }

            var exitException = GetExitException(inst);
            if (exitException != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(actualIndent + indentStep);
                EditorGUILayout.LabelField(LC.Combine(Lc.Exception) + $": {exitException.Message}", EditorStyles.miniLabel);
                GUILayout.EndHorizontal();
            }

            if (children.Count > 0 && _foldoutStates.TryGetValue(uid, out bool expanded) && expanded)
            {
                foreach (var child in children)
                {
                    DrawProcedureNode(child, indentLevel + 1, childMap);
                }
            }
        }

        private void DrawStateLabel(ProcedureState state)
        {
            GUIStyle style;
            string stateText;
            switch (state)
            {
                case ProcedureState.Active:
                    style = GUIUtils.ColorText(textColor: Color.green);
                    stateText = LC.Combine(Lc.Active);
                    break;
                case ProcedureState.Entering:
                    style = GUIUtils.ColorText(textColor: Color.yellow);
                    stateText = LC.Combine(Lc.In, Lc.Enter);
                    break;
                case ProcedureState.Suspended:
                    style = GUIUtils.ColorText(textColor: Color.cyan);
                    stateText = LC.Combine(Lc.Suspended);
                    break;
                case ProcedureState.Exiting:
                    style = GUIUtils.ColorText(textColor: Color.red);
                    stateText = LC.Combine(Lc.In, Lc.Exit);
                    break;
                default:
                    style = GUIUtils.ColorText(textColor: Color.white);
                    stateText = LC.Combine(Lc.Exception);
                    break;
            }
            GUILayout.Label(stateText, style,GUILayout.MaxWidth(70));
        }

        private void DrawPendingItem(object inst)
        {
            if (inst == null) return;
            var exception = GetExitException(inst);
            
            GUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField($"{GetProcedureType(inst)} (UID:{GetUid(inst)})", GUILayout.Width(200));
            EditorGUILayout.LabelField(LC.Combine(Lc.Depth) + $":{GetDepth(inst)}", GUILayout.Width(60));
            EditorGUILayout.LabelField(LC.Combine(Lc.Exit, Lc.Reason) + $":{GetExitReason(inst)}", GUILayout.Width(100));
            EditorGUILayout.LabelField(exception != null ? LC.Combine(Lc.Have, Lc.Exception) + $":{exception.Message}" : LC.Combine(Lc.No, Lc.Exception), EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
        }

        #endregion
    }
}
#endif