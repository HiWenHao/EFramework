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
        [SerializeField]
        private int m_LanguageIndex;
        public int LanguageIndex => m_LanguageIndex;

        [SerializeField]
        private string m_ScriptAuthor = "Default";
        public string ScriptAuthor => m_ScriptAuthor;

        [SerializeField]
        private string m_ScriptVersion = "0.1";
        public string ScriptVersion => m_ScriptVersion;

        [Header(LanguagAttribute.ResourcesArea)]
        [SerializeField]
        private ResourcesArea m_ResourcesArea;
        public ResourcesArea ResourcesArea => m_ResourcesArea;

        [Header(LanguagAttribute.AppConst)]
        [SerializeField]
        private AppConstConfig m_AppConst;
        public AppConstConfig AppConst => m_AppConst;

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
        /// 后期版本 与发布版本没多大差别
        /// </summary>
        Rc = 4,
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
        [Header(LanguagAttribute.CopyResToCommit)]
        [SerializeField] private bool m_CopyResToCommit = false;
        public bool CopyResToCommit => m_CopyResToCommit;

        [Header(LanguagAttribute.CleanCommitPathRes)]
        [SerializeField] private bool m_CleanCommitRes = true;
        public bool CleanCommitPathRes => m_CleanCommitRes;

        [Header(LanguagAttribute.ServerType)]
        [SerializeField] private ServerTypeEnum m_ServerType = ServerTypeEnum.Intranet;
        public ServerTypeEnum ServerType => m_ServerType;

        [Header(LanguagAttribute.InnerUrl)]
        [SerializeField]
        private string m_InnerUrl = "http://127.0.0.1:8080";
        /// <summary> 内网资源地址 </summary>
        public string InnerUrl => m_InnerUrl;

        [Header(LanguagAttribute.ExtraUrl)]
        [SerializeField]
        private string m_ExtraUrl = "http://127.0.0.1:8080";
        /// <summary> 外网资源地址 </summary>
        public string ExtraUrl => m_ExtraUrl;

        [Header(LanguagAttribute.FormalUrl)]
        [SerializeField]
        private string m_FormalUrl = "http://127.0.0.1:8080";
        /// <summary> 正式资源地址 </summary>
        public string FormalUrl => m_FormalUrl;

        [Header(LanguagAttribute.StandbyUrl)]
        [SerializeField]
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
        [Header(LanguagAttribute.AppName)]
        [SerializeField]
        private string m_AppName = "EasyFramework";
        /// <summary>
        /// 应用名称
        /// </summary>
        public string AppName => m_AppName;

        [Header(LanguagAttribute.AppPrefix)]
        [SerializeField]
        private string m_AppPrefix = "EF_";
        /// <summary>
        /// 保存内容时的前缀
        /// </summary>
        public string AppPrefix => m_AppPrefix;

        [Header(LanguagAttribute.AppVersion)]
        [SerializeField]
        private string m_AppVersion = "1.0";
        /// <summary>
        /// 应用版本
        /// </summary>
        public string AppVersion => m_AppVersion;

        [Header(LanguagAttribute.AppStage)]
        [SerializeField]
        private AppStageEnum m_AppStage = AppStageEnum.Debug;
        /// <summary>
        /// 开发阶段
        /// </summary>
        public AppStageEnum AppStage => m_AppStage;


        [Header(LanguagAttribute.UIPrefabsPath)]
        [SerializeField]
        private string m_UIPath= "Prefabs/UI/";
        public string UIPrefabsPath => m_UIPath;

        [Header(LanguagAttribute.AudioPath)]
        [SerializeField]
        private string m_AudioPath = "Sources/";
        public string AudioPath => m_AudioPath;

        [Header(LanguagAttribute.ManagerLevel)]
        [SerializeField]
        private List<string> m_ManagerLevel = new List<string>()
        {
            "TimeManager",
            "HttpManager",
            "WebSocketManager",
            "LoadManager",
            "ToolManager",
            "SceneManager",
            "ObjectToolManager",
            "AudioManager",
            "FolderManager",
            "UIManager",
        };
        public List<string> ManagerLevels => m_ManagerLevel;
    }
}
