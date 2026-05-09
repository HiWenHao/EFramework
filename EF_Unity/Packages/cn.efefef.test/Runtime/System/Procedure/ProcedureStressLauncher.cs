using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Systems.Procedure;
using UnityEngine;

public class ProcedureStressLauncher : MonoBehaviour
{
    [Header("并发根流程数量")]
    public int concurrentRoots = 1000;

    [Header("每波启动间隔(ms)")]
    public int launchInterval = 20;

    [Header("是否无限压力")]
    public bool infiniteStress = false;

    [Header("每波生成数量")]
    public int waveCount = 10;

    private int _waveIndex;

    async void Start()
    {
        await UniTask.DelayFrame(5);

        var mgr = EF.Procedure;

        // 注册
        mgr.Register<CommercialStressProcedure>();

        // 缩短超时便于测试
        var field = typeof(ProcedureSystem)
            .GetField(
                "defaultTimeoutSeconds",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        field?.SetValue(mgr, 0.2f);

        Debug.Log(
            "<color=yellow>========== PROCEDURE STRESS TEST START =========</color>");

        if (infiniteStress)
        {
            InfiniteStress().Forget();
        }
        else
        {
            await LaunchWave(concurrentRoots);
        }
    }

    private async UniTaskVoid InfiniteStress()
    {
        float duration = 60f;

        float timer = 0;

        while (timer < duration)
        {
            _waveIndex++;

            await LaunchWave(waveCount);

            await UniTask.Delay(1000);

            timer += 1f;
        }

        Debug.Log("等待剩余流程结束...");

        await UniTask.WaitUntil(
            () => !EF.Procedure.HasRunningProcedure);

        Debug.Log(
            "<color=green>========== 全部流程结束 =========</color>");
    }
    private async UniTask LaunchWave(int count)
    {
        List<UniTask> tasks = new();

        for (int i = 0; i < count; i++)
        {
            tasks.Add(StartOne(i));

            await UniTask.Delay(launchInterval);
        }

        await UniTask.WhenAll(tasks);
    }

    private async UniTask StartOne(int index)
    {
        try
        {
            await EF.Procedure.Switch
                <CommercialStressProcedure>(
                    new Dictionary<string, object>
                    {
                        { "RootIndex", index }
                    });
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"StartOne Exception\n{e}");
        }
    }
}