/*
 * ================================================
 * Describe:      Please modify the description..
 * Author:        Alvin8412
 * CreationTime:  2026-04-29 11:00:31
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-29 11:00:31
 * ScriptVersion: 0.1 
 * ================================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework.Systems.Ui;

namespace EFExample
{
    /// <summary>
    /// Please modify the description.
    /// </summary>
    public partial class ChatMessageView
    {
        void IUiView.Awake()
        {
        }

        void IUiView.Enable(params object[] args)
        {
            EF.Ui.CloseView<BottomSubfieldView>().Forget();
        }

        void IUiView.Quit()
        {
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnClickBtn_Back()
        {
            EF.Ui.OpenPageView<BottomSubfieldView>().Forget();
            EF.Ui.CloseView(this).Forget();
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}
