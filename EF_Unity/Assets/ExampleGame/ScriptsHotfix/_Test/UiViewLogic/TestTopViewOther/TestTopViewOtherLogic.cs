/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-24 21:58:47
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 14:14:29
 * ScriptVersion: 0.1 
 * ================================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Ui;

namespace EFExample
{
    /// <summary>
    /// Please modify the description.
    /// </summary>
    public partial class TestTopViewOther
    {
        void IUiView.Awake()
        {
            D.Log("TestTopViewOther Awake");
        }

        void IUiView.Quit()
        {
            D.Log("TestTopViewOther Quit");
        }

        public async UniTask CloseAll()
        {
            await UiSystem.Instance.CloseAllView();
            UiSystem.Instance.OpenView<UiAView>().Forget();
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_OpenOne()
        {
            UiSystem.Instance.OpenView<TestTopView>().Forget();
        }

        private void OnClickBtn_CloseAll()
        {
            CloseAll().Forget();
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}
