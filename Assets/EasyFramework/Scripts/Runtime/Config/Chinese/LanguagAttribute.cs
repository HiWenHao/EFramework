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
    public struct LanguagAttribute
    {
        #region ProjectSetting - 项目设置
        public const string ScriptAuthor = "脚本作者名。";
        public const string ScriptVersion = "脚本版本号。";
        public const string LanguageIndex = "框架显示语言索引。";
        public const string RendererPipline = "框架渲染管线索引。";
        #endregion

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

        #region PathConfigSetting - 路径配置
        public const string FrameworkPath = "框架在工程下的路径。";
        public const string AtlasFolder = "图集在工程下的路径。";
        public const string ExtractPath = "动画压缩后在工程下的路径。";
        public const string UIPrefabPath = "UI预制件保存在工程下的路径。";
        public const string UICodePath = "UI脚本保存在工程下的路径。";
        public const string SublimePath = "Sublime在系统中的路径。";
        public const string NotepadPath = "Notepad在系统中的路径。";
        #endregion

        #region AutoBindSetting - UI自动绑定配置
        public const string Namespace = "默认命名空间。";
        public const string RulePrefixes = "组件的规则设置。";
        #endregion
    }
}
