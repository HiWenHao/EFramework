/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-24 21:45:15
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 15:25:07
 * ScriptVersion: 0.1 
 * ================================================
 */

using EasyFramework;
using EasyFramework.Managers.Ui;
using Cysharp.Threading.Tasks;

namespace EFExample
{
    /// <summary>
    /// Please modify the description.
    /// </summary>
    public partial class TestTopView
    {
        void IUiView.Awake()
        {
            D.Log("TestTopView Awake");

            OpenOtherAfterDelayAsync().Forget();
        }

        // 延时 10 秒后打开另一个视图
        private static async UniTaskVoid OpenOtherAfterDelayAsync()
        {
            await UniTask.Delay(10000);
            await UiManager.Instance.OpenView<TestTopViewOther>();
        }

        void IUiView.Quit()
        {
            D.Warning("TestTopView Quit");
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}
