/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin5100
 * CreationTime:    2026-11-26 18:19:44
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-26 18:19:44
 * ScriptVersion:   0.1
 * ================================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.GameMaster;
using EasyFramework.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExampleGame
{
    /// <summary>
    /// GM示例
    /// </summary>
    public class GmExample : MonoBehaviour
    {
        // 面板分区名（示例）
        private const string GmSectionScene = "场景";
        private const string GmSectionDebug = "调试";

        private void Start()
        {
            RegisterGmCommands();

            EFC.GetOrCreate<UiManager>().OpenView<GmPanelView>().Forget();
        }

        // 注册 GM 总控台示例命令（使用项目内真实存在的类型）
        private static void RegisterGmCommands()
        {
            // 流程/场景跳转：带参数 scene，面板会显示输入框
            GameMasterManager.Register(
                "flow.scene",
                "切换场景",
                GmRisk.Dangerous,
                ctx =>
                {
                    if (string.IsNullOrEmpty(ctx.Args)) return UniTask.CompletedTask;

                    SceneManager.LoadScene(ctx.Args);
                    D.Log("[ Gm ] 切换场景：" + ctx.Args);
                    return UniTask.CompletedTask;
                },
                "要切换的场景名", GmSectionScene);

            // 数据/资源调试：打印当前已注册单例数量（只读）
            GameMasterManager.Register(
                "data.singletons", "统计单例数量", GmRisk.ReadOnly,
                ctx =>
                {
                    int count = EFC.GetAllSingletons().Count;
                    D.Log("[ Gm ] 已注册单例数：" + count);
                    return UniTask.CompletedTask;
                }, null, GmSectionDebug);


            // 开关类：切换 UiManager 调试日志
            GameMasterManager.Register(
                "toggle.uidebug", "UI 调试日志开关", GmRisk.Normal,
                ctx =>
                {
                    UiManager.Instance.OpenDebug = !UiManager.Instance.OpenDebug;
                    D.Log("[ Gm ] UI 调试日志：" + UiManager.Instance.OpenDebug);
                    return UniTask.CompletedTask;
                }, null, GmSectionDebug);


            GameMasterManager.Register(
                "test.panel", "测试", GmRisk.Normal,
                ctx =>
                {
                    D.Log(int.TryParse(ctx.Args, out int value)
                        ? $"[ Gm ] 传递参数: {value}"
                        : $"[ Gm ] 参数错误，输入内容为： {ctx.Args}");

                    return UniTask.CompletedTask;
                }, 
                "int值");
        }
    }
}