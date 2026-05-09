using System;
using System.Collections.Generic;
using EasyFramework.Systems.Procedure;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class ProcedureHierarchyVisualizer : MonoBehaviour
{
    [SerializeField] private Transform rootDisplayParent;

    private readonly Dictionary<long, GameObject> _nodes = new();

    private IDisposable _enterToken;
    private IDisposable _activateToken;
    private IDisposable _suspendToken;
    private IDisposable _resumeToken;
    private IDisposable _leaveToken;

    private async void Start()
    {
        await UniTask.DelayFrame(3);

        if (rootDisplayParent == null)
            rootDisplayParent = transform;

        var eventSystem = EF.Events;

        _enterToken = eventSystem.Subscribe((ProcedureEnterEvent e) => OnEnter(e));
        _activateToken = eventSystem.Subscribe((ProcedureActivateEvent e) => OnActivate(e));
        _suspendToken = eventSystem.Subscribe((ProcedureSuspendEvent e) => OnSuspend(e));
        _resumeToken = eventSystem.Subscribe((ProcedureResumeEvent e) => OnResume(e));
        _leaveToken = eventSystem.Subscribe((ProcedureExitEvent e) => OnLeave(e));
    }

    private void OnDisable()
    {
        _enterToken?.Dispose();
        _activateToken?.Dispose();
        _suspendToken?.Dispose();
        _resumeToken?.Dispose();
        _leaveToken?.Dispose();

        foreach (var node in _nodes.Values)
        {
            if (node != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(node);
#else
                Destroy(node);
#endif
            }
        }

        _nodes.Clear();
    }

    private void OnEnter(ProcedureEnterEvent evt)
    {
        var go = new GameObject();

        go.name =
            $"{evt.ProcedureType.Name}#{evt.Uid} [Entering]";

        if (evt.ParentUid != 0 &&
            _nodes.TryGetValue(evt.ParentUid, out var parent))
        {
            go.transform.SetParent(parent.transform, false);
        }
        else
        {
            go.transform.SetParent(rootDisplayParent, false);
        }

        _nodes[evt.Uid] = go;
    }

    private void OnActivate(ProcedureActivateEvent evt)
    {
        SetState(evt.Uid, "Active");
    }

    private void OnSuspend(ProcedureSuspendEvent evt)
    {
        SetState(evt.Uid, "Suspended");
    }

    private void OnResume(ProcedureResumeEvent evt)
    {
        SetState(evt.Uid, "Active");
    }

    private void OnLeave(ProcedureExitEvent evt)
    {
        if (!_nodes.TryGetValue(evt.Uid, out var go))
            return;

        _nodes.Remove(evt.Uid);

#if UNITY_EDITOR
        DestroyImmediate(go);
#else
        go.SetActive(false);
        Destroy(go);
#endif
    }

    private void SetState(long uid, string state)
    {
        if (!_nodes.TryGetValue(uid, out var go))
            return;

        int idx = go.name.IndexOf('[');

        string prefix =
            idx >= 0
                ? go.name.Substring(0, idx).TrimEnd()
                : go.name;

        go.name = $"{prefix} [{state}]";
    }
}