/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-04-30 17:15:02
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-04-30 17:15:02
 * ScriptVersion: 0.1
 * ===============================================
*/
namespace EasyFramework.Edit
{
    internal struct LanguagAttribute
    {
        #region AppConst - 项目常量
        public const string AppConst = "项目常量设置。";

        public const string AppName = "项目当前应用名称。";
        public const string AppStage = "项目当前开发阶段。";
        public const string AppVersion = "项目当前应用版本。";
        public const string AppPrefix = "项目本地化保存内容时的前缀。";

        public const string AudioPath = "Resources文件夹下存放 音频文件 的路径。";
        public const string UIPrefabsPath = "Resources文件夹下存放 UI面板预制件 的路径。";

        public const string ManagerLevel = "自上而下，更新越靠前，退出越靠后。";
        #endregion

        #region ResourcesArea - 资源存放地
        public const string ResourcesArea = "出包资源存放地。";

        public const string CopyResToCommit = "复制构建的AB资源到上传资源目录。";
        public const string CleanCommitPathRes = "是否在构建资源的时候清理上传到服务端目录的老资源。";
        
        public const string ServerType = "服务器类型。";
        public const string InnerUrl = "内网资源地址。";
        public const string ExtraUrl = "外网资源地址。";
        public const string FormalUrl = "正式资源地址。";
        public const string StandbyUrl = "备用服务器。";

        #endregion

    }
}
