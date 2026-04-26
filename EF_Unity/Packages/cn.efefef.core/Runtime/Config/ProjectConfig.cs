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
using EasyFramework.Edit;
using UnityEngine;

namespace EasyFramework
{
    /// <summary>
    /// 框架设置界面
    /// </summary>
    public class ProjectConfig : ScriptableObject
    {
        [SerializeField, HeaderPro("脚本作者名", "The script author")]
        private string _scriptAuthor = "Default";

        public string ScriptAuthor => _scriptAuthor;

        [SerializeField, HeaderPro("脚本版本号", "The script version")]
        private string _scriptVersion = "0.1";

        public string ScriptVersion => _scriptVersion;

        [SerializeField, HeaderPro("出包资源存放地", "Where the out packet resource is stored")]
        private ResourcesArea _resourcesArea;

        public ResourcesArea ResourcesArea => _resourcesArea;

        [SerializeField, HeaderPro("项目常量设置", "Project constant settings")]
        private AppConstConfig _appConst;

        public AppConstConfig AppConst => _appConst;

        //[HeaderPro("项目常量")]
        //[SerializeField]
        //private AppConstConfig m_AppConst;
        //public AppConstConfig AppConst => m_AppConst;


        //[HeaderPro("Hotfix")]
        //[SerializeField]
        //private string m_ResourceVersionFileName = "ResourceVersion.txt";
        //public string ResourceVersionFileName { get { return m_ResourceVersionFileName; } }
        //public string WindowsAppUrl = "";
        //public string MacOSAppUrl = "";
        //public string IOSAppUrl = "";
        //public string AndroidAppUrl = "";
        //[HeaderPro("Server")]
        //[SerializeField]
        //private string m_CurUseServerChannel;
        //public string CurUseServerChannel => m_CurUseServerChannel;
        //[SerializeField]
        //private List<ServerChannelInfo> m_ServerChannelInfos;

        //public List<ServerChannelInfo> ServerChannelInfos
        //{
        //    get => m_ServerChannelInfos;
        //}

        //[HeaderPro("Config")]
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
        [SerializeField, HeaderPro("复制构建的AB资源到上传资源目录", "Copy the built AB resource to the uploaded resource directory")]
        private bool m_CopyResToCommit = false;

        public bool CopyResToCommit => m_CopyResToCommit;

        [SerializeField,
         HeaderPro("是否在构建资源的时候清理上传到服务端目录的老资源",
             "Whether to clean up old resources uploaded to the server directory when building resources")]
        private bool m_CleanCommitRes = true;

        public bool CleanCommitPathRes => m_CleanCommitRes;

        [SerializeField, HeaderPro("服务器类型", "Server type")]
        private ServerTypeEnum m_ServerType = ServerTypeEnum.Intranet;

        public ServerTypeEnum ServerType => m_ServerType;

        [SerializeField, HeaderPro("内网资源地址", "Intranet resource address")]
        private string m_InnerUrl = "http://127.0.0.1:8080";

        /// <summary> 内网资源地址 </summary>
        public string InnerUrl => m_InnerUrl;

        [SerializeField, HeaderPro("外网资源地址", "Extranet resource address")]
        private string m_ExtraUrl = "http://127.0.0.1:8080";

        /// <summary> 外网资源地址 </summary>
        public string ExtraUrl => m_ExtraUrl;

        [SerializeField, HeaderPro("正式资源地址", "Official resource address")]
        private string m_FormalUrl = "http://127.0.0.1:8080";

        /// <summary> 正式资源地址 </summary>
        public string FormalUrl => m_FormalUrl;

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
        public static int LanguageIndex = 0;

        [SerializeField, HeaderPro("项目当前应用名称", "Current application name of the project")]
        private string _appName = "EasyFramework";

        /// <summary>
        /// 应用名称
        /// </summary>
        public string AppName => _appName;

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

        [SerializeField, HeaderPro("项目当前开发阶段", "Current development stage of the project")]
        private AppStageEnum _appStage = AppStageEnum.Debug;

        /// <summary>
        /// 开发阶段
        /// </summary>
        public AppStageEnum AppStage => _appStage;


        [SerializeField, HeaderPro("Resources文件夹下存放 UI面板预制件 的路径", "UI prefabs path in the resource folder")]
        private string _uiPath = "Prefabs/UI/";

        public string UIPrefabsPath => _uiPath;

        [SerializeField, HeaderPro("Resources文件夹下存放 音频文件 的路径", "Audio path in the resource folder")]
        private string _audioPath = "Sources/";

        public string AudioPath => _audioPath;

        [SerializeField,
         HeaderPro("自上而下，更新越靠前，退出越靠后", "From top to bottom, the more forward the update, the more backward the exit")]
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