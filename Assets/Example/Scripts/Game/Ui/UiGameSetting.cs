/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-09 17:04:29
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-09 17:04:29
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XHTools;

namespace ExampleGame.UI
{
	/// <summary>
	/// 实例游戏设置界面
	/// </summary>
	public class UiGameSetting : UIPageBase
	{
		List<Button> Buttons;
		public override void Awake(GameObject obj, params object[] args)
		{
			EF.Tool.RecursiveSearch<Button>(obj.transform, "btn_Close").RegisterInListAndBindEvent(OnClickClose, ref Buttons);
			EF.Tool.RecursiveSearch<Button>(obj.transform, "btn_QuitGame").RegisterInListAndBindEvent(OnClickQuitGame, ref Buttons);
		}

		public override void Quit()
		{

		}

        #region Private function
        void OnClickClose()
        {
            EF.Sources.Play2DEffectSouceByName("OnClick1");
            EF.Ui.Pop();
		}
        void OnClickQuitGame()
        {
            EF.Sources.Play2DEffectSouceByName("OnClick1");
            EF.Ui.PopToOnlyHome();
			EF.Scenes.LoadSceneWithNameNow("Null");
        }
        #endregion
    }
}
