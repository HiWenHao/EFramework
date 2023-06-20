/* 
 * ================================================
 * Describe:      This script is used to set the editor panel language.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-28 16:14:49
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-28 16:14:49
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace EasyFramework.Edit
{
    public enum ELanguage
    {
        English,
        中文,
    }
    /// <summary>
    /// The language config in editor panel.
    /// 编辑器面板下的语言配置
    /// </summary>
    public static class LC
    {
        static bool m_init;
        static int m_currentIndex = -1;
        static LanguageBase m_language;

        public static LanguageBase Language
        {
            get
            {
                if (!m_init)
                {
                    m_init = true;
                    m_language = new LanguageBase();
                    ExcelTool.ExcelDataManager.Init("Config/LC");
                    EDC_Config.CacheData();
                }
                if (m_currentIndex != ProjectUtility.Project.LanguageIndex)
                {
                    m_currentIndex = ProjectUtility.Project.LanguageIndex;
                    Initialize(ProjectUtility.Project.LanguageIndex);
                }
                return m_language;
            }
        }

        static void Initialize(int index)
        {
            #region Common
            m_language.Ok = EDC_Config.Get("Ok").ShowName[index];
            m_language.Cancel = EDC_Config.Get("Cancel").ShowName[index];
            m_language.ConfirmDelete = EDC_Config.Get("ConfirmDelete").ShowName[index];
            m_language.DeleteAll = EDC_Config.Get("DeleteAll").ShowName[index];
            m_language.ClearAll = EDC_Config.Get("ClearAll").ShowName[index];
            m_language.Hints = EDC_Config.Get("Hints").ShowName[index];
            m_language.Overwrite = EDC_Config.Get("Overwrite").ShowName[index];
            m_language.DefaultSetting = EDC_Config.Get("DefaultSetting").ShowName[index];

            m_language.PathSelect = EDC_Config.Get("PathSelect").ShowName[index];
            m_language.PathDefault = EDC_Config.Get("PathDefault").ShowName[index];
            m_language.PathSelecteError = EDC_Config.Get("PathSelecteError").ShowName[index];
            m_language.PathSelecteErrorContent = EDC_Config.Get("PathSelecteErrorContent").ShowName[index];

            #endregion

            #region Project Setting
            m_language.EditorLanguage = EDC_Config.Get("EditorLanguage").ShowName[index];
            m_language.ScriptAuthor = EDC_Config.Get("ScriptAuthor").ShowName[index];
            m_language.ScriptVersion = EDC_Config.Get("ScriptVersion").ShowName[index];

            #endregion

            #region Path Config Setting
            m_language.UnderProjectPath = EDC_Config.Get("UnderProjectPath").ShowName[index];
            m_language.NonProjectPath = EDC_Config.Get("NonProjectPath").ShowName[index];
            m_language.FrameworkPath = EDC_Config.Get("FrameworkPath").ShowName[index];
            m_language.SublimePath = EDC_Config.Get("SublimePath").ShowName[index];
            m_language.NotepadPath = EDC_Config.Get("NotepadPath").ShowName[index];
            m_language.AtlasSavePath = EDC_Config.Get("AtlasSavePath").ShowName[index];
            m_language.DefaultUIPrefabSavePath = EDC_Config.Get("DefaultUIPrefabSavePath").ShowName[index];
            m_language.DefaultUICodeSavePath = EDC_Config.Get("DefaultUICodeSavePath").ShowName[index];
            m_language.AnimatorExtractPath = EDC_Config.Get("AnimatorExtractPath").ShowName[index];

            #endregion

            #region Auto Bind Setting
            m_language.SetRulePrefixes = EDC_Config.Get("SetRulePrefixes").ShowName[index];
            m_language.ScriptNamespace = EDC_Config.Get("ScriptNamespace").ShowName[index];
            m_language.DefaultScriptNamespace = EDC_Config.Get("DefaultScriptNamespace").ShowName[index];
            m_language.ScriptClassName = EDC_Config.Get("ScriptClassName").ShowName[index];
            m_language.ScriptClassNameTips = EDC_Config.Get("ScriptClassNameTips").ShowName[index];
            m_language.CreatedUIPrefabSavePath = EDC_Config.Get("CreatedUIPrefabSavePath").ShowName[index];
            m_language.NoCreatedUIPrefabSavePath = EDC_Config.Get("NoCreatedUIPrefabSavePath").ShowName[index];
            m_language.AutoBindingComponents = EDC_Config.Get("AutoBindingComponents").ShowName[index];
            m_language.BindingGenerate = EDC_Config.Get("BindingGenerate").ShowName[index];
            m_language.UnloadBindingScripts = EDC_Config.Get("UnloadBindingScripts").ShowName[index];

            #endregion

            #region Sprite Collection
            m_language.Atlas = EDC_Config.Get("Atlas").ShowName[index];
            m_language.AtlasPath = EDC_Config.Get("AtlasPath").ShowName[index];
            m_language.CreateAtlas = EDC_Config.Get("CreateAtlas").ShowName[index];
            m_language.PackPreview = EDC_Config.Get("PackPreview").ShowName[index];
            m_language.AtlasXButtonTips = EDC_Config.Get("AtlasXButtonTips").ShowName[index];
            m_language.AtlasDelButton = EDC_Config.Get("AtlasDelButton").ShowName[index];
            m_language.AtlasDelButtonTips = EDC_Config.Get("AtlasDelButtonTips").ShowName[index];
            m_language.AtlasClearAllTips = EDC_Config.Get("AtlasClearAllTips").ShowName[index];
            m_language.AtlasDeleteAllTips = EDC_Config.Get("AtlasDeleteAllTips").ShowName[index];
            m_language.AtlasOverwriteTips = EDC_Config.Get("AtlasOverwriteTips").ShowName[index];
            m_language.AtlasSelectFolder = EDC_Config.Get("AtlasSelectFolder").ShowName[index];
            m_language.AtlasExistInCollection = EDC_Config.Get("AtlasExistInCollection").ShowName[index];
            m_language.AtlasExistAlsoOverwrite = EDC_Config.Get("AtlasExistAlsoOverwrite").ShowName[index];
            
            #endregion

        }
    }

    public class LanguageBase
    {
        #region Common
        #region Path
        /// <summary> 路径选择 </summary>
        public string PathSelect { get; set; }
        /// <summary> 默认路径 </summary>
        public string PathDefault { get; set; }
        /// <summary> 路径选择错误标题 </summary>
        public string PathSelecteError { get; set; }
        /// <summary> 路径选择错误描述 </summary>
        public string PathSelecteErrorContent { get; set; }
        #endregion

        /// <summary> 确认按钮 </summary>
        public string Ok { get; set; }
        /// <summary> 取消按钮 </summary>
        public string Cancel { get; set; }
        /// <summary> 确认删除 </summary>
        public string ConfirmDelete { get; set; }
        /// <summary> 清除全部 </summary>
        public string ClearAll { get; set; }
        /// <summary> 删除全部 </summary>
        public string DeleteAll { get; set; }
        /// <summary> 提示 </summary>
        public string Hints { get; set; }
        /// <summary> 覆写 </summary>
        public string Overwrite { get; set; }
        /// <summary> 默认设置 </summary>
        public string DefaultSetting { get; set; }
        #endregion

        #region Project Setting
        /// <summary> 编辑器语言 </summary>
        public string EditorLanguage { get; set; }
        /// <summary> 脚本作者名 </summary>
        public string ScriptAuthor { get; set; }
        /// <summary> 脚本版本号 </summary>
        public string ScriptVersion { get; set; }
        #endregion

        #region Path Config Setting
        /// <summary> 在项目路径下 </summary>
        public string UnderProjectPath { get; set; }
        /// <summary> 非项目路径下 </summary>
        public string NonProjectPath { get; set; }
        /// <summary> 框架路径 </summary>
        public string FrameworkPath { get; set; }
        /// <summary> Sublime文件路径 </summary>
        public string SublimePath { get; set; }
        /// <summary> Notepad文件路径 </summary>
        public string NotepadPath { get; set; }
        /// <summary> 图集保存路径 </summary>
        public string AtlasSavePath { get; set; }
        /// <summary> 默认UI预制件路径 </summary>
        public string DefaultUIPrefabSavePath { get; set; }
        /// <summary> 默认UI代码路径 </summary>
        public string DefaultUICodeSavePath { get; set; }
        /// <summary> 动画提取路径 </summary>
        public string AnimatorExtractPath { get; set; }
        #endregion

        #region Auto Bind Setting
        /// <summary> 组件规则设置 </summary>
        public string SetRulePrefixes { get; set; }
        /// <summary> 命名空间 </summary>
        public string ScriptNamespace { get; set; }
        /// <summary> 默认命名空间 </summary>
        public string DefaultScriptNamespace { get; set; }
        /// <summary> 类名 </summary>
        public string ScriptClassName { get; set; }
        /// <summary> 类名提示 </summary>
        public string ScriptClassNameTips { get; set; }
        /// <summary> 生成UI预制件 </summary>
        public string CreatedUIPrefabSavePath { get; set; }
        /// <summary> 不生成UI预制件 </summary>
        public string NoCreatedUIPrefabSavePath { get; set; }
        /// <summary> 自动绑定 </summary>
        public string AutoBindingComponents { get; set; }
        /// <summary> 生成 </summary>
        public string BindingGenerate { get; set; }
        /// <summary> 卸载绑定脚本 </summary>
        public string UnloadBindingScripts { get; set; }

        #endregion

        #region Sprite Collection
        /// <summary> 图集 </summary>
        public string Atlas { get; set; }
        /// <summary> 图集路径 </summary>
        public string AtlasPath { get; set; }
        /// <summary> 创建图集 </summary>
        public string CreateAtlas { get; set; }
        /// <summary> 预览图集 </summary>
        public string PackPreview { get; set; }
        /// <summary> 从收集器中移除这个图集 </summary>
        public string AtlasXButtonTips { get; set; }
        /// <summary> 删除 </summary>
        public string AtlasDelButton { get; set; }
        /// <summary> 删除这个图集，并从收集器中移除 </summary>
        public string AtlasDelButtonTips { get; set; }
        /// <summary> 清空当前收集器中的全部图集 </summary>
        public string AtlasClearAllTips { get; set; }
        /// <summary> 从资产文件夹中删除当前收集器里的全部图集 </summary>
        public string AtlasDeleteAllTips { get; set; }
        /// <summary> 覆写当前收集器里的全部图集 </summary>
        public string AtlasOverwriteTips { get; set; }
        /// <summary> 请先选择图集生成文件夹 </summary>
        public string AtlasSelectFolder { get; set; }
        /// <summary> 当前收集器中已经存在图集，请检查！ </summary>
        public string AtlasExistInCollection { get; set; }
        /// <summary> 图集已存在，是否覆盖？ </summary>
        public string AtlasExistAlsoOverwrite { get; set; }
        #endregion
    }
}
