/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-20 09-51-11
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-02-04 17:20:11
 * ScriptVersion: 0.1 
 * ================================================
*/
using EasyFramework;
using EasyFramework.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// Please modify the description.
    /// </summary>
    public class UiC : UIPageBase
    {
        int m_TimeEventId;
        Action<bool> timeEvent;
        /* ---------- Do not change anything with an ' -- Auto' ending. 不要对以 -- Auto 结尾的内容做更改 ---------- */
		#region Components.可使用组件 -- Auto
		private Text Txt_TotalTime;
		private Slider Sld_Timer;
		private List<Button> m_AllButtons;
		private List<ButtonPro> m_AllButtonPros;
		#endregion Components -- Auto

		public override void Awake(GameObject obj, params object[] args)
		{
			#region Find components and register button event. 查找组件并且注册按钮事件 -- Auto
			Txt_TotalTime = EF.Tool.Find<Text>(obj.transform, "Txt_TotalTime");
			Sld_Timer = EF.Tool.Find<Slider>(obj.transform, "Sld_Timer");
			EF.Tool.Find<Button>(obj.transform, "Btn_QuitC").RegisterInListAndBindEvent(OnClickBtn_QuitC, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_AddTimeEvent").RegisterInListAndBindEvent(OnClickBtn_AddTimeEvent, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_RemoveTimeEvent").RegisterInListAndBindEvent(OnClickBtn_RemoveTimeEvent, ref m_AllButtons);
            #endregion  Find components end. -- Auto

            D.Warning("C init" + obj.transform.childCount);

            Sld_Timer.onValueChanged.AddListener(EF.Timer.SetTimeScale);
            timeEvent = (bol) => { D.Warning("TimerEvent is done.   10f"); };

            foreach (var item in args)
            {
                D.Log($"C enter  {item}");
            }



            D.Emphasize("C :   " + SerialId);
        }

        public override void OnFocus(bool isPause, params object[] args)
        {
            D.Warning("C pause");
        }

        public override void Update(float elapse, float realElapse)
        {
            Txt_TotalTime.text = $"当前游戏已运行 {EF.Timer.TotalTime} s";
        }

        public override void Quit()
		{
			#region Quit Buttons.按钮 -- Auto
			m_AllButtons.ReleaseAndRemoveEvent();
			m_AllButtons = null;
			m_AllButtonPros.ReleaseAndRemoveEvent();
			m_AllButtonPros = null;
			m_AllButtons.ReleaseAndRemoveEvent();
			m_AllButtons = null;
			m_AllButtonPros.ReleaseAndRemoveEvent();
			m_AllButtonPros = null;
            #endregion Buttons.按钮 -- Auto            
			
			D.Warning("C quit" + SerialId);
        }

        #region Button event in game ui page.
        void OnClickBtn_QuitC() 
		{
            EF.Ui.Pop("C page is exit, also pass args to current page.");
        }
        void OnClickBtn_AddTimeEvent()
        {
            D.Warning("Add timerEvent with 10.0f");
            m_TimeEventId = EF.Timer.AddOnce(10f, timeEvent);
        }
        void OnClickBtn_RemoveTimeEvent()
        {
            EF.Timer.RemoveAt(m_TimeEventId);
        }
		#endregion button event.  Do not change here.不要更改这行 -- Auto
	}
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
