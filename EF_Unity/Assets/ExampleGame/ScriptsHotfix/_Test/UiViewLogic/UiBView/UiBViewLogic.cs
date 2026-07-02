/*
 * ================================================
 * Describe:      案例音频管理器.
 * Author:        Alvin8412
 * CreationTime:  2026-04-06 23:05:26
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-06 23:05:28
 * ScriptVersion: 0.1
 * ================================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Ui;
using EFExample.UI.Tips;
using UnityEngine;

namespace EFExample
{
    /// <summary>
    /// 案例音频管理器
    /// </summary>
    public partial class UiBView : IUiEnable<string>, IUiDisable<string>
    {
        private bool _muteSource;

        void IUiView.Awake()
        {
            Sld_Volum.onValueChanged.AddListener(OnVolumChanged);
        }

        public void Enable(string args1)
        {
            D.Emphasize($"B enter  {args1}");
        }

        public void Disable(string args1)
        {
            D.Emphasize($"B exit  {args1}");
        }

        void IUiView.Quit()
        {
            Sld_Volum.onValueChanged.RemoveAllListeners();
            D.Warning("B quit");
        }

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        private void OnVolumChanged(float volum)
        {
            EF.Audio.SetBgmVolum(volum);
            EF.Audio.SetEffectVolum(volum);
        }

        private void OnClickBtn_ToC()
        {
            UiSystem.Instance.OpenView<UiCView>().Forget();
        }

        private void OnClickBtn_ToCPop()
        {
            OnClickBtn_ToCPopHelper().Forget();
        }

        private async UniTaskVoid OnClickBtn_ToCPopHelper()
        {
            await UiSystem.Instance.OpenView<UiCView>();
            await UiSystem.Instance.CloseView<UiBView, string>("这是给被关闭的 B 页面所传递的参数");
        }

        private void OnClickBtn_BackA()
        {
            UiSystem.Instance.CloseViewAndNotify<UiBView, UiAView, int>(456456465).Forget();
        }

        private void OnClickBtn_StopAllEffect()
        {
            EF.Audio.StopAllEffectSources();
        }

        private void OnClickBtn_StopEffect()
        {
            EF.Audio.StopEffectSourceByName("Haoheng");
        }

        private void OnClickBtn_StopBGM()
        {
            EF.Audio.StopBGM();
        }

        private void OnClickBtn_MuteAll()
        {
            _muteSource = !_muteSource;
            EF.Audio.MuteAll(_muteSource);
        }

        private void OnClickBtn_UnPauseAll()
        {
            EF.Audio.UnPauseAll();
        }

        private void OnClickBtn_PauseAll()
        {
            EF.Audio.PauseAll();
        }

        private void OnClickBtn_2D()
        {
            EF.Audio.Play2DEffectSouceByName("HaoHeng", delegate { D.Log("2D HaoHeng is play done."); });
        }

        private void OnClickBtn_3D()
        {
            EF.Audio.Play3DEffectSouceByName("HaoHeng", Vector3.one, delegate { D.Log("3D HaoHeng is play done."); });
        }

        private void OnClickBtn_bgm()
        {
            EF.Audio.PlayBGMByName("BGM", true);
        }

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}