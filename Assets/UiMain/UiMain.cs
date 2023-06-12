/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-06-01 15:33:34
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-06-01 15:33:34
 * ScriptVersion: 0.1 
 * ================================================
*/
using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EasyFramework;
using TMPro;

namespace EFExample
{
	/// <summary>
	/// Please modify the description.
	/// </summary>
	public class UiMain : UIPageBase
	{
		/* ---------- Do not change anything with an ' -- Auto' ending. 不要对以 -- Auto 结尾的内容做更改 ---------- */
		#region Components.可使用组件 -- Auto
		private RectTransform Tran_LabelAll;
		private RectTransform Tran_LabelBG;
		private TextMeshProUGUI TxtM_Left;
		private TextMeshProUGUI TxtM_Right;
		private RectTransform Tran_CommonLabel;
		private TextMeshProUGUI TxtM_CommonLabel;
		private RectTransform Tran_BottomBG;
		private TMP_InputField Ipt_EnterLabel;
		private List<Button> m_AllButtons;
		private List<ButtonPro> m_AllButtonPros;
		#endregion Components -- Auto

		public override void Awake(GameObject obj, params object[] args)
		{
			#region Find components and register button event. 查找组件并且注册按钮事件 -- Auto
			Tran_LabelAll = EF.Tool.Find<RectTransform>(obj.transform, "Tran_LabelAll");
			Tran_LabelBG = EF.Tool.Find<RectTransform>(obj.transform, "Tran_LabelBG");
			TxtM_Left = EF.Tool.Find<TextMeshProUGUI>(obj.transform, "TxtM_Left");
			TxtM_Right = EF.Tool.Find<TextMeshProUGUI>(obj.transform, "TxtM_Right");
			Tran_CommonLabel = EF.Tool.Find<RectTransform>(obj.transform, "Tran_CommonLabel");
			TxtM_CommonLabel = EF.Tool.Find<TextMeshProUGUI>(obj.transform, "TxtM_CommonLabel");
			Tran_BottomBG = EF.Tool.Find<RectTransform>(obj.transform, "Tran_BottomBG");
			Ipt_EnterLabel = EF.Tool.Find<TMP_InputField>(obj.transform, "Ipt_EnterLabel");
			EF.Tool.Find<Button>(obj.transform, "Btn_Speaking").RegisterInListAndBindEvent(OnClickBtn_Speaking, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_SendLabel").RegisterInListAndBindEvent(OnClickBtn_SendLabel, ref m_AllButtons);
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
		void OnClickBtn_Speaking() 
		{
			D.Log("OnClick:  Btn_Speaking");
		}
		void OnClickBtn_SendLabel() 
		{
			D.Log("OnClick:  Btn_SendLabel");
		}
		#endregion button event.  Do not change here.不要更改这行 -- Auto
	}
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
