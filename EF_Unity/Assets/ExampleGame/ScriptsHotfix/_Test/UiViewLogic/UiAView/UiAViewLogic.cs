/*
 * ================================================
 * Describe:      案例首页.
 * Author:        Alvin8412
 * CreationTime:  2026-04-06 23:04:44
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-20 09:54:37
 * ScriptVersion: 0.1
 * ================================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Ui;
using ExampleGame;

namespace EFExample
{
    /// <summary>
    /// 案例首页
    /// </summary>
    public partial class UiAView: IUiEnable<int>
    {
        void IUiView.Awake()
        {
            UiManager.Instance.OpenView<TestTopView>().Forget();
            UiManager.Instance.OpenView<TestBottomViewOne>().Forget();
        }
        public void Enable(int args1)
        {
            D.Log("A view Enable:  " + args1);
        }

        void IUiView.Quit()
        {
            D.Warning("A Quit");
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_ToB()
        {
            UiManager.Instance.OpenView<UiBView, string>("向B传递参数").Forget();
        }

        private void OnClickBtn_Quit()
        {
            EF.QuitGame();
        }

        private void OnClickBtn_Test()
        {
            D.Log("OnClick:  Btn_Test");
            EF.UI.AddChild<UiAView, UiAViewChild>().Forget();
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}
