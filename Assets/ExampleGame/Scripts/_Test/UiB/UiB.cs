/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-20 09-41-25
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-20 09-41-25
 * ScriptVersion: 0.1 
 * ================================================
*/
using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EasyFramework;

namespace GMTest
{
	/// <summary>
	/// Please modify the description.
	/// </summary>
	public class UiB : UIPageBase
    {
        bool m_bol_MuteSource;
        /* ---------- Do not change anything with an ' -- Auto' ending. 不要对以 -- Auto 结尾的内容做更改 ---------- */
        #region Components.可使用组件 -- Auto
        private Slider Sld_Volum;
		private List<Button> m_AllButtons;
		private List<ButtonPro> m_AllButtonPros;
		#endregion Components -- Auto

		public override void Awake(GameObject obj, params object[] args)
		{
			#region Find components and register button event. 查找组件并且注册按钮事件 -- Auto
			Sld_Volum = EF.Tool.Find<Slider>(obj.transform, "Sld_Volum") ;
			EF.Tool.Find<Button>(obj.transform, "Btn_ToC").RegisterInListAndBindEvent(OnClickBtn_ToC, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_ToCPop").RegisterInListAndBindEvent(OnClickBtn_ToCPop, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_BackA").RegisterInListAndBindEvent(OnClickBtn_BackA, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_StopAllEffect").RegisterInListAndBindEvent(OnClickBtn_StopAllEffect, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_StopEffect").RegisterInListAndBindEvent(OnClickBtn_StopEffect, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_StopBGM").RegisterInListAndBindEvent(OnClickBtn_StopBGM, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_MuteAll").RegisterInListAndBindEvent(OnClickBtn_MuteAll, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_UnPauseAll").RegisterInListAndBindEvent(OnClickBtn_UnPauseAll, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_PauseAll").RegisterInListAndBindEvent(OnClickBtn_PauseAll, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_2D").RegisterInListAndBindEvent(OnClickBtn_2D, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_3D").RegisterInListAndBindEvent(OnClickBtn_3D, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_bgm").RegisterInListAndBindEvent(OnClickBtn_bgm, ref m_AllButtons);
			#endregion  Find components end. -- Auto

			Sld_Volum.onValueChanged.AddListener(OnVolumChanged);


            D.Correct("B :   " + SerialId);
        }

        public override void OnFocus(bool isPause, params object[] args)
        {
            D.Correct("B pause:      " + isPause);
            foreach (var item in args)
            {
                D.Correct($"B enter  {item}");
            }
        }

        public override void Quit()
        {
            D.Correct("B quit" + SerialId);
            #region Quit Buttons.按钮 -- Auto
            m_AllButtons.ReleaseAndRemoveEvent();
			m_AllButtons = null;
			m_AllButtonPros.ReleaseAndRemoveEvent();
			m_AllButtonPros = null;
			#endregion Buttons.按钮 -- Auto
		}

		#region Button event in game ui page.
		void OnVolumChanged(float volum)
        {
            EF.Audio.SetBgmVolum(volum);
            EF.Audio.SetEffectVolum(volum);
        }
        void OnClickBtn_ToC()
        {
            EF.Ui.Push(new UiC(), true);
        }
		void OnClickBtn_ToCPop()
        {
            EF.Ui.PopAndPushTo(new UiC(), true, false);
        }
		void OnClickBtn_BackA()
        {
            EF.Ui.Pop();
        }
		void OnClickBtn_StopAllEffect()
        {
            EF.Audio.StopAllEffectSources();
        }
		void OnClickBtn_StopEffect()
        {
            EF.Audio.StopEffectSourceByName("Haoheng");
        }
		void OnClickBtn_StopBGM()
        {
            EF.Audio.StopBGM();
        }
		void OnClickBtn_MuteAll()
        {
            m_bol_MuteSource = !m_bol_MuteSource;
            EF.Audio.MuteAll(m_bol_MuteSource);
        }
		void OnClickBtn_UnPauseAll()
        {
            EF.Audio.UnPauseAll();
        }
		void OnClickBtn_PauseAll()
        {
            EF.Audio.PauseAll();
        }
		void OnClickBtn_2D()
        {
            EF.Audio.Play2DEffectSouceByName("HaoHeng", delegate
            {
                D.Log("2D HaoHeng is play done.");
            });
        }
		void OnClickBtn_3D()
        {
            EF.Audio.Play3DEffectSouceByName("HaoHeng", Vector3.one, delegate
            {
                D.Log("3D HaoHeng is play done.");
            });
        }
		void OnClickBtn_bgm()
        {
            EF.Audio.PlayBGMByName("BGM", true);
        }
		#endregion button event.  Do not change here.不要更改这行 -- Auto
	}
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
