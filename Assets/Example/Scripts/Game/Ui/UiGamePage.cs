/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-09 16:11:18
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-09 16:11:18
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XHTools;

namespace ExampleGame.UI
{
	/// <summary>
	/// 实例游戏的游戏界面
	/// </summary>
	public class UiGamePage : UIPageBase
	{
		List<Button> Buttons;
		public override void Awake(GameObject obj, params object[] args)
		{
			EF.Tool.RecursiveSearch<Button>(obj.transform, "btn_Setting").RegisterInListAndBindEvent(OnClickSetting, ref Buttons);
		}

		public override void Quit()
		{
            Buttons.ReleaseAndRemoveEvent();
			Buttons = null;

            EF.Sources.StopBGM();
        }

        #region Pirvate function

        #region Bottom


        void OnClickSetting()
        {
            EF.Sources.Play2DEffectSouceByName("OnClick1");
            EF.Ui.Push(new UiGameSetting());
        }
        #endregion
        #endregion
    }
}
