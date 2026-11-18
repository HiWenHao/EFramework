/*
 * ================================================
 * Describe:        This script is used to draw a minimal launcher view with UGUI.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 16:00:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 16:00:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using EasyFramework;
using EasyFramework.Launcher;
using UnityEngine;
using UnityEngine.UI;

namespace EFLauncherSample
{
    /// <summary>
    /// 极简启动界面：运行时用 UGUI 现拼，不依赖预制体和美术资源。
    /// <para>直接复制这个文件改成自己的更新界面即可 —— 想用 DOTween 做动效、想换布局都随你。</para>
    /// </summary>
    /// <remarks>
    /// 注意：这里能用 UGUI / TextMeshPro，<b>不能用 EF.UI</b> —— 它在热更层，此刻还没加载。
    /// </remarks>
    public sealed class SampleLauncherView : MonoBehaviour, ILauncherView
    {
        private Text _stateText;
        private Text _progressText;
        private RectTransform _fillRect;
        private GameObject _retryRoot;
        private GameObject _canvasGo;
        private Action _retryAction;

        private void Awake()
        {
            BuildUi();
        }

        #region ILauncherView

        public void OnStateChanged(LauncherState state)
        {
            // Launcher 只给状态，展示文案由 View 解析（中英双语示例）
            if (_stateText != null)
                _stateText.text = $"{state} · {ResolveTip(state)}";

            if (_retryRoot != null && state != LauncherState.Failed)
                _retryRoot.SetActive(false);

            // 每帧都在刷的启动界面在 Completed 之后就该收场。
            // 只关 Canvas 子物体，不要 SetActive(false) 本物体 —— Launcher 可能挂在同一个物体上；
            // 流程不会因此中断（UniTask 只受销毁取消），但界面会失活，后续状态没人显示。
            if (state == LauncherState.Completed && _canvasGo != null)
                _canvasGo.SetActive(false);
        }

        public void OnProgress(long currentCount, long totalCount, long currentBytes, long totalBytes)
        {
            if (_progressText != null)
            {
                _progressText.text = totalCount <= 0
                    ? string.Empty
                    : $"{currentCount}/{totalCount}   {FormatSize(currentBytes)} / {FormatSize(totalBytes)}";
            }

            if (_fillRect != null)
            {
                float ratio = totalBytes > 0 ? Mathf.Clamp01((float)currentBytes / totalBytes) : 0f;
                _fillRect.anchorMax = new Vector2(ratio, 1f);
            }
        }

        public void OnError(LauncherErrorCode code, Action retry)
        {
            _retryAction = retry;

            if (_stateText != null)
                _stateText.text = $"启动失败 [{code}]\n{ResolveError(code)}";

            if (_retryRoot != null)
                _retryRoot.SetActive(true);

            D.Error($"[LauncherSample] {code}: {ResolveError(code)}");
        }

        #endregion

        #region UI 构建

        private void BuildUi()
        {
            var canvasGo = new GameObject("LauncherCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvasGo = canvasGo;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = CreateRect("Root", canvasGo.transform);
            Stretch(root);
            var background = root.gameObject.AddComponent<RawImage>();
            background.color = new Color(0.08f, 0.09f, 0.10f, 1f);

            _stateText = CreateText("State", root, 40, Color.white, TextAnchor.MiddleCenter);
            Anchor(_stateText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 90f), new Vector2(0f, 100f));

            var barBg = CreateRect("BarBg", root);
            Anchor(barBg, new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.5f),
                new Vector2(0f, 16f), new Vector2(0f, -40f));
            barBg.gameObject.AddComponent<RawImage>().color = new Color(1f, 1f, 1f, 0.12f);

            _fillRect = CreateRect("BarFill", barBg);
            _fillRect.anchorMin = Vector2.zero;
            _fillRect.anchorMax = new Vector2(0f, 1f);
            _fillRect.offsetMin = Vector2.zero;
            _fillRect.offsetMax = Vector2.zero;
            _fillRect.gameObject.AddComponent<RawImage>().color = new Color(0.29f, 0.54f, 0.86f, 1f);

            _progressText = CreateText("Progress", root, 28, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter);
            Anchor(_progressText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 40f), new Vector2(0f, -84f));

            _retryRoot = BuildRetryButton(root);
            _retryRoot.SetActive(false);
        }

        private GameObject BuildRetryButton(Transform parent)
        {
            var rect = CreateRect("Retry", parent);
            Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(320f, 100f), new Vector2(0f, -220f));

            var graphic = rect.gameObject.AddComponent<RawImage>();
            graphic.color = new Color(0.35f, 0.35f, 0.38f, 1f);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.onClick.AddListener(OnRetryClicked);

            var label = CreateText("Label", rect, 32, Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.text = "重试 / Retry";

            return rect.gameObject;
        }

        private void OnRetryClicked()
        {
            var retry = _retryAction;
            _retryAction = null;

            if (_retryRoot != null)
                _retryRoot.SetActive(false);

            retry?.Invoke();
        }

        #endregion

        #region 文案解析

        private static string ResolveTip(LauncherState state)
        {
            return state switch
            {
                LauncherState.Idle => "准备中 / Ready",
                LauncherState.Booting => "初始化 / Initializing",
                LauncherState.Initializing => "初始化资源系统 / Init assets",
                LauncherState.CheckingVersion => "检查资源版本 / Check version",
                LauncherState.Downloading => "更新资源 / Downloading",
                LauncherState.LoadingMetadata => "加载 AOT 元数据 / AOT metadata",
                LauncherState.LoadingHotfix => "加载热更程序集 / Hotfix assemblies",
                LauncherState.Invoking => "启动游戏 / Starting",
                LauncherState.Completed => "进入游戏 / Enter game",
                LauncherState.Failed => "启动失败 / Failed",
                LauncherState.Retrying => "准备重试 / Retrying",
                LauncherState.WaitingForConfirm => "等待进入游戏 / Awaiting confirm",
                _ => state.ToString(),
            };
        }

        private static string ResolveError(LauncherErrorCode code)
        {
            return code switch
            {
                LauncherErrorCode.ConfigInvalid => "配置缺失或不合法 / Invalid config",
                LauncherErrorCode.PackageInitFailed => "资源包初始化失败 / Package init failed",
                LauncherErrorCode.VersionRequestFailed => "获取资源版本失败 / Version request failed",
                LauncherErrorCode.ManifestUpdateFailed => "更新资源清单失败 / Manifest update failed",
                LauncherErrorCode.DownloadFailed => "资源下载失败 / Download failed",
                LauncherErrorCode.MetadataLoadFailed => "AOT 元数据加载失败 / Metadata load failed",
                LauncherErrorCode.AssemblyLoadFailed => "热更程序集加载失败 / Assembly load failed",
                LauncherErrorCode.EntryNotFound => "找不到热更入口 / Entry not found",
                LauncherErrorCode.EntryThrew => "热更入口执行异常 / Entry threw",
                LauncherErrorCode.HookFailed => "启动钩子执行异常 / Boot hook failed",
                LauncherErrorCode.RetryExhausted => "重试次数已用尽 / Retries exhausted",
                LauncherErrorCode.Unexpected => "启动流程出现未知异常 / Unexpected error",
                LauncherErrorCode.ConfirmFailed => "进入确认钩子异常 / Confirm hook failed",
                _ => code.ToString(),
            };
        }

        #endregion

        #region 工具

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max,
            Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, Color color, TextAnchor anchor)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes}B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F1}KB";
            return $"{bytes / (1024f * 1024f):F1}MB";
        }

        #endregion
    }
}
