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
using System.Reflection;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Procedure
{
    [CustomEditor(typeof(ProcedureManager))]
    public class ProcedureManagerInspector : EFInspectorBase<ProcedureManager>
    {
        private const int MaxRenderDepth = 16;

        private FieldInfo _stackField;
        private FieldInfo _factoriesField;

        private Type _procedureInstanceType;
        private FieldInfo _uidField;
        private FieldInfo _stateField;
        private FieldInfo _depthField;
        private FieldInfo _exitStateField;
        private FieldInfo _parentUidField;
        private FieldInfo _exitQueuedField;
        private FieldInfo _exitReasonField;
        private FieldInfo _procedureTypeField;
        private FieldInfo _exitExceptionField;
        private FieldInfo _runtimeVersionField;

        private bool _reflectionReady;
        private bool _showActivityTree = true;
        private bool _showRegisteredProcedures = true;

        private Dictionary<long, bool> _nodeFoldouts;

        private Vector2 _treeScrollPos;

        private Dictionary<Type, Func<IProcedure>> _cachedFactories;

        protected override string Title => LC.Combine("Core.ProcedureRunningMonitor");

        protected override void OnEditorEnable()
        {
            _nodeFoldouts = new Dictionary<long, bool>();
            _cachedFactories = new Dictionary<Type, Func<IProcedure>>();
            _reflectionReady = false;

            try
            {
                var sysType = typeof(ProcedureManager);
                _stackField = sysType.GetField("_instanceStack", BindingFlags.NonPublic | BindingFlags.Instance);
                _factoriesField = sysType.GetField("_factories", BindingFlags.NonPublic | BindingFlags.Instance);

                // ProcedureInstance 在 EasyFramework.Procedure 命名空间，且为 internal，只能反射获取
                _procedureInstanceType = sysType.Assembly.GetType("EasyFramework.Procedure.ProcedureInstance", false);
                if (_procedureInstanceType == null)
                {
                    D.Error("[ProcedureSystemInspector] 无法获取 ProcedureInstance 类型");
                    return;
                }

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

                if (_stackField == null || _factoriesField == null ||
                    _uidField == null || _parentUidField == null || _depthField == null ||
                    _exitStateField == null || _stateField == null || _procedureTypeField == null)
                {
                    D.Error("[ProcedureSystemInspector] 关键反射字段缺失，请检查 ProcedureManager 和 ProcedureInstance 的字段定义");
                    return;
                }

                _reflectionReady = true;
                RefreshFactoriesCache();
            }
            catch (Exception e)
            {
                _reflectionReady = false;
                D.Error($"[ProcedureSystemInspector] 反射初始化失败: {e}");
            }

            FieldInfo GetInstanceField(string instanceName) => _procedureInstanceType?.GetField(instanceName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        protected override void OnEditorUpdate()
        {
            RefreshFactoriesCache();
        }

        protected override void OnEditorGUI()
        {
            if (!_reflectionReady)
            {
                EditorGUILayout.HelpBox(LC.Combine("Core.ReflectErrorCheckCode"),
                    MessageType.Error);
                return;
            }

            // 只枚举一次栈，三处共用同一份快照，避免多次分配与视图不一致
            var activeInstances = GetActiveInstancesFromStack();
            DrawGlobalInfo(activeInstances);
            DrawRegisteredProcedures(activeInstances);
            DrawRuntimeMonitor(activeInstances);
        }

        private void RefreshFactoriesCache()
        {
            if (!_reflectionReady || _factoriesField == null) return;
            _cachedFactories ??= new Dictionary<Type, Func<IProcedure>>();
            _cachedFactories.Clear();
            if (_factoriesField.GetValue(Target) is not IDictionary dict) return;
            foreach (DictionaryEntry entry in dict)
            {
                if (entry is { Key: Type t, Value: Func<IProcedure> factory })
                    _cachedFactories[t] = factory;
            }
        }

        // ---------- 全局信息 ----------
        private void DrawGlobalInfo(List<object> activeInstances)
        {
            EditorGUILayout.BeginVertical("box");
            int stackDepth = activeInstances.Count;
            var activeTop = stackDepth > 0 ? activeInstances[^1] : null; // 栈顶

            if (activeTop != null)
            {
                EditorGUILayout.LabelField($"{LC.Combine("Core.CurrentDepth")}: {stackDepth}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"● {LC.Combine("Core.CurrentActiveProcedure")}   {GetProcedureType(activeTop)?.Name}   UID [ {GetUid(activeTop)} ]",
                    GUIUtils.ColorText(textColor: Color.green));
            }
            else
            {
                EditorGUILayout.LabelField(LC.Combine("Core.NoActiveProcedure"),
                    EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
                return;
            }

            // 显示系统配置参数（直接从 target 中读取）
            var sys = (ProcedureManager)target;
            var maxDepthField =
                typeof(ProcedureManager).GetField("maxDepth", BindingFlags.NonPublic | BindingFlags.Instance);
            var maxChainField =
                typeof(ProcedureManager).GetField("maxChainRepeat", BindingFlags.NonPublic | BindingFlags.Instance);
            var timeoutField = typeof(ProcedureManager).GetField("defaultTimeoutSeconds",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var leaveTimeoutField = typeof(ProcedureManager).GetField("leaveTimeoutSeconds",
                BindingFlags.NonPublic | BindingFlags.Instance);
            int maxDepth = maxDepthField != null ? (int)maxDepthField.GetValue(sys) : 0;
            int maxChain = maxChainField != null ? (int)maxChainField.GetValue(sys) : 0;
            float enterTimeout = timeoutField != null ? (float)timeoutField.GetValue(sys) : 0;
            float leaveTimeout = leaveTimeoutField != null ? (float)leaveTimeoutField.GetValue(sys) : 0;

            EditorGUILayout.LabelField(
                $"{LC.Combine("Config")}: {LC.Combine("Core.MaxDepth")}={maxDepth}, {LC.Combine("Core.ChainRepetition")}={maxChain}, {LC.Combine("Core.EnterTimeout")}={enterTimeout}s, {LC.Combine("Core.ExitTimeout")}={leaveTimeout}s",
                GUIUtils.SmallNote());
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // ---------- 运行时活动树 ----------
        private void DrawRuntimeMonitor(List<object> activeInstances)
        {
            EditorGUILayout.Space(24);
            EditorGUILayout.BeginVertical("box");

            CleanupFoldoutStates(activeInstances);
            var rootInstances = BuildTree(activeInstances);
            _showActivityTree = EditorGUILayout.Foldout(_showActivityTree,
                LC.Combine("Core.ActiveProcedureTreeTotal") + $": {activeInstances.Count}",
                true);
            if (_showActivityTree)
            {
                _treeScrollPos = EditorGUILayout.BeginScrollView(_treeScrollPos,
                    GUILayout.MaxHeight(500f),
                    GUILayout.ExpandWidth(true));

                if (rootInstances.Count == 0)
                    EditorGUILayout.LabelField(LC.Combine("Core.NoProcedureExist"),
                        EditorStyles.centeredGreyMiniLabel);
                else
                {
                    var childMap = BuildChildMap(activeInstances);
                    EditorGUI.indentLevel++;
                    var visited = new HashSet<long>();
                    foreach (var root in rootInstances)
                        DrawProcedureNode(root, 0, childMap, visited);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        // ---------- 注册流程列表（显示所有存活实例）----------
        private void DrawRegisteredProcedures(List<object> allInstances)
        {
            if (_cachedFactories == null || _cachedFactories.Count == 0)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            _showRegisteredProcedures = EditorGUILayout.Foldout(_showRegisteredProcedures,
                LC.Combine("Core.ProcedureAlreadyRegistered") + $" ({_cachedFactories.Count})", true);
            if (!_showRegisteredProcedures)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            // 按类型分组实例；GetProcedureType 可能返回 null，跳过即可
            var instancesByType = new Dictionary<Type, List<object>>();
            foreach (var inst in allInstances)
            {
                var procType = GetProcedureType(inst);
                if (procType == null)
                    continue;

                if (!instancesByType.TryGetValue(procType, out var list))
                {
                    list = new List<object>();
                    instancesByType[procType] = list;
                }

                list.Add(inst);
            }

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
                        EditorGUILayout.LabelField($"  {LC.Combine("Self")} UID:[ {uid} ]", GUILayout.Width(100));
                        EditorGUILayout.LabelField($"{LC.Combine("Depth")}: [ {depth} ]", GUILayout.Width(80));
                        EditorGUILayout.LabelField($"{LC.Combine("State")}:", GUILayout.Width(50));
                        EditorGUILayout.LabelField(stateLabel, DrawStateGUIStyle(state), GUILayout.Width(100));
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($"  ({LC.Combine("Core.NoSurvivingInstance")})",
                        EditorStyles.miniLabel);
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
            var stackObj = _stackField.GetValue(Target);
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
            {
                if (GetParentUid(inst) == 0)
                    roots.Add(inst);
            }

            roots.Sort((a, b) => GetDepth(a).CompareTo(GetDepth(b)));
            return roots;
        }

        private void CleanupFoldoutStates(List<object> activeInstances)
        {
            var validUids = new HashSet<long>();
            foreach (var inst in activeInstances)
                validUids.Add(GetUid(inst));

            List<long> keysToRemove = null;
            foreach (var key in _nodeFoldouts.Keys)
            {
                if (validUids.Contains(key))
                    continue;

                keysToRemove ??= new List<long>();
                keysToRemove.Add(key);
            }

            if (keysToRemove == null)
                return;

            foreach (var key in keysToRemove)
                _nodeFoldouts.Remove(key);
        }

        // ---------- UI 绘制 ----------
        private void DrawProcedureNode(object inst, int indentLevel, Dictionary<long, List<object>> childMap, HashSet<long> visited)
        {
            if (inst == null || indentLevel > MaxRenderDepth)
                return;

            long uid = GetUid(inst);
            // 防御：Uid 读取异常或数据成环时禁止递归绘制，避免栈溢出
            if (!visited.Add(uid))
                return;

            const int indentStep = 20;
            int actualIndent = Mathf.Min(indentLevel * indentStep, 200);

            GUILayout.BeginHorizontal();
            GUILayout.Space(actualIndent);

            var state = GetState(inst);
            GUILayout.Label(GetStateLabel(state), DrawStateGUIStyle(state), GUILayout.MaxWidth(70));
            var children = childMap.TryGetValue(uid, out var childList) ? childList : new List<object>();

            string displayName = $"{GetProcedureType(inst)?.Name ?? "<unknown>"} - [ UID:{uid} ]";

            if (children.Count > 0)
            {
                _nodeFoldouts.TryAdd(uid, true);
                _nodeFoldouts[uid] = EditorGUILayout.Foldout(_nodeFoldouts[uid], displayName, true,
                    GUIUtils.ColorText(textColor: GUIUtils.LightYellow));
            }
            else
                EditorGUILayout.LabelField(displayName, GUIUtils.ColorText(textColor: GUIUtils.LightYellow), GUILayout.MinWidth(200));

            GUILayout.EndHorizontal();

            // 详细信息行
            GUILayout.BeginHorizontal();
            GUILayout.Space(actualIndent + indentStep);
            string contents = LC.Combine("Depth") + $": {GetDepth(inst)} | " +
                              LC.Combine("Version") + $": {GetRuntimeVersion(inst)} | " +
                              LC.Combine("Parent") + $"UID: {GetParentUid(inst)} | " +
                              LC.Combine("Core.AwaitExit") + $": {(GetExitQueued(inst) == 1 ? "√" : "X")}";
            EditorGUILayout.LabelField(contents, EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            var exitException = GetExitException(inst);
            if (exitException != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(actualIndent + indentStep);
                EditorGUILayout.LabelField(LC.Combine("Exception") + $": {exitException.Message}", EditorStyles.miniLabel);
                GUILayout.EndHorizontal();
            }

            if (children.Count > 0 && _nodeFoldouts.TryGetValue(uid, out bool expanded) && expanded)
            {
                foreach (var child in children)
                    DrawProcedureNode(child, indentLevel + 1, childMap, visited);
            }
        }

        private string GetStateLabel(ProcedureState state)
        {
            return state switch
            {
                ProcedureState.Active => LC.Combine("Active"),
                ProcedureState.Entering => LC.Combine("Core.InEnter"),
                ProcedureState.Suspended => LC.Combine("Suspended"),
                ProcedureState.Exiting => LC.Combine("Core.InExit"),
                _ => LC.Combine("Exception")
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