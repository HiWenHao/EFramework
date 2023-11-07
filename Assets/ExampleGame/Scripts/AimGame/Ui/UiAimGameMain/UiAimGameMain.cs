/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-08-18 15:03:40
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-11-07 17:39:06
 * ScriptVersion: 0.1 
 * ================================================
*/
using dnlib.DotNet;
using EasyFramework;
using EasyFramework.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AimGame
{
    /// <summary>
    /// Please modify the description.
    /// </summary>
    public class UiAimGameMain : UIPageBase
    {
        int m_hitNum;
        bool m_start;
        float m_timer, m_timerOffset;
        /* ---------- Do not change anything with an ' -- Auto' ending. 不要对以 -- Auto 结尾的内容做更改 ---------- */
		#region Components.可使用组件 -- Auto
		private RectTransform Tran_AimMark;
		private Text Txt_HitNumber;
		private Text Txt_ElapsedTime;
		private RectTransform Tran_Setting;
		private Toggle Tog_SideswayOpen;
		private Toggle Tog_SideswayClose;
		private TMP_InputField Ipt_MouseSpeed;
		private Slider Sld_MouseSpeed;
		private List<Button> m_AllButtons;
		private List<ButtonPro> m_AllButtonPros;
        #endregion Components -- Auto

        GameObject Btn_Start;

        public override void Awake(GameObject obj, params object[] args)
        {
			#region Find components and register button event. 查找组件并且注册按钮事件 -- Auto
			Tran_AimMark = EF.Tool.Find<RectTransform>(obj.transform, "Tran_AimMark");
			Txt_HitNumber = EF.Tool.Find<Text>(obj.transform, "Txt_HitNumber");
			Txt_ElapsedTime = EF.Tool.Find<Text>(obj.transform, "Txt_ElapsedTime");
			Tran_Setting = EF.Tool.Find<RectTransform>(obj.transform, "Tran_Setting");
			Tog_SideswayOpen = EF.Tool.Find<Toggle>(obj.transform, "Tog_SideswayOpen");
			Tog_SideswayClose = EF.Tool.Find<Toggle>(obj.transform, "Tog_SideswayClose");
			Ipt_MouseSpeed = EF.Tool.Find<TMP_InputField>(obj.transform, "Ipt_MouseSpeed");
			Sld_MouseSpeed = EF.Tool.Find<Slider>(obj.transform, "Sld_MouseSpeed");
			EF.Tool.Find<Button>(obj.transform, "Btn_Back").RegisterInListAndBindEvent(OnClickBtn_Back, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_Set").RegisterInListAndBindEvent(OnClickBtn_Set, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_Start").RegisterInListAndBindEvent(OnClickBtn_Start, ref m_AllButtons);
			EF.Tool.Find<Button>(obj.transform, "Btn_CloseSet").RegisterInListAndBindEvent(OnClickBtn_CloseSet, ref m_AllButtons);
            #endregion  Find components end. -- Auto

            #region Custom Find
            Btn_Start = EF.Tool.Find(obj.transform, "Btn_Start").gameObject;
            #endregion

            Tog_SideswayOpen.onValueChanged.AddListener(OnSieswayChanged);

            Tog_SideswayOpen.isOn = AimGameConfig.Instance.SideswayOpen;
            Tog_SideswayClose.isOn = !AimGameConfig.Instance.SideswayOpen;

            Sld_MouseSpeed.minValue = 0.0f;
            Sld_MouseSpeed.maxValue = 2.0f;

            OnMouseSpeedChanging(AimGameConfig.Instance.MouseSpeed);
            Sld_MouseSpeed.onValueChanged.AddListener(OnMouseSpeedChanged);
            Ipt_MouseSpeed.onEndEdit.AddListener(OnMouseSpeedChangedDone);

            m_timer = 0;
            m_hitNum = 0;
            AimGameConfig.Instance.HitNumber += HitNumber;
        }

        public override void OnFocus(bool focus, params object[] args)
        {
            if (focus)
            {
                SetShowOrHideWithMouse(false);
            }
        }

        public override void Update(float elapse, float realElapse)
        {
            if (m_start && (m_timerOffset += elapse) >= 1.0f)
            {
                m_timerOffset = 0.0f;
                m_timer++;
                Txt_ElapsedTime.text = m_timer.ToString();
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                OnClickBtn_Set();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {

                if (!AimGameController.Instance.InPause)
                    SetShowOrHideWithMouse(true);
                else
                    SetShowOrHideWithMouse(false);
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                SetShowOrHideWithMouse(true);
                EF.Ui.Push(new UiAimGameShop(), false);
            }
        }

        public override void Quit()
        {
            #region Quit Buttons.按钮 -- Auto
            m_AllButtons.ReleaseAndRemoveEvent();
            m_AllButtons = null;
            m_AllButtonPros.ReleaseAndRemoveEvent();
            m_AllButtonPros = null;
            #endregion Buttons.按钮 -- Auto
            Tog_SideswayOpen.onValueChanged.RemoveAllListeners();
            AimGameConfig.Instance.HitNumber -= HitNumber;
        }

        private void OnSieswayChanged(bool arg0)
        {
            AimGameConfig.Instance.SetSidesway(arg0);
        }

        private void OnMouseSpeedChanged(float value)
        {
            OnMouseSpeedChanging(value);
        }
        private void OnMouseSpeedChangedDone(string msg)
        {
            OnMouseSpeedChanging(float.Parse(msg));
        }

        private void SetShowOrHideWithMouse(bool active)
        {
            Cursor.visible = active;
            Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
            EF.Ui.OpenClickEffect = active;
            AimGameController.Instance.InPause = active;
        }


        void OnMouseSpeedChanging(float value)
        {
            value = (int)(value * 100) / 100f;
            Ipt_MouseSpeed.text = $"{value}";
            Sld_MouseSpeed.value = value;
            AimGameConfig.Instance.SetMouseSpeed(value);
        }

        #region Event
        void HitNumber(int num)
        {
            m_hitNum += num;
            Txt_HitNumber.text = m_hitNum.ToString();
        }
        #endregion

        #region Button event in game ui page.
        void OnClickBtn_Start()
        {
            m_start = true;
            Btn_Start.SetActive(false);
            AimGameController.Instance.ReStart();
            SetShowOrHideWithMouse(false);

            Tran_AimMark.gameObject.SetActive(true);
        }
        void OnClickBtn_Back()
        {
            EF.Ui.PopToOnlyOne(true);

            SetShowOrHideWithMouse(true);
            AimGameController.Instance.QuitGame();
        }
        void OnClickBtn_Set()
        {
            if (Tran_Setting.gameObject.activeSelf)
            {
                Tran_Setting.gameObject.SetActive(false);
                SetShowOrHideWithMouse(false);
            }
            else
            {
                Tran_Setting.gameObject.SetActive(true);
                SetShowOrHideWithMouse(true);
            }
        }
        void OnClickBtn_CloseSet()
        {
            Tran_Setting.gameObject.SetActive(false);
            SetShowOrHideWithMouse(false);
        }
        #endregion button event.  Do not change here.不要更改这行 -- Auto
    }
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
