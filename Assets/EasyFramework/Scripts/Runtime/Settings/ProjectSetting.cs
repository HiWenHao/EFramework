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

        [Header("出包资源存放地")]
        [SerializeField]
        private ResourcesArea m_ResourcesArea;
        public ResourcesArea ResourcesArea => m_ResourcesArea;

        [Header("项目常量")]
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
        [Header("Copy the built AB resource to the uploaded resource directory")]
        [Tooltip("复制构建的AB资源到上传资源目录")]
        [SerializeField] private bool m_CopyResToCommit = false;
        public bool CopyResToCommit => m_CopyResToCommit;

        [Header("Whether to clean up old resources uploaded to the server directory when building resources")]
        [Tooltip("是否在构建资源的时候清理上传到服务端目录的老资源")]
        [SerializeField] private bool m_CleanCommitRes = true;
        public bool CleanCommitPathRes => m_CleanCommitRes;

        [Header("服务器类型")]
        [SerializeField] private ServerTypeEnum m_ServerType = ServerTypeEnum.Intranet;
        public ServerTypeEnum ServerType => m_ServerType;

        [Header("内网资源地址")]
        [SerializeField]
        private string m_InnerUrl = "http://192.168.0.1:8080";
        /// <summary> 内网资源地址 </summary>
        public string InnerUrl => m_InnerUrl;

        [Header("外网资源地址")]
        [SerializeField]
        private string m_ExtraUrl = "http://192.168.0.1:8080";
        /// <summary> 外网资源地址 </summary>
        public string ExtraUrl => m_ExtraUrl;

        [Header("正式资源地址")]
        [SerializeField]
        private string m_FormalUrl = "http://192.168.0.1:8080";
        /// <summary> 正式资源地址 </summary>
        public string FormalUrl => m_FormalUrl;

        [Header("备用服务器")]
        [SerializeField]
        private string m_StandbyUrl = "http://192.168.0.1:8080";
        /// <summary> 备用资源地址 </summary>
        public string StandbyUrl => m_StandbyUrl;
    }

    /// <summary>
    /// 项目常量
    /// </summary>
    [Serializable]
    public class AppConstConfig
    {
        [Tooltip("应用名称")]
        [SerializeField]
        private string m_AppName = "EasyFramework";
        public string AppName => m_AppName;

        [Tooltip("保存内容时的前缀")]
        [SerializeField]
        private string m_AppPrefix = "EF_";
        public string AppPrefix => m_AppPrefix;

        [Tooltip("应用版本")]
        [SerializeField]
        private string m_AppVersion = "1.0";
        public string AppVersion => m_AppVersion;

        [Tooltip("开发阶段")]
        [SerializeField]
        private AppStageEnum m_AppStage = AppStageEnum.Debug;
        public AppStageEnum AppStage => m_AppStage;


        [Header("UI prefabs path in the resource folder.//  Resources 下存放 UI面板 的路径")]
        [SerializeField]
        private string m_UIPath= "Prefabs/UI/";
        public string UIPath => m_UIPath;

        [Header("Audio path in the resource folder.//  Resources 下存放 AudioClip 的路径")]
        [SerializeField]
        private string m_AudioPath = "Sources/";
        public string AudioPath => m_AudioPath;

        [Header("From top to bottom, the more forward the update, the more backward the exit.")]
        [Tooltip("自上而下，更新越靠前，退出越靠后")]
        [SerializeField]
        private List<string> m_ManagerLevel = new List<string>()
        {
            "TimeManager",
            "HttpManager",
            "SocketManager",
            "LoadManager",
            "ToolManager",
            "SceneManager",
            "ObjectToolManager",
            "SourceManager",
            "FolderManager",
            "UIManager",
        };
        public List<string> ManagerLevels => m_ManagerLevel;
    }
}
