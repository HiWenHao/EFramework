/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-09 18:59:28
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-09 18:59:28
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using EasyFramework.Systems.Procedure;

public class ProcedureStressWindow : MonoBehaviour
{
    public bool autoStart = true;

    public int startCount = 5000;

    public bool infiniteMode = true;

    private FieldInfo _stackField;

    private FieldInfo _uidField;

    private Vector2 _scroll;

    async void Start()
    {
        ProcedureStressRuntime.Reset();

        var mgr = EF.Procedure;

        mgr.Register<CommercialProcedureBomb>();

        _stackField =
            typeof(ProcedureSystem)
                .GetField(
                    "_instanceStack",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

        _uidField =
            typeof(ProcedureSystem)
                .GetField(
                    "_uidToInstance",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

        if (autoStart)
        {
            StartStress();
        }

        await Cysharp.Threading.Tasks.UniTask.CompletedTask;
    }

    private void StartStress()
    {
        for (int i = 0; i < startCount; i++)
        {
            EF.Procedure
                .Switch<CommercialProcedureBomb>()
                .Forget();
        }
    }

    private void Update()
    {
        if (infiniteMode)
        {
            if (Random.value < 0.8f)
            {
                EF.Procedure
                    .Switch<CommercialProcedureBomb>()
                    .Forget();
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(
            new Rect(10, 10, 420, 700),
            GUI.skin.box);

        _scroll =
            GUILayout.BeginScrollView(
                _scroll,
                GUILayout.Width(420),
                GUILayout.Height(700));

        DrawLabel("=== PROCEDURE STRESS MONITOR ===");

        DrawLabel($"FPS: {(1f / Time.smoothDeltaTime):F1}");

        long mem = System.GC.GetTotalMemory(false);

        DrawLabel($"Memory: {mem / 1024 / 1024} MB");

        DrawLabel($"Stack Count: {GetStackCount()}");

        DrawLabel($"UID Map Count: {GetUidMapCount()}");

        DrawLabel($"Enter Count: {ProcedureStressRuntime.EnterCount}");

        DrawLabel($"Leave Count: {ProcedureStressRuntime.LeaveCount}");

        DrawLabel($"Active Count: {ProcedureStressRuntime.ActiveCount}");

        DrawLabel($"Exception Count: {ProcedureStressRuntime.ExceptionCount}");

        DrawLabel($"Timeout Simulations: {ProcedureStressRuntime.TimeoutSimulation}");

        DrawLabel($"Max Alive: {ProcedureStressRuntime.MaxAlive}");

        DrawLabel(
            $"Running Time: " +
            $"{Time.realtimeSinceStartup - ProcedureStressRuntime.StartTime:F1}s");

        GUILayout.Space(20);

        if (GUILayout.Button("Burst x100"))
        {
            for (int i = 0; i < 100; i++)
            {
                EF.Procedure
                    .Switch<CommercialProcedureBomb>()
                    .Forget();
            }
        }

        if (GUILayout.Button("Burst x1000"))
        {
            for (int i = 0; i < 1000; i++)
            {
                EF.Procedure
                    .Switch<CommercialProcedureBomb>()
                    .Forget();
            }
        }
        
        if (GUILayout.Button("Storm x10000"))
        {
            for (int i = 0; i < 10000; i++)
            {
                EF.Procedure
                    .Switch<CommercialProcedureBomb>()
                    .Forget();
            }
        }

        if (GUILayout.Button("Exit Storm"))
        {
            for (int i = 0; i < 5000; i++)
            {
                EF.Procedure
                    .Switch<CommercialProcedureBomb>()
                    .Forget();
            }
        }

        if (GUILayout.Button("GC Collect"))
        {
            System.GC.Collect();
        }

        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private int GetStackCount()
    {
        var stack =
            _stackField.GetValue(EF.Procedure)
                as System.Collections.ICollection;

        return stack?.Count ?? 0;
    }

    private int GetUidMapCount()
    {
        var map =
            _uidField.GetValue(EF.Procedure)
                as System.Collections.IDictionary;

        return map?.Count ?? 0;
    }

    private void DrawLabel(string text)
    {
        GUILayout.Label(text);
    }
}