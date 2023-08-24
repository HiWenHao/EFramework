/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-08-24 17:06:53
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-08-24 17:49:21
 * ScriptVersion: 0.1 
 * ================================================
*/
using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EasyFramework;

namespace AimGame
{
	/// <summary>
	/// Please modify the description.
	/// </summary>
	public class UiAimGameShop : UIPageBase
	{
		/* ---------- Do not change anything with an ' -- Auto' ending. 不要对以 -- Auto 结尾的内容做更改 ---------- */
		#region Components.可使用组件 -- Auto
		private RectTransform Tran_GunInfo;
		private Text Txt_GunName;
		private Text Txt_GunType;
		private Text Txt_GunDescription;
		private Text Txt_GunFireType;
		private Text Txt_GunFiringRate;
		private Text Txt_Magazine;
		private Text Txt_InjuryHead;
		private Text Txt_InjuryBody;
		private Text Txt_InjuryLimbs;
		private List<Button> m_AllButtons;
		private List<ButtonPro> m_AllButtonPros;
		#endregion Components -- Auto

		int gunIndex;

		Gun[] guns;

		public override void Awake(GameObject obj, params object[] args)
		{
			#region Find components and register button event. 查找组件并且注册按钮事件 -- Auto
			Tran_GunInfo = EF.Tool.Find<RectTransform>(obj.transform, "Tran_GunInfo");
			Txt_GunName = EF.Tool.Find<Text>(obj.transform, "Txt_GunName");
			Txt_GunType = EF.Tool.Find<Text>(obj.transform, "Txt_GunType");
			Txt_GunDescription = EF.Tool.Find<Text>(obj.transform, "Txt_GunDescription");
			Txt_GunFireType = EF.Tool.Find<Text>(obj.transform, "Txt_GunFireType");
			Txt_GunFiringRate = EF.Tool.Find<Text>(obj.transform, "Txt_GunFiringRate");
			Txt_Magazine = EF.Tool.Find<Text>(obj.transform, "Txt_Magazine");
			Txt_InjuryHead = EF.Tool.Find<Text>(obj.transform, "Txt_InjuryHead");
			Txt_InjuryBody = EF.Tool.Find<Text>(obj.transform, "Txt_InjuryBody");
			Txt_InjuryLimbs = EF.Tool.Find<Text>(obj.transform, "Txt_InjuryLimbs");
			EF.Tool.Find<Button>(obj.transform, "Btn_Back").RegisterInListAndBindEvent(OnClickBtn_Back, ref m_AllButtons);
			#endregion  Find components end. -- Auto

			EF.Event.AddEnvet<int, bool>("MouseEnter", OnMouseEnterGunInfo);
			gunIndex = -1;
			guns = new Gun[]
			{
				new Standard(),
				new RifleAK47(),
			};
        }

		public override void Quit()
		{
			EF.Event.RemoveEvent<int, bool>("MouseEnter", OnMouseEnterGunInfo);

        }


		void OnMouseEnterGunInfo(int index, bool inAndOut)
		{
			D.Correct($"index = {index}       inAndOut = {inAndOut}");

			if (gunIndex == index)
				return;

			gunIndex = index;

			Txt_GunName.text = guns[gunIndex].Name;
			Txt_GunType.text = guns[gunIndex].GunsType.ToString();
            Txt_GunDescription.text = guns[gunIndex].Description;
            Txt_GunFireType.text = guns[gunIndex].FireType.ToString();
            Txt_GunFiringRate.text = $"{(int)(1000 / guns[gunIndex].FiringRate)}/s";
            Txt_Magazine.text = $"{guns[gunIndex].Magazine / guns[gunIndex].TotalAmmo}";
            Txt_InjuryHead.text = $"{guns[gunIndex].InjuryHead}";
            Txt_InjuryBody.text = $"{guns[gunIndex].InjuryBody}";
            Txt_InjuryLimbs.text = $"{guns[gunIndex].InjuryLimbs}";
        }

		#region Button event in game ui page.
		void OnClickBtn_Back() 
		{
			EF.Ui.Pop();
		}
		#endregion button event.  Do not change here.不要更改这行 -- Auto
	}
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
