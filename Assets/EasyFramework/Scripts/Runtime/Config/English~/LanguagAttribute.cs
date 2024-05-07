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
    }
}
