/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-09-13 20:35:40
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-09-13 20:35:40
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XHTools;

namespace GMTest
{
    public class UiA : UIPageBase
    {
        List<Button> m_lst_Buttons;
        public override void Awake(GameObject obj, params object[] args)
        {
            D.Log("A init");

            EF.Tool.RecursiveSearch<Button>(obj.transform, "btn_ToB").RegisterInListAndBindEvent(OnClickToB, ref m_lst_Buttons);
            EF.Tool.RecursiveSearch<Button>(obj.transform, "btn_Quit").RegisterInListAndBindEvent(OnClickQuit, ref m_lst_Buttons);
            EF.Tool.RecursiveSearch<Button>(obj.transform, "btn_StartGame").RegisterInListAndBindEvent(OnClickStartGame, ref m_lst_Buttons);

        }

        public override void OnPause(bool enable, params object[] args)
        {
            D.Log("A pause   " + enable);
            foreach (var item in args)
            {
                D.Warning($"A to B  {item}");
            }
        }

        public override void Quit()
        {
            m_lst_Buttons.ReleaseAndRemoveEvent();
            m_lst_Buttons = null;
            D.Log("A quit");
        }

        #region Private function
        void OnClickToB()
        {
            EF.Ui.Push(new UiB(), "向B传递参数");
        }
        void OnClickQuit()
        {
            EF.QuitGame();
        }
        void OnClickStartGame()
        {
            EF.Sources.SetBgmVolum(0.3f);
            EF.Sources.PlayBGMByName("BGM");
            EF.Scenes.LoadSceneWithName("GameMain", delegate
            {
                EF.Ui.Push(new ExampleGame.UI.UiGameMain());
            });
        }
        #endregion
    }
}
