/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-17 14:02:47
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 16:47:08
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// 项目设置面板
    /// <para>Project settings panel.</para>
    /// </summary>
    [EFConfigPanel(Priority = 0)]
    internal class EFProjectConfigPanel : EFSerializedConfigPanelBase<ProjectConfig>
    {
        #region 品牌横幅常量 / Brand banner constants

        private const string BrandTitle = "Easy Framework";
        private const string BrandMark = "EF";
        private const float HeroCardHeight = 88f;           // Hero 卡片高度
        private const int CardTextureSize = 64;             // 卡片圆角纹理尺寸
        private const int CardCornerRadius = 12;            // 卡片圆角半径
        private const int MarkTextureSize = 64;             // 徽标渐变纹理尺寸
        private const float MarkBlockSize = 52f;            // 渐变方块边长
        private const int MarkCornerRadius = 12;            // 渐变方块圆角半径
        private const float MarkToTitleGap = 20f;           // 方块与标题间距
        private const float GlowSpread = 6f;                // 方块光晕外扩像素
        private const int TitleFontSize = 22;
        private const int MarkFontSize = 26;
        private const float MascotButtonWidth = 26f;   // 吉祥物切换按钮宽度
        private const float MascotButtonHeight = 22f;  // 吉祥物切换按钮高度
        private const float MascotButtonMargin = 6f;   // 吉祥物切换按钮边距

        // 以下色值取样自 EF_Icon.png 的实际像素：深灰底 + 青/红双色字母
        private static readonly Color HeroCardBg = new Color(0.15f, 0.16f, 0.17f, 1f);
        private static readonly Color BrandCyan = new Color(0.424f, 0.788f, 0.788f, 1f);
        private static readonly Color BrandCyanBright = new Color(0.345f, 0.933f, 0.933f, 1f);
        private static readonly Color BrandRed = new Color(0.82f, 0.529f, 0.529f, 1f);
        private static readonly Color TitleTextColor = new Color(0.93f, 0.94f, 0.94f, 1f);
        private static readonly Color MarkTextColor = Color.white;

        private static readonly GUIContent TitleContent = new GUIContent(BrandTitle);
        private static readonly GUIContent MarkContent = new GUIContent(BrandMark);

        #endregion

        #region 跑马灯动画常量 / Marquee animation constants

        private const float MarqueeSpeed = 16f;          // 光团每秒前进的格数
        private const float MarqueeDotSpacing = 12f;     // 光点间距（像素）
        private const float MarqueeDotSize = 4f;         // 光点方块边长
        private const int MarqueeHeadCount = 3;          // 同时流动的光团数量
        private const int MarqueeTrail = 6;              // 每个光团的拖尾格数
        private const float MarqueeInset = 4f;           // 光点距卡片内边缘的距离
        private const float MarqueeMinAlpha = 0.06f;     // 光点最暗透明度
        private const float MarqueeMaxAlpha = 1f;        // 光点最亮透明度
        private const float MarkGlowAlpha = 0.16f;       // 徽标静态柔光透明度

        #endregion

        #region 字段 / Fields

        private static Texture2D _cardTexture;               // 卡片圆角底纹（程序化生成）
        private static Texture2D _markTexture;               // 徽标青红渐变圆角纹理（程序化生成）
        private static GUIStyle _cardStyle;                  // 卡片九宫格样式
        private static GUIStyle _heroTitleStyle;             // Hero 标题样式
        private static GUIStyle _markTextStyle;              // 徽标文字样式

        private readonly EFCatMascot _cat = new EFCatMascot(); // 通用猫咪吉祥物

        bool _systemInfoSwitch;
        string _editorUser;
        private string _systemInfoText; // 缓存的系统信息文本（持续重绘下避免每帧重建）

        private int _systemInfoTextVersion; // 构建缓存时的语言状态版本（语言变更需重建）

        private string[] _languageDisplayNames; // 缓存的语言下拉显示名（持续重绘下避免每帧分配）

        private int _languageDisplayNamesVersion; // 构建缓存时的语言状态版本

        // 署名是逐用户设置（EFTools/Local/EditorUser.txt），不是 ProjectConfig 的字段
        string _authorInput;

        private SerializedProperty _scriptVersion;
        private SerializedProperty _resourcesArea;
        private SerializedProperty _appConstConfig;
        private SerializedProperty _scriptNamespace;

        #endregion

        public override string Name => LC.Combine("Core.ProjectSettings");

        public override bool NeedsContinuousRepaint => true;

        public override void OnEnable(string assetsPath)
        {
            LC.ReloadLanguages();
            base.OnEnable(assetsPath);
        }

        public override void OnDestroy()
        {
            _cat.Dispose();
            ReleaseBannerAssets();
            base.OnDestroy();
        }

        public override void LoadWindowData()
        {
            BindConfig(ConfigManager.Project);
            _appConstConfig = FindConfigProperty("_appConst");
            _scriptVersion = FindConfigProperty("_scriptVersion");
            _resourcesArea = FindConfigProperty("_resourcesArea");
            _scriptNamespace = FindConfigProperty("_scriptNamespace");
            _authorInput = LocalUserSettings.Author;

            var type = typeof(UnityEditor.Connect.UnityOAuth).Assembly.GetType("UnityEditor.Connect.UnityConnect");
            MethodInfo methodInfo = type.GetMethod("GetUserInfo");
            PropertyInfo instance = type.GetProperty("instance");
            var userInfo = methodInfo.Invoke(instance.GetValue(null), null);
            var userInfoType = userInfo.GetType();
            PropertyInfo propertyInfo = userInfoType.GetProperty("displayName");
            _editorUser = (string)propertyInfo.GetValue(userInfo);
            EditorPrefs.SetString($"{ConfigManager.Project.AppConst.AppPrefix}EditorUser", _editorUser);

            _systemInfoSwitch = true;
        }

        protected override void OnPanelGUI()
        {
            DrawBrandBanner();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);
            DrawLanguageSelector();

            // 全部内容（脚本作者以下 + 硬件信息）统一放进同一个滚动视窗
            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition, GUIUtils.ScrollViewBackground());

            // 逐用户信息必须放在「配置改动检查」之外：改名字只该写本机偏好，
            // 不能把 ProjectConfig 变脏——否则 A、B 两台机器各自改名，都会让同一个资产进提交
            EditorGUILayout.LabelField(LC.Combine("Core.EditorUser"), _editorUser);

            // DelayedTextField：只在回车/失焦时提交，避免每敲一个字符写一次偏好
            _authorInput = EditorGUILayout.DelayedTextField(LC.Combine("Core.ScriptAuthor"), _authorInput);
            if (!string.Equals(_authorInput, LocalUserSettings.Author, System.StringComparison.Ordinal))
                LocalUserSettings.SaveAuthor(_authorInput);

            bool configChanged;
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                _scriptVersion.stringValue = EditorGUILayout.TextField(LC.Combine("Core.ScriptVersion"),
                    _scriptVersion.stringValue);
                _scriptNamespace.stringValue = EditorGUILayout.TextField(LC.Combine("ScriptNamespace"),
                    _scriptNamespace.stringValue);

                EditorGUILayout.PropertyField(_appConstConfig);

                EditorGUILayout.Space(6);
                EditorGUILayout.PropertyField(_resourcesArea);
                EditorGUILayout.Space(20);

                configChanged = changeCheckScope.changed;
            }

            // 硬件信息并入同一滚动视窗：折叠时标题仍留在视窗内，不再掉到最下方
            SystemInfos();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            ApplyConfigIfChanged(configChanged, withoutUndo: true);
        }

        /// <summary>
        /// 绘制编辑器语言选择行：下拉 + 新增语言 + 刷新，附当前语言摘要（复合语句）
        /// <para>Draw the editor language row: dropdown + add + refresh, with a composite-sentence summary.</para>
        /// </summary>
        private void DrawLanguageSelector()
        {
            var options = LC.GetLanguageOptions();
            // 下拉显示名数组按语言状态版本缓存，避免持续重绘下每帧分配
            if (_languageDisplayNames == null || _languageDisplayNamesVersion != LC.LanguageVersion)
            {
                _languageDisplayNames = new string[options.Count];
                for (int i = 0; i < options.Count; i++)
                    _languageDisplayNames[i] = options[i].DisplayName;
                _languageDisplayNamesVersion = LC.LanguageVersion;
            }

            EditorGUILayout.BeginHorizontal();
            int previousIndex = LC.GetSelectedIndex();
            int selectedIndex = EditorGUILayout.Popup(
                LC.Combine("Core.DisplayLanguage"), previousIndex, _languageDisplayNames);
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(22f)))
                EFEditorLanguageCreateWindow.OpenWindow();
            if (GUILayout.Button("↻", EditorStyles.miniButton, GUILayout.Width(22f)))
                LC.ReloadLanguages();
            EditorGUILayout.EndHorizontal();

            if (selectedIndex != previousIndex)
                LC.EditorLanguageCode = options[selectedIndex].Code;
        }

        // 纹理与样式均为懒加载
        private static void EnsureBannerAssets()
        {
            if (_cardTexture)
                return;

            _cardTexture = MakeRoundedTexture(CardTextureSize, CardCornerRadius, HeroCardBg);
            _markTexture = MakeRoundedTexture(MarkTextureSize, MarkCornerRadius, BrandCyan, BrandRed);

            _cardStyle = new GUIStyle
            {
                border = new RectOffset(CardCornerRadius, CardCornerRadius, CardCornerRadius, CardCornerRadius)
            };
            FillBackground(_cardStyle, _cardTexture);

            _heroTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = TitleFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            FillTextColor(_heroTitleStyle, TitleTextColor);

            _markTextStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = MarkFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            FillTextColor(_markTextStyle, MarkTextColor);
        }

        // 为样式所有状态填充同一背景，避免鼠标悬停时切到空白 hover 状态而变色
        private static void FillBackground(GUIStyle style, Texture2D texture)
        {
            style.normal.background = texture;
            style.hover.background = texture;
            style.active.background = texture;
            style.focused.background = texture;
            style.onNormal.background = texture;
            style.onHover.background = texture;
            style.onActive.background = texture;
            style.onFocused.background = texture;
        }

        // 为样式所有状态填充同一文字色，避免鼠标交互导致文字变色
        private static void FillTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        // 窗口销毁时释放全部程序化生成的纹理与样式
        private static void ReleaseBannerAssets()
        {
            if (_cardTexture)
                Object.DestroyImmediate(_cardTexture);
            if (_markTexture)
                Object.DestroyImmediate(_markTexture);
            _cardTexture = null;
            _markTexture = null;
            _cardStyle = null;
            _heroTitleStyle = null;
            _markTextStyle = null;
        }

        // 生成圆角矩形纹理；end 不为空时按横向做 start→end 渐变
        private static Texture2D MakeRoundedTexture(int size, int radius, Color start, Color? end = null)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float half = size * 0.5f;
            float inner = half - radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - half;
                    float py = y + 0.5f - half;
                    float qx = Mathf.Abs(px) - inner;
                    float qy = Mathf.Abs(py) - inner;
                    float mx = Mathf.Max(qx, 0f);
                    float my = Mathf.Max(qy, 0f);
                    float dist = Mathf.Sqrt(mx * mx + my * my) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float coverage = Mathf.Clamp01(0.5f - dist);
                    Color color = end.HasValue ? Color.Lerp(start, end.Value, (float)x / (size - 1)) : start;
                    color.a *= coverage;
                    pixels[y * size + x] = color;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        // 绘制品牌横幅：圆角 Hero 卡片内居中放置青红渐变 EF 方块 + 标题，边框跑马灯 + 通用猫咪吉祥物
        private void DrawBrandBanner()
        {
            Rect card = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                GUILayout.Height(HeroCardHeight), GUILayout.ExpandWidth(true));
            EnsureBannerAssets();

            // Label 非交互、内部按 Repaint 绘制；配合全状态背景，既不 hover 变色也不触发 Draw 事件异常
            GUI.Label(card, GUIContent.none, _cardStyle);
            DrawMarquee(card);

            float titleWidth = _heroTitleStyle.CalcSize(TitleContent).x;
            float contentWidth = MarkBlockSize + MarkToTitleGap + titleWidth;
            float contentX = card.center.x - contentWidth * 0.5f;
            float contentY = card.center.y - MarkBlockSize * 0.5f;
            Rect markRect = new Rect(contentX, contentY, MarkBlockSize, MarkBlockSize);

            // 标题先画：猫咪路过时可覆盖 "Easy Framework"
            GUI.Label(new Rect(markRect.xMax + MarkToTitleGap, card.y, titleWidth, card.height),
                TitleContent, _heroTitleStyle);

            // 猫咪在标题之上绘制
            _cat.Draw(card, EditorApplication.timeSinceStartup);

            // EF 方块最后画：压在猫咪之上，猫路过时不会遮盖 EF 标识
            Color previous = GUI.color;
            Color glowColor = Color.white;
            glowColor.a = MarkGlowAlpha;
            GUI.color = glowColor;
            GUI.DrawTexture(new Rect(markRect.x - GlowSpread, markRect.y - GlowSpread,
                    markRect.width + GlowSpread * 2f, markRect.height + GlowSpread * 2f), _markTexture,
                ScaleMode.StretchToFill, true);
            GUI.color = previous;

            GUI.DrawTexture(markRect, _markTexture, ScaleMode.StretchToFill, true);
            GUI.Label(markRect, MarkContent, _markTextStyle);

            DrawMascotSelector(card);
        }

        // 绘制吉祥物切换按钮：下拉选择内置/已有自定义素材，或新建自定义素材目录
        private void DrawMascotSelector(Rect card)
        {
            Rect buttonRect = new Rect(card.xMax - MascotButtonWidth - MascotButtonMargin,
                card.y + MascotButtonMargin, MascotButtonWidth, MascotButtonHeight);
            if (!GUI.Button(buttonRect, EditorGUIUtility.IconContent("d_FilterByLabel"), EditorStyles.miniButton))
                return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(LC.Combine("Core.MascotBuiltin")), string.IsNullOrEmpty(_cat.MascotName),
                () => _cat.MascotName = string.Empty);

            string[] existing = EFMascotConfig.GetExistingMascotNames();
            foreach (string name in existing)
            {
                string captured = name;
                menu.AddItem(new GUIContent(captured), _cat.MascotName == name, () => _cat.MascotName = captured);
            }

            if (existing.Length > 0)
                menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent(LC.Combine("Core.MascotNewCustom")), false, EFMascotCreateWindow.OpenWindow);
            menu.ShowAsContext();
        }

        // 沿卡片矩形边框绘制循环跑马灯：光点按周长均匀分布，多个光团带拖尾循环推进
        private static void DrawMarquee(Rect card)
        {
            float width = card.width - MarqueeInset * 2f;
            float height = card.height - MarqueeInset * 2f;
            if (width <= 0f || height <= 0f)
                return;

            float perimeter = 2f * (width + height);
            int dotCount = Mathf.Max(1, Mathf.FloorToInt(perimeter / MarqueeDotSpacing));
            float headDot = (float)(EditorApplication.timeSinceStartup * MarqueeSpeed);

            for (int i = 0; i < dotCount; i++)
            {
                // 该光点到最近光团头部的环形距离（格数）
                float phase = (i - headDot) / dotCount * MarqueeHeadCount;
                float delta = Mathf.Repeat(-phase, 1f) * (dotCount / (float)MarqueeHeadCount);

                float tail = 1f - Mathf.Clamp01(delta / MarqueeTrail);
                float intensity = tail * tail;
                Color color = Color.Lerp(BrandCyanBright, BrandRed, (float)i / dotCount);
                color.a = Mathf.Lerp(MarqueeMinAlpha, MarqueeMaxAlpha, intensity);

                Vector2 point = BorderPoint(card, i, dotCount, width, height);
                EditorGUI.DrawRect(new Rect(point.x, point.y, MarqueeDotSize, MarqueeDotSize), color);
            }
        }

        // 把第 index 个光点映射到矩形边框上的坐标（沿上→右→下→左顺时针分布）
        private static Vector2 BorderPoint(Rect card, int index, int dotCount, float width, float height)
        {
            float distance = (float)index / dotCount * (width + height) * 2f + MarqueeInset;
            float halfW = MarqueeDotSize * 0.5f;
            float top = card.y + MarqueeInset;
            float left = card.x + MarqueeInset;

            if (distance < width)
                return new Vector2(left + distance - halfW, top - halfW);
            distance -= width;
            if (distance < height)
                return new Vector2(card.xMax - MarqueeInset - halfW, top + distance - halfW);
            distance -= height;
            if (distance < width)
                return new Vector2(card.xMax - MarqueeInset - distance - halfW, card.yMax - MarqueeInset - halfW);
            distance -= width;
            return new Vector2(left - halfW, card.yMax - MarqueeInset - distance - halfW);
        }

        /// <summary>
        /// 系统信息
        /// </summary>
        private void SystemInfos()
        {
            _systemInfoSwitch = EditorGUILayout.BeginFoldoutHeaderGroup(_systemInfoSwitch,
                LC.Combine("Core.CurrentHardwareInfo"));
            if (_systemInfoSwitch)
            {
                // 面板处于持续重绘状态（吉祥物动画），此处文本按需重建而非每帧重建，避免每帧产生大字符串分配
                // 失效判据用 LC.LanguageVersion（int），直接读 EditorLanguageCode 会产生枚举转字符串的每帧分配
                if (_systemInfoText == null || _systemInfoTextVersion != LC.LanguageVersion)
                {
                    _systemInfoText = BuildSystemInfoText();
                    _systemInfoTextVersion = LC.LanguageVersion;
                }

                GUILayout.Box(_systemInfoText, "FrameBox", GUILayout.MinWidth(360f));
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }

        // 构建系统信息文本（仅在语言切换时重建）
        private static string BuildSystemInfoText()
        {
            return
                "--------------------------------------------------------------------------------\n" +
                $"{LC.Combine("Core.OperatingSystem")}: {SystemInfo.operatingSystem}\n" +
                $"{LC.Combine("Core.SystemMemorySize")}: {SystemInfo.systemMemorySize} MB\n" +
                $"{LC.Combine("Core.CpuName")}: {SystemInfo.processorType}\n" +
                $"{LC.Combine("Core.CpuCount")}: {SystemInfo.processorCount}\n" +
                "--------------------------------------------------------------------------------\n" +
                $"{LC.Combine("Core.GpuName")}: {SystemInfo.graphicsDeviceName}\n" +
                $"{LC.Combine("Core.GpuType")}: {SystemInfo.graphicsDeviceType}\n" +
                $"{LC.Combine("Core.GpuMemorySize")}: {SystemInfo.graphicsMemorySize} MB\n" +
                $"{LC.Combine("Core.GpuId")}: {SystemInfo.graphicsDeviceID}\n" +
                $"{LC.Combine("Core.GpuVendor")}: {SystemInfo.graphicsDeviceVendor}\n" +
                $"{LC.Combine("Core.GpuVendorId")}: {SystemInfo.graphicsDeviceVendorID}\n" +
                "--------------------------------------------------------------------------------\n" +
                $"{LC.Combine("Core.DeviceVersion")}: {SystemInfo.deviceModel}\n" +
                $"{LC.Combine("Core.DeviceName")}: {SystemInfo.deviceName}\n" +
                $"{LC.Combine("Core.DeviceType")}: {SystemInfo.deviceType}\n" +
                $"{LC.Combine("Core.DeviceId")}: {SystemInfo.deviceUniqueIdentifier}\n" +
                "--------------------------------------------------------------------------------\n" +
                $"{LC.Combine("Core.ScreenCurrent")} Dpi: {Screen.dpi}\n" +
                $"{LC.Combine("Core.ScreenResolution")}: {Screen.currentResolution}\n" +
                "--------------------------------------------------------------------------------";
        }
    }
}