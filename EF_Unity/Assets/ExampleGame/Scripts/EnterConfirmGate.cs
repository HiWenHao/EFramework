/*
 * ================================================
 * Describe:      进入游戏前的确认闸门（AOT 侧 UGUI，实现 ILauncherEnterConfirm）
 * Author:        Alvin8412
 * CreationTime:  2026-11-18 21:30:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-18 21:30:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using EasyFramework.Launcher;
using UnityEngine;
using UnityEngine.UI;

namespace EFExample
{
    /// <summary>
    /// 进入游戏前的确认闸门。挂到 GameLauncher 物体或其子物体上即生效。
    /// <para>按钮默认隐藏；热更程序集加载完毕后由 Launcher 调用 OnReadyToEnter，此时才显示。</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnterConfirmGate : MonoBehaviour, ILauncherEnterConfirm
    {
        [Header("确认按钮 / Confirm button")]
        [SerializeField] private Button _enterButton;

        [Header("就绪提示 / Ready tip（可选）")]
        [SerializeField] private GameObject _readyTip;

        private Action _onComplete;

        private void Awake()
        {
            if (_enterButton == null)
            {
                Debug.LogError("[EnterConfirmGate] 未绑定 _enterButton，确认闸门将永远无法放行。");
                enabled = false;
                return;
            }

            _enterButton.gameObject.SetActive(false);
            _enterButton.onClick.AddListener(OnConfirmClicked);

            if (_readyTip != null)
                _readyTip.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_enterButton != null)
                _enterButton.onClick.RemoveListener(OnConfirmClicked);
        }

        private void OnConfirmClicked()
        {
            var callback = _onComplete;
            _onComplete = null; // 幂等：重复点击只放行一次

            // 收起自身，避免热更层 UI 打开后仍盖在上层
            _enterButton.gameObject.SetActive(false);
            if (_readyTip != null)
                _readyTip.SetActive(false);

            callback?.Invoke();
        }

        public void OnReadyToEnter(Action onComplete)
        {
            _onComplete = onComplete;
            _enterButton.gameObject.SetActive(true);
            if (_readyTip != null)
                _readyTip.SetActive(true);
        }
    }
}
