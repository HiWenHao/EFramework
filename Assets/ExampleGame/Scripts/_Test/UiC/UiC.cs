/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-20 09-51-11
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-20 10-08-19
 * ScriptVersion: 0.1 
 * ================================================
*/
using EasyFramework;
using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XHTools;

namespace GMTest
{
	/// <summary>
	/// Please modify the description.
	/// </summary>
	public class UiC : UIPageBase
    {
        EAction timeEvent, CountdownEvent;
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
			Txt_TotalTime = EF.Tool.Find<Text>(obj.transform, "Txt_TotalTime") ;
			Sld_Timer = EF.Tool.Find<Slider>(obj.transform, "Sld_Timer") ;
			EF.Tool.Find<Button>(obj.transform, "Btn_QuitC").RegisterInListAndBindEvent(OnClickBtn_QuitC, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_AddTimeEvent").RegisterInListAndBindEvent(OnClickBtn_AddTimeEvent, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_RemoveTimeEvent").RegisterInListAndBindEvent(OnClickBtn_RemoveTimeEvent, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_AddCountdownEvent1").RegisterInListAndBindEvent(OnClickBtn_AddCountdownEvent1, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_AddCountdownEvent3").RegisterInListAndBindEvent(OnClickBtn_AddCountdownEvent3, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_AddCountdownEvent5").RegisterInListAndBindEvent(OnClickBtn_AddCountdownEvent5, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_RemoveCountdownEvent").RegisterInListAndBindEvent(OnClickBtn_RemoveCountdownEvent, ref m_AllButtons);
            #endregion  Find components end. -- Auto

            D.Warning("C init" + obj.transform.childCount);

            Sld_Timer.onValueChanged.AddListener(EF.Timer.SetTimeScale);
            timeEvent = delegate { D.Warning("TimerEvent is done.   10f"); };
            CountdownEvent = delegate { D.Warning("CountdownEvent is done.   3f"); };

            foreach (var item in args)
            {
                D.Log($"C enter  {item}");
            }



            D.Correct("C :   " + SerialId);
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
            EF.Timer.AddTimeEvent(10f, timeEvent);
        }
        void OnClickBtn_RemoveTimeEvent()
        {
            EF.Timer.RemoveTimeEvent(timeEvent);
        }
        void OnClickBtn_AddCountdownEvent1()
        {
            D.Log("Add countdownEvent with 1.0f");
            EF.Timer.AddCountdownEvent(1.0f, delegate { D.Log("CountdownEvent is done.   1.0f"); });
        }
        void OnClickBtn_AddCountdownEvent3()
        {
            D.Warning("Add countdownEvent with 3.0f");
            EF.Timer.AddCountdownEvent(3.0f, CountdownEvent);
        }
        void OnClickBtn_AddCountdownEvent5()
        {
            D.Correct("Add countdownEvent with 5.0f");
            EF.Timer.AddCountdownEvent(5.0f, delegate { D.Correct("CountdownEvent is done.   5.0f"); });
        }
        void OnClickBtn_RemoveCountdownEvent()
        {
            EF.Timer.RemoveCountdownEvent(CountdownEvent);
        }
		#endregion button event.  Do not change here.不要更改这行 -- Auto
	}
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
