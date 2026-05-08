using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using EasyFramework.Managers.Procedure;

public class ProcedureStressMonitor : MonoBehaviour
{
    private FieldInfo _stackField;
    private FieldInfo _uidField;

    private float _timer;

    private long _lastMemory;

    private void Start()
    {
        var mgr = ProcedureManager.Instance;

        _stackField = typeof(ProcedureManager)
            .GetField(
                "_instanceStack",
                BindingFlags.NonPublic |
                BindingFlags.Instance);

        _uidField = typeof(ProcedureManager)
            .GetField(
                "_uidToInstance",
                BindingFlags.NonPublic |
                BindingFlags.Instance);

        _lastMemory = GC.GetTotalMemory(false);

        StartCoroutine(MonitorCoroutine());
    }

    IEnumerator MonitorCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            PrintStats();
        }
    }

    private void PrintStats()
    {
        var stack =
            _stackField.GetValue(ProcedureManager.Instance)
                as System.Collections.ICollection;

        var uidMap =
            _uidField.GetValue(ProcedureManager.Instance)
                as System.Collections.IDictionary;

        long memory = GC.GetTotalMemory(false);

        long delta = memory - _lastMemory;

        _lastMemory = memory;

        Debug.Log(
            $"<color=orange>" +
            $"[MONITOR] " +
            $"Stack={stack?.Count} " +
            $"UIDMap={uidMap?.Count} " +
            $"Memory={memory / 1024 / 1024}MB " +
            $"Delta={delta / 1024}KB " +
            $"FPS={(1f / Time.smoothDeltaTime):F1}" +
            $"</color>");
    }
}