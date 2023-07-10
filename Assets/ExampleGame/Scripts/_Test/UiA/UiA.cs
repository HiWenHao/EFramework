/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-20 09-35-44
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-06-19 16:07:06
 * ScriptVersion: 0.1 
 * ================================================
*/
using EasyFramework.UI;
using GMTest;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EasyFramework;

namespace GMTest
{
	/// <summary>
	/// Please modify the description.
	/// </summary>
	public class UiA : UIPageBase
	{
		/* ---------- Do not change anything with an ' -- Auto' ending. 不要对以 -- Auto 结尾的内容做更改 ---------- */
		#region Components.可使用组件 -- Auto
		private List<Button> m_AllButtons;
		private List<ButtonPro> m_AllButtonPros;
		#endregion Components -- Auto

		public override void Awake(GameObject obj, params object[] args)
		{
			#region Find components and register button event. 查找组件并且注册按钮事件 -- Auto
			EF.Tool.Find<Button>(obj.transform, "Btn_ToB").RegisterInListAndBindEvent(OnClickBtn_ToB, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_Quit").RegisterInListAndBindEvent(OnClickBtn_Quit, ref m_AllButtons);
			#endregion  Find components end. -- Auto

			D.Correct("A :   " + SerialId);
		}

        public override void Open(params object[] args)
        {
            D.Correct("A Open:   " + SerialId);
			EF.Ui.Push(new UiB(), false);
        }

        public override void OnFocus(bool enable, params object[] args)
        {
            D.Log("A pause   " + enable);
            foreach (var item in args)
            {
                D.Warning($"A to B  {item}");
            }

			if (enable)
			{
				Camera.main.gameObject.GetComponent<Test>().enabled = true;
            }
        }

        public override void Close()
        {
            D.Warning("A close" + SerialId);
        }
        public override void Quit()
        {
            D.Log("A quit" + SerialId);
            #region Quit Buttons.按钮 -- Auto
            m_AllButtons.ReleaseAndRemoveEvent();
			m_AllButtons = null;
			m_AllButtonPros.ReleaseAndRemoveEvent();
			m_AllButtonPros = null;
			#endregion Buttons.按钮 -- Auto
		}

		#region Button event in game ui page.
		void OnClickBtn_ToB()
        {
			EF.Ui.Push(new UiB(), true, "向B传递参数");
        }
		void OnClickBtn_Quit()
        {
            EF.QuitGame();
        }
		#endregion button event.  Do not change here.不要更改这行 -- Auto
	}
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
