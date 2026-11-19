/*
 * ================================================
 * Describe:        启动界面反馈（ILauncherView）+ 进入游戏前确认闸门（ILauncherEnterConfirm）。
 *                  Boot view (ILauncherView) + pre-entry confirmation gate (ILauncherEnterConfirm).
 *                  所有 UI 引用均可留空：留空时对应功能自动降级，不抛异常、不卡流程。
 *                  Every UI reference is optional: missing references degrade gracefully without exceptions or stalls.
 *                  契约实现位于 AOT 程序集 ExampleGame，不使用 EF.UI（热更层此时尚不存在）。
 *                  Implemented in the AOT assembly; EF.UI is unavailable here (hot update layer not yet loaded).
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 19:04:45
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-18 22:05:00
 * ScriptVersion:   0.3
 * ================================================
 */

using System;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Launcher;
using UnityEngine;
using UnityEngine.UI;

namespace Launch
{
    /// <summary>
    /// 启动界面：状态提示 / 下载进度 / 进入确认 / 失败重试。
    /// <para>Boot view: state tip, download progress, entry confirmation, and failure retry.</para>
    /// <para>挂在 GameLauncher 上，在 Inspector 拖入 UI 引用即可。字段全部可空：</para>
    /// <para>Attach to GameLauncher and assign UI references in the Inspector. All fields are optional:</para>
    /// <list type="bullet">
    ///   <item><c>GameStart</c> 留空 → 确认闸门自动放行（不会卡住启动）
    ///     <para><c>GameStart</c> null → gate auto-passes (boot never stalls).</para></item>
    ///   <item><c>LogoImage</c> 留空 → 跳过淡出
    ///     <para><c>LogoImage</c> null → skip the fade-out.</para></item>
    ///   <item><c>RetryButton</c> 留空 → 失败只显示文字，不提供重试入口
    ///     <para><c>RetryButton</c> null → show error text only, no retry entry.</para></item>
    /// </list>
    /// </summary>
    public class LaunchViewTips : MonoBehaviour, ILauncherView, ILauncherEnterConfirm
    {
        #region Inspector 字段

        [Header("状态 / 进度")] [SerializeField]
        private Text Contents;

        [SerializeField] private Text Progress;
        [SerializeField] private Text Bytes;
        [SerializeField] private Slider ProgressBar;

        [Header("Logo 淡出")] [SerializeField] private Image LogoImage;
        [SerializeField, Range(0.5f, 5f)] private float LogoFadeDuration = 2f;

        [Header("进入游戏确认")] [SerializeField] private Button GameStart;

        [Header("失败 / 重试")] [SerializeField] private Text ErrorText;
        [SerializeField] private Button RetryButton;

        #endregion

        #region 私有状态

        private Action _onComplete;
        private Action _retry;
        private bool _confirmConsumed;

        #endregion

        #region 生命周期

        private void Awake()
        {
            SetActive(GameStart, false);
            SetActive(RetryButton, false);
            SetText(ErrorText, string.Empty, true);

            if (GameStart != null)
                GameStart.onClick.AddListener(OnGameStartClicked);
            if (RetryButton != null)
                RetryButton.onClick.AddListener(OnRetryClicked);
        }

        private void Start()
        {
            FadeLogoAsync().Forget();
        }

        private void OnDestroy()
        {
            if (GameStart != null)
                GameStart.onClick.RemoveListener(OnGameStartClicked);
            if (RetryButton != null)
                RetryButton.onClick.RemoveListener(OnRetryClicked);
        }

        #endregion

        #region Logo 淡出

        private async UniTaskVoid FadeLogoAsync()
        {
            if (LogoImage == null || LogoFadeDuration <= 0f)
                return;

            Color color = LogoImage.color;
            float fromAlpha = color.a;
            float elapsed = 0f;
            var token = this.GetCancellationTokenOnDestroy();

            while (!token.IsCancellationRequested && elapsed < LogoFadeDuration)
            {
                await UniTask.NextFrame(token);
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(fromAlpha, 0f, Mathf.Clamp01(elapsed / LogoFadeDuration));
                if (LogoImage != null)
                    LogoImage.color = color;
            }

            if (LogoImage != null)
                LogoImage.enabled = false;
        }

        #endregion

        #region ILauncherView

        /// <summary>
        /// 状态切换。按 state 解析展示文案，并同步按钮显隐。
        /// <para>State changed. Resolves display text from the state and updates button visibility.</para>
        /// </summary>
        public void OnStateChanged(LauncherState state)
        {
            // 文案由 View 自行解析——Launcher 是通用包，只给状态，不给展示文本
            SetText(Contents, ResolveTip(state));

            // Failed 的文案由 OnError 单独展示；其余状态清掉上一次的错误
            if (state != LauncherState.Failed)
            {
                SetText(ErrorText, string.Empty, true);
                SetActive(RetryButton, false);
            }

            // 确认按钮只在 OnReadyToEnter 时显示，重试等状态下必须收起
            if (state != LauncherState.WaitingForConfirm)
            {
                SetActive(GameStart, false);
            }
        }

        public void OnProgress(long currentCount, long totalCount, long currentBytes, long totalBytes)
        {
            // totalCount == 0 表示当前没有下载任务
            if (totalCount <= 0)
            {
                SetText(Progress, string.Empty);
                SetText(Bytes, string.Empty);
                if (ProgressBar != null)
                    ProgressBar.value = 0f;
                return;
            }

            SetText(Progress, $"{currentCount}/{totalCount}");
            SetText(Bytes, $"{FormatBytes(currentBytes)} / {FormatBytes(totalBytes)}");

            if (ProgressBar == null) return;
            ProgressBar.minValue = 0f;
            ProgressBar.maxValue = totalCount;
            ProgressBar.value = currentCount;
        }

        /// <summary>
        /// 启动失败。按 code 解析展示文案，显示重试入口。
        /// <para>Boot failed. Resolves display text from the code and shows the retry entry.</para>
        /// </summary>
        public void OnError(LauncherErrorCode code, Action retry)
        {
            _retry = retry;
            SetText(ErrorText, $"启动失败 [{code}] {ResolveError(code)}", true);
            SetActive(RetryButton, retry != null);

            // 失败时确认按钮必须收起，避免误点放行
            SetActive(GameStart, false);
        }

        #endregion

        #region ILauncherEnterConfirm

        /// <summary>
        /// 热更已就绪、即将进入游戏。显示确认按钮等待用户点击；无按钮则自动放行。
        /// <para>Hot update ready. Shows the confirm button and awaits a click; auto-passes if no button is assigned.</para>
        /// </summary>
        public void OnReadyToEnter(Action onComplete)
        {
            _onComplete = onComplete;
            _confirmConsumed = false;

            if (GameStart == null)
            {
                D.Warning("[LaunchViewTips] 未绑定 GameStart 按钮，确认闸门自动放行。" +
                          "在 Inspector 的「进入游戏确认」处拖入 UGUI Button 即可启用。");
                Confirm();
                return;
            }

            SetActive(GameStart, true);
        }

        #endregion

        #region 按钮回调

        private void OnGameStartClicked()
        {
            Confirm();
        }

        private void Confirm()
        {
            if (_confirmConsumed)
                return; // 幂等：重复点击只放行一次

            _confirmConsumed = true;
            SetActive(GameStart, false);

            Action callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        private void OnRetryClicked()
        {
            Action retry = _retry;
            _retry = null;
            SetActive(RetryButton, false);
            retry?.Invoke();
        }

        #endregion

        #region 文案解析（Launcher 是通用包，不提供展示文本，文案在此收口）

        private static string ResolveTip(LauncherState state)
        {
            return state switch
            {
                LauncherState.Idle => "准备中",
                LauncherState.Booting => "初始化",
                LauncherState.Initializing => "初始化资源系统",
                LauncherState.CheckingVersion => "检查资源版本",
                LauncherState.Downloading => "更新资源",
                LauncherState.LoadingMetadata => "加载 AOT 元数据",
                LauncherState.LoadingHotfix => "加载热更程序集",
                LauncherState.Invoking => "启动游戏",
                LauncherState.Completed => "进入游戏",
                LauncherState.Failed => "启动失败",
                LauncherState.Retrying => "准备重试",
                LauncherState.WaitingForConfirm => "等待进入游戏",
                _ => state.ToString(),
            };
        }

        private static string ResolveError(LauncherErrorCode code)
        {
            return code switch
            {
                LauncherErrorCode.ConfigInvalid => "配置缺失或不合法",
                LauncherErrorCode.PackageInitFailed => "资源包初始化失败",
                LauncherErrorCode.VersionRequestFailed => "获取资源版本失败",
                LauncherErrorCode.ManifestUpdateFailed => "更新资源清单失败",
                LauncherErrorCode.DownloadFailed => "资源下载失败",
                LauncherErrorCode.MetadataLoadFailed => "AOT 元数据加载失败",
                LauncherErrorCode.AssemblyLoadFailed => "热更程序集加载失败",
                LauncherErrorCode.EntryNotFound => "找不到热更入口",
                LauncherErrorCode.EntryThrew => "热更入口执行异常",
                LauncherErrorCode.HookFailed => "启动钩子执行异常",
                LauncherErrorCode.RetryExhausted => "重试次数已用尽",
                LauncherErrorCode.Unexpected => "启动流程出现未知异常",
                LauncherErrorCode.ConfirmFailed => "进入确认钩子异常",
                _ => code.ToString(),
            };
        }

        #endregion

        #region 工具

        private static void SetText(Text label, string value, bool failed = false)
        {
            if (label != null)
                label.text = value;
            switch (failed)
            {
                case true when !string.IsNullOrEmpty(value):
                    D.Error(value);
                    break;
                case false when !string.IsNullOrEmpty(value):
                    D.Log(value);
                    break;
            }
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            if (bytes < 1024L) return $"{bytes} B";
            if (bytes < 1024L * 1024L) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }

        #endregion
    }
}