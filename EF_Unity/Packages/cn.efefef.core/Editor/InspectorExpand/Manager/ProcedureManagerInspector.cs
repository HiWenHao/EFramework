/*
 * ================================================
 * Describe:      用来显示流程运行时的监控面板
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

namespace EasyFramework.Managers.Procedure.Editor
{
    [CustomEditor(typeof(ProcedureManager))]
    public class ProcedureManagerInspector : UnityEditor.Editor
    {
        private ProcedureManager _targetSystem;

        private FieldInfo _stackField;
        private FieldInfo _factoriesField;

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
        private bool _showRegisteredProcedures = true;

        private readonly Dictionary<long, bool> _nodeFoldouts = new();
        private readonly Dictionary<long, bool> _paramFoldouts = new();

        private const double RepaintInterval = 0.3f;
        private Vector2 _treeScrollPos;

        private Dictionary<Type, Func<IProcedure>> _cachedFactories;

        private void OnEnable()
        {
            _targetSystem = (ProcedureManager)target;
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
            if (_targetSystem == null) return;

            if (!(EditorApplication.timeSinceStartup - _lastRepaintTime >= RepaintInterval)) return;
            _lastRepaintTime = EditorApplication.timeSinceStartup;
            RefreshFactoriesCache();
            Repaint();
        }

        private void RefreshFactoriesCache()
        {
            if (!_reflectionReady || _factoriesField == null) return;
            var factoriesObj = _factoriesField.GetValue(_targetSystem);
            if (factoriesObj is IDictionary dict)
            {
                _cachedFactories = new Dictionary<Type, Func<IProcedure>>();
                foreach (DictionaryEntry entry in dict)
                {
                    if (entry.Key is Type t && entry.Value is Func<IProcedure> factory)
                        _cachedFactories[t] = factory;
                }
            }
            else
            {
                _cachedFactories = new Dictionary<Type, Func<IProcedure>>();
            }
        }

        private void CacheReflectionInfo()
        {
            try
            {
                var sysType = typeof(ProcedureManager);
                _stackField = sysType.GetField("_instanceStack", BindingFlags.NonPublic | BindingFlags.Instance);
                _factoriesField = sysType.GetField("_factories", BindingFlags.NonPublic | BindingFlags.Instance);

                _procedureInstanceType = sysType.Assembly.GetType("EasyFramework.Managers.Procedure.ProcedureInstance");
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

                if (_stackField == null || _factoriesField == null ||
                    _procedureInstanceType == null || _uidField == null || _parentUidField == null || _depthField == null)
                    throw new Exception("关键反射字段缺失，请检查 ProcedureSystem 和 ProcedureInstance 的字段定义");

                _reflectionReady = true;
                RefreshFactoriesCache();
            }
            catch (Exception e)
            {
                _reflectionReady = false;
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
            EditorGUI.LabelField(titleRect, LC.Combine(Lc.Procedure, Lc.Running, Lc.Monitor), GUIUtils.InspectorTitle());

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Non, Lc.Running, Lc.Not, Lc.Data), MessageType.Info);
                return;
            }

            if (!_reflectionReady)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Reflect, Lc.Error, Lc.Please, Lc.Check, Lc.Code), MessageType.Error);
                return;
            }

            DrawGlobalInfo();           // 新增全局信息
            DrawRegisteredProcedures();
            DrawRuntimeMonitor();       // 活动树

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(Lc.Manually, Lc.Refresh), GUILayout.Width(80)))
                Repaint();
            EditorGUILayout.LabelField(LC.Combine(Lc.Auto, Lc.Refresh, Lc.Interval) + $": {RepaintInterval:F1} s", GUIUtils.SmallNote());
            EditorGUILayout.EndHorizontal();
        }

        // ---------- 全局信息 ----------
        private void DrawGlobalInfo()
        {
            EditorGUILayout.BeginVertical("box");
            var activeInstances = GetActiveInstancesFromStack();
            int stackDepth = activeInstances.Count;
            var activeTop = stackDepth > 0 ? activeInstances[^1] : null; // 栈顶

            if (activeTop != null)
            {
                EditorGUILayout.LabelField($"{LC.Combine(Lc.Current, Lc.Depth)}: {stackDepth}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"● {LC.Combine(Lc.Current, Lc.Active, Lc.Procedure)}   {GetProcedureType(activeTop)?.Name}   UID [ {GetUid(activeTop)} ]", GUIUtils.ColorText(textColor: Color.green));
            }
            else
            {
                EditorGUILayout.LabelField(LC.Combine(Lc.Current, Lc.No, Lc.Active, Lc.Procedure), EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
                return;
            }

            // 显示系统配置参数（直接从 target 中读取）
            var sys = (ProcedureManager)target;
            var maxDepthField = typeof(ProcedureManager).GetField("maxDepth", BindingFlags.NonPublic | BindingFlags.Instance);
            var maxChainField = typeof(ProcedureManager).GetField("maxChainRepeat", BindingFlags.NonPublic | BindingFlags.Instance);
            var timeoutField = typeof(ProcedureManager).GetField("defaultTimeoutSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
            var leaveTimeoutField = typeof(ProcedureManager).GetField("leaveTimeoutSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
            int maxDepth = maxDepthField != null ? (int)maxDepthField.GetValue(sys) : 0;
            int maxChain = maxChainField != null ? (int)maxChainField.GetValue(sys) : 0;
            float enterTimeout = timeoutField != null ? (float)timeoutField.GetValue(sys) : 0;
            float leaveTimeout = leaveTimeoutField != null ? (float)leaveTimeoutField.GetValue(sys) : 0;

            EditorGUILayout.LabelField($"{LC.Combine(Lc.Config)}: {LC.Combine(Lc.Max, Lc.Depth)}={maxDepth}, {LC.Combine(Lc.Chain, Lc.Repetition)}={maxChain}, {LC.Combine(Lc.Enter, Lc.Timeout)}={enterTimeout}s, {LC.Combine(Lc.Exit, Lc.Timeout)}={leaveTimeout}s", GUIUtils.SmallNote());
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // ---------- 运行时活动树 ----------
        private void DrawRuntimeMonitor()
        {
            EditorGUILayout.Space(24);
            EditorGUILayout.BeginVertical("box");

            var activeInstances = GetActiveInstancesFromStack();
            CleanupFoldoutStates(activeInstances);
            var rootInstances = BuildTree(activeInstances);
            _showActivityTree = EditorGUILayout.Foldout(_showActivityTree,
                LC.Combine(Lc.Activity, Lc.Procedure, Lc.Tree, Lc.Total, Lc.Is) + $": {activeInstances.Count}",
                true);
            if (_showActivityTree)
            {
                _treeScrollPos = EditorGUILayout.BeginScrollView(_treeScrollPos,
                    GUILayout.MaxHeight(500f),
                    GUILayout.ExpandWidth(true));

                if (rootInstances.Count == 0)
                    EditorGUILayout.LabelField(LC.Combine(Lc.No, Lc.Procedure, Lc.Exist), EditorStyles.centeredGreyMiniLabel);
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
            EditorGUILayout.EndVertical();
        }

        // ---------- 注册流程列表（显示所有存活实例）----------
        // 注册列表（已优化）
        private void DrawRegisteredProcedures()
        {
            if (_cachedFactories == null || _cachedFactories.Count == 0)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            _showRegisteredProcedures = EditorGUILayout.Foldout(_showRegisteredProcedures,
                LC.Combine(Lc.Already, Lc.Register, Lc.Procedure) + $" ({_cachedFactories.Count})", true);
            if (!_showRegisteredProcedures)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            var allInstances = GetActiveInstancesFromStack(); // 所有存活实例
            // 按类型分组实例
            var instancesByType = allInstances.GroupBy(GetProcedureType).ToDictionary(g => g.Key, g => g.ToList());

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var kv in _cachedFactories)
            {
                var procType = kv.Key;
                EditorGUILayout.BeginVertical("CN Box", GUILayout.MinHeight(30));
                EditorGUILayout.LabelField(procType.Name, GUIUtils.ColorText(textColor: GUIUtils.LightYellow));
                EditorGUI.indentLevel++;

                if (instancesByType.TryGetValue(procType, out var instances) && instances.Count > 0)
                {
                    foreach (var inst in instances)
                    {
                        var state = GetState(inst);
                        var stateLabel = GetStateLabel(state);
                        var uid = GetUid(inst);
                        var depth = GetDepth(inst);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  {LC.Combine(Lc.Self)} UID:[ {uid} ]", GUILayout.Width(100));
                        EditorGUILayout.LabelField($"{LC.Combine(Lc.Depth)}: [ {depth} ]", GUILayout.Width(80));
                        EditorGUILayout.LabelField($"{LC.Combine(Lc.State)}:", GUILayout.Width(50));
                        EditorGUILayout.LabelField(stateLabel, DrawStateGUIStyle(state), GUILayout.Width(100));
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($"  ({LC.Combine(Lc.No, Lc.Surviving, Lc.Instance)})", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        // ---------- 数据获取 ----------
        private List<object> GetActiveInstancesFromStack()
        {
            var list = new List<object>();
            if (_stackField == null) return list;
            var stackObj = _stackField.GetValue(_targetSystem);
            if (stackObj == null) return list;
            if (stackObj is not IEnumerable enumerable) return list;

            // 栈是 LIFO，为了显示顺序，反转一下（栈顶显示在最下面）
            var temp = new List<object>();
            foreach (var inst in enumerable)
            {
                if (inst == null) continue;
                var exitStateValue = _exitStateField?.GetValue(inst);
                int exitState = exitStateValue != null ? Convert.ToInt32(exitStateValue) : 0;
                if (exitState == 0)
                    temp.Add(inst);
            }
            temp.Reverse(); // 让根流程在顶部，子流程在下方，符合树形展示习惯
            return temp;
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
            foreach (var kv in map)
                kv.Value.Sort((a, b) => GetDepth(a).CompareTo(GetDepth(b)));
            return map;
        }

        private List<object> BuildTree(List<object> instances)
        {
            var roots = new List<object>();
            foreach (var inst in instances)
                if (GetParentUid(inst) == 0)
                    roots.Add(inst);
            roots.Sort((a, b) => GetDepth(a).CompareTo(GetDepth(b)));
            return roots;
        }

        private void CleanupFoldoutStates(List<object> activeInstances)
        {
            var validUids = new HashSet<long>(activeInstances.Select(GetUid));
            var keysToRemove = _nodeFoldouts.Keys.Where(uid => !validUids.Contains(uid)).ToList();
            foreach (var key in keysToRemove)
                _nodeFoldouts.Remove(key);

            var paramKeysToRemove = _paramFoldouts.Keys.Where(uid => !validUids.Contains(uid)).ToList();
            foreach (var key in paramKeysToRemove)
                _paramFoldouts.Remove(key);
        }

        // ---------- UI 绘制 ----------
        private void DrawProcedureNode(object inst, int indentLevel, Dictionary<long, List<object>> childMap)
        {
            if (inst == null) return;
            const int indentStep = 20;
            int actualIndent = Mathf.Min(indentLevel * indentStep, 200);

            GUILayout.BeginHorizontal();
            GUILayout.Space(actualIndent);

            var state = GetState(inst);
            GUILayout.Label(GetStateLabel(state), DrawStateGUIStyle(state), GUILayout.MaxWidth(70));
            long uid = GetUid(inst);
            var children = childMap.TryGetValue(uid, out var childList) ? childList : new List<object>();

            string displayName = $"{GetProcedureType(inst).Name} - [ UID:{uid} ]";

            if (children.Count > 0)
            {
                _nodeFoldouts.TryAdd(uid, true);
                _nodeFoldouts[uid] = EditorGUILayout.Foldout(_nodeFoldouts[uid], displayName, true, GUIUtils.ColorText(textColor: GUIUtils.LightYellow));
            }
            else
            {
                EditorGUILayout.LabelField(displayName, GUIUtils.ColorText(textColor: GUIUtils.LightYellow), GUILayout.MinWidth(200));
            }

            GUILayout.EndHorizontal();

            // 详细信息行
            GUILayout.BeginHorizontal();
            GUILayout.Space(actualIndent + indentStep);
            string contents = LC.Combine(Lc.Depth) + $": {GetDepth(inst)} | " +
                              LC.Combine(Lc.Version) + $": {GetRuntimeVersion(inst)} | " +
                              LC.Combine(Lc.Parent) + $"UID: {GetParentUid(inst)} | " +
                              LC.Combine(Lc.Await, Lc.Exit) + $": {(GetExitQueued(inst) == 1 ? "√" : "X")}";
            EditorGUILayout.LabelField(contents, EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            // 参数折叠
            var paramsDict = GetParams(inst);
            if (paramsDict != null && paramsDict.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(actualIndent + indentStep);
                _paramFoldouts.TryAdd(uid, false);
                _paramFoldouts[uid] = EditorGUILayout.Foldout(_paramFoldouts[uid], LC.Combine(Lc.Param) + $" ({paramsDict.Count})", true);
                GUILayout.EndHorizontal();

                if (_paramFoldouts[uid])
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

            if (children.Count > 0 && _nodeFoldouts.TryGetValue(uid, out bool expanded) && expanded)
            {
                foreach (var child in children)
                    DrawProcedureNode(child, indentLevel + 1, childMap);
            }
        }

        private string GetStateLabel(ProcedureState state)
        {
            return state switch
            {
                ProcedureState.Active => LC.Combine(Lc.Active),
                ProcedureState.Entering => LC.Combine(Lc.In, Lc.Enter),
                ProcedureState.Suspended => LC.Combine(Lc.Suspended),
                ProcedureState.Exiting => LC.Combine(Lc.In, Lc.Exit),
                _ => LC.Combine(Lc.Exception)
            };
        }

        private GUIStyle DrawStateGUIStyle(ProcedureState state)
        {
            return state switch
            {
                ProcedureState.Active => GUIUtils.ColorText(textColor: Color.green),
                ProcedureState.Entering => GUIUtils.ColorText(textColor: Color.yellow),
                ProcedureState.Suspended => GUIUtils.ColorText(textColor: Color.cyan),
                ProcedureState.Exiting => GUIUtils.ColorText(textColor: Color.red),
                _ => GUIUtils.ColorText(textColor: Color.gray)
            };
        }
    }
}
#endif