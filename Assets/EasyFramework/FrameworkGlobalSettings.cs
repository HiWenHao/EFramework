/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-14 11:55:30
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-14 11:55:30
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;
using UnityEngine;

namespace EasyFramework.Framework
{
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
        [Tooltip("资源管理类型")]
        [SerializeField] private string m_ResAdminType = "Default";
        public string ResAdminType => m_ResAdminType;

        [Tooltip("资源管理编号")]
        [SerializeField] private string m_ResAdminCode = "0";
        public string ResAdminCode => m_ResAdminCode;

        [Tooltip("是否Copy构建的ab资源到上传资源目录")]
        [SerializeField] private bool m_WhetherCopyResToCommitPath = false;
        public bool WhetherCopyResToCommitPath => m_WhetherCopyResToCommitPath;

        [Tooltip("是否在构建资源的时候清理上传到服务端目录的老资源")]
        [SerializeField] private bool m_CleanCommitPathRes = true;
        public bool CleanCommitPathRes => m_CleanCommitPathRes;

        [Tooltip("服务器类型")]
        [SerializeField] private ServerTypeEnum m_ServerType = ServerTypeEnum.Intranet;
        public ServerTypeEnum ServerType => m_ServerType;

        [Tooltip("内网地址")][SerializeField]
        private string m_InnerResourceSourceUrl = "http://192.168.0.1:8080";
        public string InnerResourceSourceUrl => m_InnerResourceSourceUrl;
        [Tooltip("外网地址")][SerializeField]
        private string m_ExtraResourceSourceUrl = "http://192.168.0.1:8080";
        public string ExtraResourceSourceUrl => m_ExtraResourceSourceUrl;
        [Tooltip("正式地址")][SerializeField]
        private string m_FormalResourceSourceUrl = "http://192.168.0.1:8080";
        public string FormalResourceSourceUrl => m_FormalResourceSourceUrl;
    }

    /// <summary>
    /// 框架全局设置.
    /// </summary>
    [Serializable]
    public class FrameworkGlobalSettings
    {
        [SerializeField]
        [Tooltip("脚本作者名")]
        private string m_ScriptAuthor = "Default";
        public string ScriptAuthor => m_ScriptAuthor;

        [SerializeField]
        [Tooltip("脚本版本")]
        private string m_ScriptVersion = "0.1";
        public string ScriptVersion => m_ScriptVersion;

        [SerializeField] 
        [Tooltip("开发阶段")]
        private AppStageEnum m_AppStage = AppStageEnum.Debug;
        public AppStageEnum AppStage => m_AppStage;

        [Header("资源存放地")]
        [SerializeField]
        private ResourcesArea m_ResourcesArea;
        public ResourcesArea ResourcesArea => m_ResourcesArea;

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
}
