using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using EasyFramework.Managers.Procedure;

public class CommercialStressProcedure : ProcedureBase
{
    private static int GlobalId;
    private int _id;
    private float _timer;
    private bool _spawned;

    protected override async UniTask OnEnterAsync()
    {
        _id = ++GlobalId;
        Debug.Log($"[ENTER] {_id}");
        await UniTask.Delay(UnityEngine.Random.Range(5, 100), cancellationToken: Token);
        if (UnityEngine.Random.value < 0.05f)
            throw new Exception("Stress enter exception");
    }

    protected override async UniTask OnLeaveAsync()
    {
        Debug.Log($"[LEAVE] {_id}");
        await UniTask.Delay(10, cancellationToken: Token);
    }

    public override void OnUpdate(float elapse, float realElapse)
    {
        _timer += realElapse;
        if (!_spawned)
        {
            _spawned = true;
            SpawnChildren().Forget();
            
        }
        if (_timer >= 3f)
        {
            Ctx.EndProcedure().Forget();
        }
    }

    private async UniTaskVoid SpawnChildren()
    {
        // 递归启动子流程以实现深度加压
        if (Depth >= 10) return;
        await UniTask.Delay(50, cancellationToken: Token);
        if (Token.IsCancellationRequested)
            return;
        // StartSubProcedure 现在等待子流程退出，从而形成串行深度嵌套
        await Ctx.StartSubProcedure<CommercialStressProcedure>();
    }
}