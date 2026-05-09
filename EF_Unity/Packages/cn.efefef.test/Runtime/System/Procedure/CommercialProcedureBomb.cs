/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-09 18:57:45
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-09 18:57:45
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using EasyFramework.Managers.Procedure;

public class CommercialProcedureBomb : ProcedureBase
{
    private static int GlobalId;

    private int _id;

    private int _frame;

    private bool _spawned;

    private bool _exitTriggered;

    protected override async UniTask OnEnterAsync()
    {
        _id = ++GlobalId;

        ProcedureStressRuntime.EnterCount++;
        ProcedureStressRuntime.ActiveCount++;

        int alive =
            ++ProcedureStressRuntime.CurrentAlive;

        if (alive > ProcedureStressRuntime.MaxAlive)
            ProcedureStressRuntime.MaxAlive = alive;

        // 超高速 Enter
        await UniTask.Yield(PlayerLoopTiming.Update);

        // 随机异常
        if (UnityEngine.Random.value < 0.03f)
        {
            ProcedureStressRuntime.ExceptionCount++;
            throw new Exception($"Enter Exception #{_id}");
        }

        // 超高速 timeout 模拟
        if (UnityEngine.Random.value < 0.02f)
        {
            ProcedureStressRuntime.TimeoutSimulation++;

            while (true)
            {
                await UniTask.Yield();
            }
        }
    }

    protected override async UniTask OnLeaveAsync()
    {
        ProcedureStressRuntime.LeaveCount++;
        ProcedureStressRuntime.ActiveCount--;
        ProcedureStressRuntime.CurrentAlive--;

        // Leave 异常
        if (UnityEngine.Random.value < 0.02f)
        {
            throw new Exception($"Leave Exception #{_id}");
        }

        // Leave 卡死
        if (UnityEngine.Random.value < 0.01f)
        {
            while (true)
            {
                await UniTask.Yield();
            }
        }

        await UniTask.Yield();
    }

    public override void OnUpdate(float elapse, float realElapse)
    {
        _frame++;

        if (!_spawned)
        {
            _spawned = true;

            SpawnChaos().Forget();
        }

        // 高频随机退出
        if (!_exitTriggered &&
            UnityEngine.Random.value < 0.08f)
        {
            _exitTriggered = true;

            Ctx.EndProcedure().Forget();
        }

        // 仅活几帧
        if (_frame >= UnityEngine.Random.Range(2, 12))
        {
            if (!_exitTriggered)
            {
                _exitTriggered = true;

                Ctx.EndProcedure().Forget();
            }
        }
    }

    private async UniTaskVoid SpawnChaos()
    {
        if (Depth >= 10)
            return;

        await UniTask.Yield();

        if (Token.IsCancellationRequested)
            return;

        int mode = UnityEngine.Random.Range(0, 5);

        switch (mode)
        {
            // 串行深度
            case 0:
            {
                await Ctx.StartSubProcedure<CommercialProcedureBomb>();
                break;
            }

            // 并发子流程
            case 1:
            {
                List<UniTask> tasks = new();

                int count = UnityEngine.Random.Range(2, 8);

                for (int i = 0; i < count; i++)
                {
                    tasks.Add(
                        Ctx.StartSubProcedure<CommercialProcedureBomb>());
                }

                await UniTask.WhenAll(tasks);

                break;
            }

            // Switch 风暴
            case 2:
            {
                int count = UnityEngine.Random.Range(2, 6);

                for (int i = 0; i < count; i++)
                {
                    ProcedureSystem.Instance
                        .Switch<CommercialProcedureBomb>()
                        .Forget();
                }

                break;
            }

            // 同帧 Exit Storm
            case 3:
            {
                for (int i = 0; i < 3; i++)
                {
                    Ctx.EndProcedure().Forget();
                }

                break;
            }

            // 空
            case 4:
                break;
        }
    }
}