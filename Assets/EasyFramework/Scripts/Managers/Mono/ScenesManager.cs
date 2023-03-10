/* 
 * ================================================
 * Describe:      This script is used to change scene.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-07-12 17:20:56
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-07-12 17:20:56
 * Version:       0.1
 * ===============================================
*/

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XHTools;

namespace EasyFramework.Managers
{
	/// <summary>
	/// Please modify the descriptionã€‚
	/// </summary>
	public class ScenesManager : MonoSingleton<ScenesManager>, IManager
    {
        int IManager.ManagerLevel => AppConst.ManagerLevel.SceneMgr;
        bool m_bol_IsLoading;
		float m_flt_transition = 1.0f;
		Transform LoadCanvas;
		Image m_img_BG;
		Text m_txt_PCTN;
		Slider m_slid_ProgressBar;
		AsyncOperation m_asyncOperation;
		EAction m_act_Callback;
        void ISingleton.Init()
        {
            LoadCanvas = Instantiate(EF.Load.Load<Transform>("Prefabs/UI/LoadCanvas"));
            LoadCanvas.SetParent(transform);
            m_img_BG = LoadCanvas.GetChild(0).GetComponent<Image>();
            m_slid_ProgressBar = m_img_BG.transform.GetChild(0).GetComponent<Slider>();
            m_txt_PCTN = m_slid_ProgressBar.transform.Find("Handle Slide Area/Handle/Text").GetComponent<Text>();

			LoadCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(Screen.width, Screen.height);
            LoadCanvas.gameObject.SetActive(false);
        }

        void ISingleton.Quit()
        {
            StopAllCoroutines();
            m_txt_PCTN = null;
            m_slid_ProgressBar = null;
            m_img_BG = null;
            LoadCanvas = null;
            m_asyncOperation = null;
        }

        #region Load
		IEnumerator LoadScene()
		{
			m_bol_IsLoading = true;
			yield return new WaitForSeconds(.1f);
			LoadCanvas.gameObject.SetActive(true);

			while (m_img_BG.color.a < 1.0f)
			{
				Color _color = m_img_BG.color;
				_color.a += 0.02f * m_flt_transition;
				m_img_BG.color = _color;
				yield return null;
			}
			m_slid_ProgressBar.value = 0.0f;
			m_slid_ProgressBar.gameObject.SetActive(true);
			while (m_slid_ProgressBar.value < .1f)
			{
				m_slid_ProgressBar.value += .01f;
				m_txt_PCTN.text = $"{Mathf.RoundToInt(m_slid_ProgressBar.value * 90)}%";
				yield return null;
			}
			m_asyncOperation = SceneManager.LoadSceneAsync(CurrentScene);

            while (!m_asyncOperation.isDone)
            {
                while ((m_asyncOperation.progress - m_slid_ProgressBar.value) >= .1f)
                {
					m_slid_ProgressBar.value += 0.01f;
					m_txt_PCTN.text = $"{Mathf.RoundToInt(m_slid_ProgressBar.value * 100)}%";
					yield return null;
				}
				yield return null;
			}
			yield return new WaitForSeconds(.5f);
            while (m_asyncOperation.isDone)
            {
				while (m_slid_ProgressBar.value < 1.0f)
				{
					m_slid_ProgressBar.value += .005f;
					float _vla = Mathf.RoundToInt(m_slid_ProgressBar.value * 100);
					m_txt_PCTN.text = _vla > 100 ? "100%" : $"{_vla}%";
					yield return null;
				}
				break;
			}

			yield return new WaitForSeconds(.1f);
			m_act_Callback?.Invoke();
			m_slid_ProgressBar.gameObject.SetActive(false);
			while (m_img_BG.color.a > .02f)
			{
				Color _color = m_img_BG.color;
				_color.a -= 0.02f * m_flt_transition;
				m_img_BG.color = _color;
				yield return null;
			}
			LoadCanvas.gameObject.SetActive(false);
			m_bol_IsLoading = false;
			StopCoroutine(LoadScene());
		}
		#endregion

		#region PUBLIC
		/// <summary>
		/// Get current scene name.èŽ·å?–å½“å‰?åœºæ™¯å??å­—
		/// </summary>
		public string CurrentScene { get; private set; }

        /// <summary>
        /// Load scene with name.é€šè¿‡å??å­—åŠ è½½åœºæ™¯
        /// </summary>
        /// <param name="sceneName">The scene name. åœºæ™¯å??å­—</param>
        /// <param name="callback">The scene load callback. åœºæ™¯åŠ è½½å›žè°ƒ</param>
        /// <param name="transition">The black screen transition speed. è¿‡åº¦é»‘å±?é€ŸçŽ‡</param>
        public void LoadSceneWithName(string sceneName, EAction callback = null, float transition = 1.0f)
		{
            if (m_bol_IsLoading)
            {
				D.Error("Current time is loading, please wait a moment.");
				return;
            }
            if (string.IsNullOrEmpty(sceneName))
            {
				D.Error("Please check scene name,because it is null or empty.");
				return;
            }
            if (CurrentScene == sceneName)
            {
				D.Error("The scenario is in use. Do not load the file again");
				//é˜²æ­¢è¿”å›žmainSecne é‡?å¤?åˆ›å»ºUI_root and _global
				callback?.Invoke();
				return;
            }
			m_flt_transition = transition;
			CurrentScene = sceneName;
			m_act_Callback = callback;
			StartCoroutine(LoadScene());
		}


        /// <summary>
        /// Load scene with name.é€šè¿‡å??å­—ç›´æŽ¥åŠ è½½åœºæ™¯,æ— è¿‡åº¦åŠ¨ç”»
        /// </summary>
        /// <param name="sceneName">The scene name. åœºæ™¯å??å­—</param>
        /// <param name="callback">The scene load callback. åœºæ™¯åŠ è½½å›žè°ƒ</param>
        public void LoadSceneWithNameNow(string sceneName, System.Action callback = null)
        {
            if (m_bol_IsLoading)
            {
                D.Error("Current time is loading, please wait a moment.");
                return;
            }
            if (CurrentScene == sceneName)
            {
                D.Error("The scenario is in use. Do not load the file again");
                //é˜²æ­¢è¿”å›žmainSecne é‡?å¤?åˆ›å»ºUI_root and _global
                callback?.Invoke();
                return;
            }
            CurrentScene = sceneName;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            callback?.Invoke();
        }
        #endregion
    }
}
