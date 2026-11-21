/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-24 21:57:27
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 15:25:21
 * ScriptVersion: 0.1 
 * ================================================
 */

using EasyFramework;
using EasyFramework.Ui;
using Cysharp.Threading.Tasks;

namespace EFExample
{
    /// <summary>
    /// Please modify the description.
    /// </summary>
    public partial class TestBottomViewOne
    {
        void IUiView.Awake()
        {
            D.Log("TestBottomViewOne Awake");

            OpenOtherAfterDelayAsync().Forget();
        }

        // 延时 10 秒后打开另一个视图
        private static async UniTaskVoid OpenOtherAfterDelayAsync()
        {
            await UniTask.Delay(10000);
            await UiManager.Instance.OpenView<TestBottomViewTwo>();
        }

        void IUiView.Quit()
        {
            D.Log("TestBottomViewOne Quit");
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}
