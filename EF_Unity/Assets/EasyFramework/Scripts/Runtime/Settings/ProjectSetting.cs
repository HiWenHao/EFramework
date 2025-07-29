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
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Edit.Setting
{
    /// <summary>
    /// 框架设置界面
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectSetting", menuName = "EF/ProjectSetting", order = 200)]
    public class ProjectSetting : ScriptableObject
    {
        [SerializeField, Header(LanguagAttribute.LanguageIndex)]
        private int _languageIndex;
        public int LanguageIndex => _languageIndex;

        [SerializeField, Header(LanguagAttribute.RendererPipline)]
        private int _rendererPipline;
        public int RendererPipline => _rendererPipline;

        [SerializeField, Header(LanguagAttribute.ScriptAuthor)]
        private string _scriptAuthor = "Default";
        public string ScriptAuthor => _scriptAuthor;

        [SerializeField, Header(LanguagAttribute.ScriptVersion)]
        private string _scriptVersion = "0.1";
        public string ScriptVersion => _scriptVersion;

        [SerializeField, Header(LanguagAttribute.ResourcesArea)]
        private ResourcesArea _resourcesArea;
        public ResourcesArea ResourcesArea => _resourcesArea;

        [SerializeField, Header(LanguagAttribute.AppConst)]
        private AppConstConfig _appConst;
        public AppConstConfig AppConst => _appConst;

        //[Header("项目常量")]
        //[SerializeField]
        //private AppConstConfig m_AppConst;
        //public AppConstConfig AppConst => m_AppConst;



        //[Header("Hotfix")]
        //[SerializeField]
        //private string m_ResourceVersionFileName = "ResourceVersion.txt";
        //public string ResourceVersionFileName { get { return m_ResourceVersionFileName; } }
        //public string WindowsAppUrl = "";
        //public string MacOSAppUrl = "";
        //public string IOSAppUrl = "";
        //public string AndroidAppUrl = "";
        //[Header("Server")]
        //[SerializeField]
        //private string m_CurUseServerChannel;
        //public string CurUseServerChannel => m_CurUseServerChannel;
        //[SerializeField]
        //private List<ServerChannelInfo> m_ServerChannelInfos;

        //public List<ServerChannelInfo> ServerChannelInfos
        //{
        //    get => m_ServerChannelInfos;
        //}

        //[Header("Config")]
        //[Tooltip("是否读取本地表 UnityEditor 下起作用")]
        //[SerializeField] private bool m_IsReadLocalConfigInEditor = true;
        //public bool ReadLocalConfigInEditor { get { return m_IsReadLocalConfigInEditor; } }
        //[SerializeField]
        //private string m_ConfigVersionFileName = "ConfigVersion.xml";
        //public string ConfigVersionFileName { get { return m_ConfigVersionFileName; } }
        //[SerializeField]
        //private string m_ConfigFolderName = "LubanConfig";
        //public string ConfigFolderName { get { return m_ConfigFolderName; } }
    }

    /// <summary>
    /// 渲染管线类型
    /// </summary>
    public enum RenderingTypeEnum
    {
        BuiltIn = 0,
        URP,
        //HDRP_Pending
    }

    /// <summary>
    /// 服务器类型
    /// </summary>
    public enum ServerTypeEnum
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,
        /// <summary>
        /// 内网
        /// </summary>
        Intranet = 1,
        /// <summary>
        /// 外网
        /// </summary>
        Extranet = 2,
        /// <summary>
        /// 正式服
        /// </summary>
        Formal = 3
    }

    /// <summary>
    /// 程序开发阶段
    /// </summary>
    public enum AppStageEnum
    {
        /// <summary>
        /// 测试版本
        /// </summary>
        Debug = 1,
        /// <summary>
        /// 前期版本
        /// </summary>
        Alpha = 2,
        /// <summary>
        /// 中期版本
        /// </summary>
        Beta = 3,
        /// <summary>
        /// 发布版本
        /// </summary>
        Release = 5
    }

    /// <summary>
    /// 资源存放地
    /// </summary>
    [Serializable]
    public class ResourcesArea
    {
        [SerializeField, Header(LanguagAttribute.CopyResToCommit)] 
        private bool m_CopyResToCommit = false;
        public bool CopyResToCommit => m_CopyResToCommit;

        [SerializeField, Header(LanguagAttribute.CleanCommitPathRes)]
        private bool m_CleanCommitRes = true;
        public bool CleanCommitPathRes => m_CleanCommitRes;

        [SerializeField, Header(LanguagAttribute.ServerType)]
        private ServerTypeEnum m_ServerType = ServerTypeEnum.Intranet;
        public ServerTypeEnum ServerType => m_ServerType;

        [SerializeField, Header(LanguagAttribute.InnerUrl)]
        private string m_InnerUrl = "http://127.0.0.1:8080";
        /// <summary> 内网资源地址 </summary>
        public string InnerUrl => m_InnerUrl;

        [SerializeField, Header(LanguagAttribute.ExtraUrl)]
        private string m_ExtraUrl = "http://127.0.0.1:8080";
        /// <summary> 外网资源地址 </summary>
        public string ExtraUrl => m_ExtraUrl;

        [SerializeField, Header(LanguagAttribute.FormalUrl)]
        private string m_FormalUrl = "http://127.0.0.1:8080";
        /// <summary> 正式资源地址 </summary>
        public string FormalUrl => m_FormalUrl;

        [SerializeField, Header(LanguagAttribute.StandbyUrl)]
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
        [SerializeField, Header(LanguagAttribute.AppName)]
        private string _appName = "EasyFramework";
        /// <summary>
        /// 应用名称
        /// </summary>
        public string AppName => _appName;

        [SerializeField, Header(LanguagAttribute.AppPrefix)]
        private string _appPrefix = "EF_";
        /// <summary>
        /// 保存内容时的前缀
        /// </summary>
        public string AppPrefix => _appPrefix;

        [SerializeField, Header(LanguagAttribute.AppVersion)]
        private string _appVersion = "1.0";
        /// <summary>
        /// 应用版本
        /// </summary>
        public string AppVersion => _appVersion;

        [SerializeField, Header(LanguagAttribute.AppStage)]
        private AppStageEnum _appStage = AppStageEnum.Debug;
        /// <summary>
        /// 开发阶段
        /// </summary>
        public AppStageEnum AppStage => _appStage;


        [SerializeField, Header(LanguagAttribute.UIPrefabsPath)]
        private string _uiPath= "Prefabs/UI/";
        public string UIPrefabsPath => _uiPath;

        [SerializeField, Header(LanguagAttribute.AudioPath)]
        private string _audioPath = "Sources/";
        public string AudioPath => _audioPath;

        [SerializeField, Header(LanguagAttribute.ManagerLevel)]
        private List<string> _managerLevel = new List<string>()
        {
            "TimeManager",
            "ToolManager",
            "EventManager",
            "HttpsManager",
            "SocketManager",
            "FolderManager",
            "LoadManager",
            "ScenesManager",
            "AudioManager",
            "UIManager",
        };
        public List<string> ManagerLevels => _managerLevel;
    }
}
