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
        public const string ScriptAuthor = "The script author.";
        public const string ScriptVersion = "The script version.";
        public const string LanguageIndex = "The frame displays the language index.";
        public const string RendererPipline = "The framework renderer pipeline index.";

        #endregion

        #region AppConst - 项目常量
        public const string AppConst = "Project constant settings.";

        public const string AppName = "Current application name of the project.";
        public const string AppStage = "Current development stage of the project.";
        public const string AppVersion = "Current application version of the project.";
        public const string AppPrefix = "The prefix when the project localizes the content.";

        public const string AudioPath = "Audio path in the resource folder.";
        public const string UIPrefabsPath = "UI prefabs path in the resource folder.";
        
        public const string ManagerLevel = "From top to bottom, the more forward the update, the more backward the exit.";
        #endregion

        #region ResourcesArea - 资源存放地
        public const string ResourcesArea = "Where the out packet resource is stored.";

        public const string CopyResToCommit = "Copy the built AB resource to the uploaded resource directory.";
        public const string CleanCommitPathRes = "Whether to clean up old resources uploaded to the server directory when building resources.";
        
        public const string ServerType = "Server type.";
        public const string InnerUrl = "Intranet resource address.";
        public const string ExtraUrl = "Extranet resource address.";
        public const string FormalUrl = "Official resource address.";
        public const string StandbyUrl = "Standby resource server address.";

        #endregion

        #region PathConfigSetting - 路径配置
        public const string FrameworkPath = "The path of the framework under engineering.";
        public const string AtlasFolder = "The path of the atlas under Engineering.";
        public const string ExtractPath = "Animation path under project after compression.";
        public const string UIPrefabPath = "The path where the UI prefab is saved under the project.";
        public const string UICodePath = "The path where the UI scripts will be saved under the project.";
        
        public const string SublimePath = "Sublime's path on your system.";
        public const string NotepadPath = "Notepad's path on your system.";
        #endregion

        #region AutoBindSetting - UI自动绑定配置
        public const string Namespace = "Default namespace.";
        public const string RulePrefixes = "Component rule Settings.";
        #endregion
    }
}
