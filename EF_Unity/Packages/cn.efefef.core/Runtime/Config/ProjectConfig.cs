/*
 * ================================================
 * Describe:      This script is used to  .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-14 11:43:10
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-14 11:43:10
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using UnityEngine;

namespace EasyFramework
{
    /// <summary>
    /// 框架设置界面
    /// </summary>
    public class ProjectConfig : ScriptableObject
    {
        [SerializeField, HeaderPro("自动判断显示语言", "Automatically detect display language")]
        private bool _autoLanguage = true;

        [SerializeField, HeaderPro("显示语言, AutoLanguage 为 false 时生效.", "The Display Language, effective when AutoLanguage is false.")]
        private SystemLanguage _displayLanguage;

        /// <summary>
        /// 自动判断显示语言（true 时忽略 DisplayLanguage，跟随系统语言）
        /// <para>Automatically detect the display language; when true, DisplayLanguage is
        /// ignored and the system language is followed.</para>
        /// </summary>
        public bool AutoLanguage { get => _autoLanguage; set => _autoLanguage = value; }

        /// <summary>
        /// 显示语言（AutoLanguage 为 false 时生效；取值与编辑器语言 code 同为 SystemLanguage 枚举名）
        /// <para>The display language; effective when AutoLanguage is false. Uses the same
        /// SystemLanguage enum name as the editor language codes.</para>
        /// </summary>
        public SystemLanguage DisplayLanguage { get => _displayLanguage; set => _displayLanguage = value; }

        [SerializeField, HeaderPro("是否在启动时跳过 Splash（构建版请按授权要求配置）",
            "Whether to skip the splash screen at startup (configure per license terms in builds)")]
        private bool _skipSplashScreen;

        /// <summary>
        /// 启动时是否跳过 Splash。构建版需按授权协议决定是否开启。
        /// <para>Whether to skip the splash screen at startup. Configure per license terms in builds.</para>
        /// </summary>
        public bool SkipSplashScreen => _skipSplashScreen;

        [SerializeField, HeaderPro("脚本版本号", "The script version")]
        private string _scriptVersion = "0.1";

        /// <summary> 脚本版本号（新建/更新脚本头部注释时使用） </summary>
        public string ScriptVersion => _scriptVersion;

        public string ScriptNamespace => _scriptNamespace;

        [SerializeField, HeaderPro("脚本命名空间", "The script namespace")]
        private string _scriptNamespace = "PleaseModifyTheNamespace.";

        [SerializeField, HeaderPro("出包资源存放地（全工程唯一的 CDN 配置）",
            "Resource location; the single CDN configuration for the whole project")]
        private ResourcesArea _resourcesArea;

        public ResourcesArea ResourcesArea => _resourcesArea;

        [SerializeField, HeaderPro("项目常量设置", "Project constant settings")]
        private AppConstConfig _appConst;

        public AppConstConfig AppConst => _appConst;
    }

    /// <summary>
    /// 资源存放地。<b>资源地址的唯一真源</b>：启动器（EF.Launcher）与热更层（EF.YooAsset 分包）
    /// 都读这里，因此工程装不装启动器都能用，也不存在两份 CDN 配置互相漂移。
    /// <para>Resource location. The single source of truth for the CDN address: both the launcher and the
    /// hot update layer read it, so EF.YooAsset works with or without EF.Launcher and no second copy can drift.</para>
    /// </summary>
    [Serializable]
    public class ResourcesArea
    {
        [SerializeField, HeaderPro("内网资源地址", "Intranet resource address")]
        private string m_InnerUrl = "http://127.0.0.1:8080";

        /// <summary> 内网资源地址 </summary>
        public string InnerUrl => m_InnerUrl;

        [SerializeField, HeaderPro("备用服务器", "Standby resource server address")]
        private string m_StandbyUrl = "http://127.0.0.1:8080";

        /// <summary> 备用资源地址 </summary>
        public string StandbyUrl => m_StandbyUrl;
    }

    /// <summary>
    /// 项目常量
    /// </summary>
    [Serializable]
    public class AppConstConfig
    {
        /// <summary>
        /// 默认前缀。除做本地化键前缀外，也用作 <c>EditorPrefs</c> 的分区前缀（逐用户设置靠它隔离工程）。
        /// <para>Default prefix. Also partitions EditorPrefs so per-user settings do not leak across projects.</para>
        /// </summary>
        public const string DefaultAppPrefix = "EF_";

        [SerializeField, HeaderPro("项目本地化保存内容时的前缀", "The prefix when the project localizes the content")]
        private string _appPrefix = DefaultAppPrefix;

        /// <summary>
        /// 保存内容时的前缀
        /// </summary>
        public string AppPrefix => _appPrefix;

        [SerializeField, HeaderPro("Resources文件夹下存放 UI面板预制件 的路径", "UI prefabs path in the resource folder")]
        private string _uiPath = "Prefabs/UI/";

        public string UIPrefabsPath => _uiPath;

        [SerializeField, HeaderPro("Resources文件夹下存放 音频文件 的路径", "Audio path in the resource folder")]
        private string _audioPath = "Sources/";

        public string AudioPath => _audioPath;
    }
}