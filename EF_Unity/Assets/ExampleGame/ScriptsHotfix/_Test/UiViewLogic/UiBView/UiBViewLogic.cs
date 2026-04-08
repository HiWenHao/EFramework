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

using EasyFramework;
using EasyFramework.UI;
using System.Collections.Generic;
using EasyFramework.Manager.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// 案例音频管理器
    /// </summary>
    public partial class UiBView
    {
        private bool _muteSource;
        void IUiView.Awake()
        {
            Sld_Volum.onValueChanged.AddListener(OnVolumChanged);
        }

        void IUiView.Enable(params object[] args)
        {
            foreach (var item in args)
            {
                D.Emphasize($"B enter  {item}");
            }
        }

        void IUiView.DisEnable(params object[] args)
        {
            foreach (var item in args)
            {
                D.Emphasize($"B exit  {item}");
            }
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
            EF.Uii.OpenPageView<UiCView>();
        }
        private void OnClickBtn_ToCPop()
        {
            EF.Uii.OpenPageView<UiCView>();
            EF.Uii.CloseView<UiBView>("这是 B 页面向 A 页面传递的参数");
        }
        private void OnClickBtn_BackA()
        {
            EF.Uii.CloseView<UiBView>("这是 B 页面向 A 页面传递的参数");
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
            EF.Audio.Play2DEffectSouceByName("HaoHeng", delegate
            {
                D.Log("2D HaoHeng is play done.");
            });
        }
        private void OnClickBtn_3D()
        {
            EF.Audio.Play3DEffectSouceByName("HaoHeng", Vector3.one, delegate
            {
                D.Log("3D HaoHeng is play done.");
            });
        }
        private void OnClickBtn_bgm()
        {
            EF.Audio.PlayBGMByName("BGM", true);
        }
        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}
