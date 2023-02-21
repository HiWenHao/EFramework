/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-20 15:05:27
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-21 17:18:53
 * ScriptVersion: 0.1 
 * ================================================
*/
using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ExampleGame
{
    namespace UI
    {
        /// <summary>
        /// Please modify the description.
        /// </summary>
        public class UiGamePage : UIPageBase
        {
            /* ---------- Do not change anything with an ' -- Auto' ending. 不要对以 -- Auto 结尾的内容做更改 ---------- */
			#region Components.可使用组件 -- Auto
			private RectTransform Tran_Title;
			private Slider Sld_Blood;
			private Image Img_Head;
			private Text Txt_Nickname;
			private Text Txt_Grade;
			private RectTransform Tran_State;
			private RectTransform Tran_Bottom;
			private List<Button> m_AllButtons;
			private List<ButtonPro> m_AllButtonPros;
            #endregion Components -- Auto

            public override void Awake(GameObject obj, params object[] args)
            {
				#region Find components and register button event. 查找组件并且注册按钮事件 -- Auto
				Tran_Title = EF.Tool.RecursiveSearch<RectTransform>(obj.transform, "Tran_Title") ;
				Sld_Blood = EF.Tool.RecursiveSearch<Slider>(obj.transform, "Sld_Blood") ;
				Img_Head = EF.Tool.RecursiveSearch<Image>(obj.transform, "Img_Head") ;
				Txt_Nickname = EF.Tool.RecursiveSearch<Text>(obj.transform, "Txt_Nickname") ;
				Txt_Grade = EF.Tool.RecursiveSearch<Text>(obj.transform, "Txt_Grade") ;
				Tran_State = EF.Tool.RecursiveSearch<RectTransform>(obj.transform, "Tran_State") ;
				Tran_Bottom = EF.Tool.RecursiveSearch<RectTransform>(obj.transform, "Tran_Bottom") ;
				EF.Tool.RecursiveSearch<Button>(obj.transform, "Btn_Setting").RegisterInListAndBindEvent(OnClickBtn_Setting, ref m_AllButtons);
                #endregion  Find components end. -- Auto

                Sld_Blood.GetComponent<RectTransform>().sizeDelta = new Vector2(PlayerObserver.Instance.Config.MaxBlood * 5.0f, 40.0f);
                Sld_Blood.maxValue = PlayerObserver.Instance.Config.MaxBlood;
                Sld_Blood.value = PlayerObserver.Instance.Config.MaxBlood;
                PlayerObserver.Instance.Config.onBloodChanged += ChangedBlood;
            }

            public override void Quit()
            {
                #region Quit Buttons.按钮 -- Auto
				m_AllButtons.ReleaseAndRemoveEvent();
				m_AllButtons = null;
				m_AllButtonPros.ReleaseAndRemoveEvent();
				m_AllButtonPros = null;  


                #endregion Buttons.按钮 -- Auto


                EF.Sources.StopBGM();
            }

            #region Button event in game ui page.
            void OnClickBtn_Setting()
            {
                EF.Sources.Play2DEffectSouceByName("OnClick1");
                EF.Ui.Push(new UiGameSetting());
            }
            #endregion button event.  Do not change here.不要更改这行 -- Auto

            private void ChangedBlood(int value)
            {
                Sld_Blood.value += value;
            }
        }
    }
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
