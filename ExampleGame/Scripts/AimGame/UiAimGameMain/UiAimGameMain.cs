/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-08-18 14:51:41
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-08-18 14:57:36
 * ScriptVersion: 0.1 
 * ================================================
*/
using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EasyFramework;

namespace EFExample
{
	/// <summary>
	/// Please modify the description.
	/// </summary>
	public class UiAimGameMain : UIPageBase
	{
		/* ---------- Do not change anything with an ' -- Auto' ending. 不要对以 -- Auto 结尾的内容做更改 ---------- */
		#region Components.可使用组件 -- Auto
		private List<Button> m_AllButtons;
		private List<ButtonPro> m_AllButtonPros;
		#endregion Components -- Auto

		public override void Awake(GameObject obj, params object[] args)
		{
			#region Find components and register button event. 查找组件并且注册按钮事件 -- Auto
			EF.Tool.Find<Button>(obj.transform, "Btn_Start").RegisterInListAndBindEvent(OnClickBtn_Start, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_Set").RegisterInListAndBindEvent(OnClickBtn_Set, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_Back").RegisterInListAndBindEvent(OnClickBtn_Back, ref m_AllButtons);
			#endregion  Find components end. -- Auto
		}

		public override void Quit()
		{
			#region Quit Buttons.按钮 -- Auto
			m_AllButtons.ReleaseAndRemoveEvent();
			m_AllButtons = null;
			m_AllButtonPros.ReleaseAndRemoveEvent();
			m_AllButtonPros = null;
			#endregion Buttons.按钮 -- Auto
		}

		#region Button event in game ui page.
		void OnClickBtn_Start() 
		{
			D.Log("OnClick:  Btn_Start");
		}
		void OnClickBtn_Set() 
		{
			D.Log("OnClick:  Btn_Set");
		}
		void OnClickBtn_Back() 
		{
			D.Log("OnClick:  Btn_Back");
		}
		#endregion button event.  Do not change here.不要更改这行 -- Auto
	}
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
