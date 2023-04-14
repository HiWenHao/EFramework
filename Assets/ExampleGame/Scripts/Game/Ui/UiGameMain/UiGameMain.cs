/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-20 14:50:08
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-20 14:50:08
 * ScriptVersion: 0.1 
 * ================================================
*/
using EasyFramework.UI;
using ExampleGame.Controller;
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
        public class UiGameMain : UIPageBase
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
                EF.Sources.Play2DEffectSouceByName("OnClick2");

                Transform _Player = Object.Instantiate(EF.Load.Load<Transform>(AppConst.Player + "VBOT"));
                _Player.SetPositionAndRotation(Vector3.up * 7.0f, Quaternion.identity);

                CameraControl.Instance.SetTarget(_Player);

                PlayerObserver.Instance.SetPlayerConfig(new Configs.VBotPlayer(100, 5, 2), _Player.GetComponent<PlayerController>());

                EF.Ui.PopAndPushTo(new UiGamePage());
            }
            #endregion button event.  Do not change here.不要更改这行 -- Auto
        }
    }
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
