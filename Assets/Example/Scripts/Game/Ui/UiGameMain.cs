/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-09 15:50:17
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-09 15:50:17
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ExampleGame.UI
{
	/// <summary>
	/// The game main page.
	/// 游戏主界面
	/// </summary>
	public class UiGameMain : UIPageBase
	{
		Button btn_Start;
        public override void Awake(GameObject obj, params object[] args)
		{
			btn_Start = EF.Tool.RecursiveSearch<Button>(obj.transform, "btn_Start");
			btn_Start.onClick.AddListener(OnClickStart);
        }

		public override void Quit()
		{
			btn_Start.onClick.RemoveAllListeners();
        }

		#region Private Function
		void OnClickStart()
		{
			EF.Ui.PopAndPushTo(new UiGamePage());
			EF.Sources.Play2DEffectSouceByName("OnClick2");
		}
        #endregion
    }
}
