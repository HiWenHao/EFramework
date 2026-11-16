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
using EasyFramework.Edit;
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

        [SerializeField, HeaderPro("脚本作者名", "The script author")]
        private string _scriptAuthor = "Default";

        public string ScriptAuthor => _scriptAuthor;

        [SerializeField, HeaderPro("脚本版本号", "The script version")]
        private string _scriptVersion = "0.1";

        /// <summary> 脚本版本号（新建/更新脚本头部注释时使用） </summary>
        public string ScriptVersion => _scriptVersion;

        public string ScriptNamespace => _scriptNamespace;

        [SerializeField, HeaderPro("脚本命名空间", "The script namespace")]
        private string _scriptNamespace = "PleaseModifyTheNamespace.";

        [SerializeField, HeaderPro("出包资源存放地", "Where the out packet resource is stored")]
        private ResourcesArea _resourcesArea;

        public ResourcesArea ResourcesArea => _resourcesArea;

        [SerializeField, HeaderPro("项目常量设置", "Project constant settings")]
        private AppConstConfig _appConst;

        public AppConstConfig AppConst => _appConst;
    }

    /// <summary>
    /// 资源存放地
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
        [SerializeField, HeaderPro("项目本地化保存内容时的前缀", "The prefix when the project localizes the content")]
        private string _appPrefix = "EF_";

        /// <summary>
        /// 保存内容时的前缀
        /// </summary>
        public string AppPrefix => _appPrefix;

        [SerializeField, HeaderPro("项目当前应用版本", "Current application version of the project")]
        private string _appVersion = "1.0";

        /// <summary>
        /// 应用版本
        /// </summary>
        public string AppVersion => _appVersion;

        [SerializeField, HeaderPro("Resources文件夹下存放 UI面板预制件 的路径", "UI prefabs path in the resource folder")]
        private string _uiPath = "Prefabs/UI/";

        public string UIPrefabsPath => _uiPath;

        [SerializeField, HeaderPro("Resources文件夹下存放 音频文件 的路径", "Audio path in the resource folder")]
        private string _audioPath = "Sources/";

        public string AudioPath => _audioPath;
    }
}