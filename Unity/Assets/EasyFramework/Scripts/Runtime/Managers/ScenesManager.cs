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

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EasyFramework.Managers
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class ScenesManager : Singleton<ScenesManager>, IManager
    {
        int m_managerLevel = -99;
        int IManager.ManagerLevel
        {
            get
            {
                if (m_managerLevel < -1)
                    m_managerLevel = EF.Projects.AppConst.ManagerLevels.IndexOf(Name);
                return m_managerLevel;
            }
        }

        bool m_bol_IsLoading;
		float m_flt_transition = 1.0f;

        Image m_img_BG;
        Text m_txt_PCTN;
        Transform LoadCanvas;
		Slider m_slid_ProgressBar;
		AsyncOperation m_asyncOperation;

		Action m_act_Callback;
        void ISingleton.Init()
        {
            LoadCanvas = Object.Instantiate(EF.Load.LoadInResources<Transform>("Prefabs/UI/LoadCanvas"));
            LoadCanvas.SetParent(EF.Singleton);
            m_img_BG = LoadCanvas.GetChild(0).GetComponent<Image>();
            m_slid_ProgressBar = m_img_BG.transform.GetChild(0).GetComponent<Slider>();
            m_txt_PCTN = m_slid_ProgressBar.transform.Find("Handle Slide Area/Handle/Text").GetComponent<Text>();

			LoadCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(Screen.width, Screen.height);
            LoadCanvas.gameObject.SetActive(false);
        }

        void ISingleton.Quit()
        {
            EF.StopAllCoroutine();
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
			EF.StopCoroutines(LoadScene());
		}
        #endregion

        #region PUBLIC
        /// <summary>
        /// Get current scene name.
        /// <para>获取当前场景名字</para>
        /// </summary>
        public string CurrentScene { get; private set; }

        /// <summary>
        /// Load scene with name.
        /// <para>通过名字加载场景</para>
        /// </summary>
        /// <param name="sceneName">The scene name. <para>场景名字</para></param>
        /// <param name="callback">The scene load callback. <para>场景加载回调</para></param>
        /// <param name="transition">The black screen transition speed. <para>过度黑屏速率</para></param>
        public void LoadSceneWithName(string sceneName, Action callback = null, float transition = 1.0f)
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
				//防止返回mainSecne 重复创建UI_root and _global
				callback?.Invoke();
				return;
            }
			m_flt_transition = transition;
			CurrentScene = sceneName;
			m_act_Callback = callback;
			EF.StartCoroutines(LoadScene());
		}


        /// <summary>
        /// Load scene with name.
        /// <para>通过名字直接加载场景,无过度动画</para>
        /// </summary>
        /// <param name="sceneName">The scene name. <para>场景名字</para></param>
        /// <param name="callback">The scene load callback. <para>场景加载回调</para></param>
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
                //防止返回mainSecne 重复创建UI_root and _global
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
