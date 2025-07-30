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
        bool _isLoading;
		float _transition = 1.0f;

        Image _bg;
        Text _pcTN;
        Transform _loadCanvas;
		Slider _progressBar;
		AsyncOperation _asyncOperation;

		Action _callback;
        void ISingleton.Init()
        {
            _loadCanvas = Object.Instantiate(EF.Load.LoadInResources<Transform>("Prefabs/UI/LoadCanvas"));
            _loadCanvas.SetParent(EF.Singleton);
            _bg = _loadCanvas.GetChild(0).GetComponent<Image>();
            _progressBar = _bg.transform.GetChild(0).GetComponent<Slider>();
            _pcTN = _progressBar.transform.Find("Handle Slide Area/Handle/Text").GetComponent<Text>();

			_loadCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(Screen.width, Screen.height);
            _loadCanvas.gameObject.SetActive(false);
        }

        void ISingleton.Quit()
        {
            EF.StopAllCoroutine();
            _pcTN = null;
            _progressBar = null;
            _bg = null;
            _loadCanvas = null;
            _asyncOperation = null;
        }

        #region Load
		IEnumerator LoadScene()
		{
			_isLoading = true;
			yield return new WaitForSeconds(.1f);
			_loadCanvas.gameObject.SetActive(true);

			while (_bg.color.a < 1.0f)
			{
				Color color = _bg.color;
				color.a += 0.02f * _transition;
				_bg.color = color;
				yield return null;
			}
			_progressBar.value = 0.0f;
			_progressBar.gameObject.SetActive(true);
			while (_progressBar.value < .1f)
			{
				_progressBar.value += .01f;
				_pcTN.text = $"{Mathf.RoundToInt(_progressBar.value * 90)}%";
				yield return null;
			}
			_asyncOperation = SceneManager.LoadSceneAsync(CurrentScene);

            while (!_asyncOperation.isDone)
            {
                while ((_asyncOperation.progress - _progressBar.value) >= .1f)
                {
					_progressBar.value += 0.01f;
					_pcTN.text = $"{Mathf.RoundToInt(_progressBar.value * 100)}%";
					yield return null;
				}
				yield return null;
			}
			yield return new WaitForSeconds(.5f);
            while (_asyncOperation.isDone)
            {
				while (_progressBar.value < 1.0f)
				{
					_progressBar.value += .005f;
					float vla = Mathf.RoundToInt(_progressBar.value * 100);
					_pcTN.text = vla > 100 ? "100%" : $"{vla}%";
					yield return null;
				}
				break;
			}

			yield return new WaitForSeconds(.1f);
			_callback?.Invoke();
			_progressBar.gameObject.SetActive(false);
			while (_bg.color.a > .02f)
			{
				Color color = _bg.color;
				color.a -= 0.02f * _transition;
				_bg.color = color;
				yield return null;
			}
			_loadCanvas.gameObject.SetActive(false);
			_isLoading = false;
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
            if (_isLoading)
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
			_transition = transition;
			CurrentScene = sceneName;
			_callback = callback;
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
            if (_isLoading)
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
