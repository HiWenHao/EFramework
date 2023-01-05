/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-09-13 20:35:40
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-09-13 20:35:40
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.UI;
using UnityEngine;
using UnityEngine.UI;
using XHTools;

namespace GMTest
{
    public class UiB : UIPageBase
    {
        bool m_bol_MuteSource;

        Button btn_ToB, btn_ToCPop, btn_BackA;
        Button btn_StopAllEffect, btn_StopEffect, btn_StopBGM, btn_MuteAll, btn_UnPauseAll, btn_PauseAll, btn_2D, btn_3D, btn_bgm;
        Slider sld_Volum;
        public override void Awake(GameObject obj, params object[] args)
        {
            D.Correct("B init");
            btn_ToB = obj.transform.Find("btn_ToC").GetComponent<Button>();
            btn_ToCPop = obj.transform.Find("btn_ToCPop").GetComponent<Button>();
            btn_BackA = obj.transform.Find("btn_BackA").GetComponent<Button>();

            btn_StopAllEffect = obj.transform.Find("btn_StopAllEffect").GetComponent<Button>();
            btn_StopEffect = obj.transform.Find("btn_StopEffect").GetComponent<Button>();
            btn_StopBGM = obj.transform.Find("btn_StopBGM").GetComponent<Button>();
            btn_MuteAll = obj.transform.Find("btn_MuteAll").GetComponent<Button>();
            btn_UnPauseAll = obj.transform.Find("btn_UnPauseAll").GetComponent<Button>();
            btn_PauseAll = obj.transform.Find("btn_PauseAll").GetComponent<Button>();
            btn_2D = obj.transform.Find("btn_2D").GetComponent<Button>();
            btn_3D = obj.transform.Find("btn_3D").GetComponent<Button>();
            btn_bgm = obj.transform.Find("btn_bgm").GetComponent<Button>();
            sld_Volum = obj.transform.Find("sld_Volum").GetComponent<Slider>();

            #region AddListener
            btn_ToB.onClick.AddListener(delegate
            {
                EF.Ui.Push(new UiC(), true);
            });
            btn_ToCPop.onClick.AddListener(delegate
            {
                EF.Ui.PopAndPushTo(new UiC(), true, false);
            });
            btn_BackA.onClick.AddListener(delegate
            {
                EF.Ui.Pop();
            });

            btn_StopAllEffect.onClick.AddListener(delegate
            {
                EF.Sources.StopAllEffectSources();
            });
            btn_StopEffect.onClick.AddListener(delegate
            {
                EF.Sources.StopEffectSourceByName("Haoheng");
            });
            btn_StopBGM.onClick.AddListener(delegate
            {
                EF.Sources.StopBGM();
            });
            btn_MuteAll.onClick.AddListener(delegate
            {
                m_bol_MuteSource = !m_bol_MuteSource;
                EF.Sources.MuteAll(m_bol_MuteSource);
            });
            btn_UnPauseAll.onClick.AddListener(delegate
            {
                EF.Sources.UnPauseAll();
            });
            btn_PauseAll.onClick.AddListener(delegate
            {
                EF.Sources.PauseAll();
            });
            btn_2D.onClick.AddListener(delegate
            {
                EF.Sources.Play2DEffectSouceByName("HaoHeng", delegate
                {
                    D.Log("2D HaoHeng is play done.");
                });
            });
            btn_3D.onClick.AddListener(delegate
            {
                EF.Sources.Play3DEffectSouceByName("HaoHeng", Vector3.one, delegate
                {
                    D.Log("3D HaoHeng is play done.");
                });
            });
            btn_bgm.onClick.AddListener(delegate
            {
                EF.Sources.PlayBGMByName("BGM", true);
            });
            sld_Volum.onValueChanged.AddListener((volum) => 
            {
                EF.Sources.SetBgmVolum(volum);
                EF.Sources.SetEffectVolum(volum);
            });
            #endregion
        }

        public override void OnPause(bool isPause, params object[] args)
        {
            D.Correct("B pause" + isPause);
            foreach (var item in args)
            {
                D.Correct($"B enter  {item}");
            }
        }

        public override void Quit()
        {
            btn_ToB.onClick.RemoveAllListeners();
            btn_ToCPop.onClick.RemoveAllListeners();
            btn_BackA.onClick.RemoveAllListeners();
            btn_ToB = null;
            btn_ToCPop = null;
            btn_BackA = null;
            D.Correct("B quit");
        }
    }
}
