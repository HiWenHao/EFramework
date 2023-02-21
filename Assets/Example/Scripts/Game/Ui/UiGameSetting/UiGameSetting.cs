/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-20 14:59:05
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-20 14:59:05
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
        public class UiGameSetting : UIPageBase
        {
            /* ---------- Do not change anything with an ' -- Auto' ending. 不要对以 -- Auto 结尾的内容做更改 ---------- */
            #region Components.可使用组件 -- Auto
            private RectTransform Tran_Setting;
            private List<Button> m_AllButtons;
            private List<ButtonPro> m_AllButtonPros;
            #endregion Components -- Auto

            public override void Awake(GameObject obj, params object[] args)
            {
                #region Find components and register button event. 查找组件并且注册按钮事件 -- Auto
                Tran_Setting = EF.Tool.RecursiveSearch<RectTransform>(obj.transform, "Tran_Setting");
                EF.Tool.RecursiveSearch<Button>(obj.transform, "Btn_Close").RegisterInListAndBindEvent(OnClickBtn_Close, ref m_AllButtons);
                EF.Tool.RecursiveSearch<Button>(obj.transform, "Btn_QuitGame").RegisterInListAndBindEvent(OnClickBtn_QuitGame, ref m_AllButtons);
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
            void OnClickBtn_Close()
            {
                EF.Sources.Play2DEffectSouceByName("OnClick1");
                EF.Ui.Pop();
            }
            void OnClickBtn_QuitGame()
            {
                EF.Sources.Play2DEffectSouceByName("OnClick1");
                EF.Ui.PopToOnlyHome();
                EF.Scenes.LoadSceneWithNameNow("Null");
            }
            #endregion button event.  Do not change here.不要更改这行 -- Auto
        }
    }
}//--------------------Auto generate footer.Do not add anything below the footer!------------   -- Auto
