/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-04-03 16:44:22
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-03 16:44:22
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.UI.Tips
{
	public partial class TipsView
	{
		#region Components.可使用组件 -- Auto
		private Text Txt_Cancel;
		private Text Txt_Display;
		private Text Txt_Confirm;
		private List<Button> m_AllButtons;
		private List<ButtonPro> m_AllButtonPros;
		#endregion Components -- Auto

		public UIViewType ViewType => UIViewType.Tips;
		
		private GameObject _tipsViewGameObject;

		private TipsViewExtraData? _tipsExtraData;

		private void Bind()
		{
			#region Find components and register button event. 查找组件并且注册按钮事件 -- Auto
			Txt_Cancel = EF.Tool.Find<Text>(_tipsViewGameObject.transform, "Txt_Cancel");
			Txt_Display = EF.Tool.Find<Text>(_tipsViewGameObject.transform, "Txt_Display");
			Txt_Confirm = EF.Tool.Find<Text>(_tipsViewGameObject.transform, "Txt_Confirm");
			EF.Tool.Find<Button>(_tipsViewGameObject.transform, "Btn_Close").RegisterInListAndBindEvent(OnClickClose, ref m_AllButtons);
			EF.Tool.Find<Button>(_tipsViewGameObject.transform, "Btn_Cancel").RegisterInListAndBindEvent(OnClickCancel, ref m_AllButtons);
			EF.Tool.Find<Button>(_tipsViewGameObject.transform, "Btn_Confirm").RegisterInListAndBindEvent(OnClickConfirm, ref m_AllButtons);
			#endregion  Find components end. -- Auto
		}

		private void Dispose()
		{
			#region Quit Buttons.按钮 -- Auto
			m_AllButtons.ReleaseAndRemoveEvent();
			m_AllButtons = null;
			m_AllButtonPros.ReleaseAndRemoveEvent();
			m_AllButtonPros = null;
			#endregion Buttons.按钮 -- Auto
			
			Object.Destroy(_tipsViewGameObject);
			_tipsViewGameObject = null;
		}
	}
}
