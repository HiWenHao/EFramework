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
        そうか,
    }
    /// <summary>
    /// The language config in editor panel.
    /// 编辑器面板下的语言配置
    /// </summary>
    public static class LC
    {
        static LanguageBase m_English;
        static LanguageBase m_Chinese;

        static bool init;
        public static ELanguage LanguageType => (ELanguage)ProjectUtility.Project.LanguageIndex;
        public static LanguageBase Language
        {
            get
            {
                switch (LanguageType)
                {
                    case ELanguage.English:
                        m_English ??= new English();
                        return m_English;
                    case ELanguage.中文:
                        m_Chinese ??= new Chinese();
                        return m_Chinese;
                    case ELanguage.そうか:
                    default:
                        if (!init)
                        {
                            init = true;
                            ExcelTool.ExcelDataManager.Init("Config/LC");
                            EDC_LC.CacheData();
                            foreach (var id in EDC_LC.Ids)
                            {
                                EDC_LC _LC = EDC_LC.Get(id);
                                D.Correct($"_LC.Lc = {_LC.Lc}");
                                D.Warning($"_LC.Lc1 = {_LC.Lc1}");
                            }
                        }
                        break;
                }
                m_English ??= new English();
                return m_English;
            }
        }
    }

    public abstract class LanguageBase
    {
        #region Common
        #region Path
        /// <summary>
        /// 路径选择
        /// </summary>
        public abstract string PathSelect { get; }
        /// <summary>
        /// 默认路径
        /// </summary>
        public abstract string PathDefault { get; }
        /// <summary>
        /// 路径选择错误标题
        /// </summary>
        public abstract string PathSelecteError { get; }
        /// <summary>
        /// 路径选择错误描述
        /// </summary>
        public abstract string PathSelecteErrorContent { get; }
        #endregion

        /// <summary>
        /// 确认按钮
        /// </summary>
        public abstract string Ok { get; }
        /// <summary>
        /// 取消按钮
        /// </summary>
        public abstract string Cancel { get; }
        /// <summary>
        /// 确认删除
        /// </summary>
        public abstract string ConfirmDelete { get; }
        /// <summary>
        /// 清除全部
        /// </summary>
        public abstract string ClearAll { get; }
        /// <summary>
        /// 删除全部
        /// </summary>
        public abstract string DeleteAll { get; }
        /// <summary>
        /// 提示
        /// </summary>
        public abstract string Hints { get; }
        /// <summary>
        /// 覆写
        /// </summary>
        public abstract string Overwrite { get; }
        /// <summary>
        /// 默认设置
        /// </summary>
        public abstract string DefaultSetting { get; }
        #endregion

        #region Project Setting
        /// <summary>
        /// 编辑器语言
        /// </summary>
        public abstract string EditorLanguage { get; }
        /// <summary>
        /// 脚本作者名
        /// </summary>
        public abstract string ScriptAuthor { get; }
        /// <summary>
        /// 脚本版本号
        /// </summary>
        public abstract string ScriptVersion { get; }
        #endregion

        #region Path Config Setting 
        /// <summary>
        /// 框架路径
        /// </summary>
        public abstract string FrameworkPath { get; }
        /// <summary>
        /// Sublime文件路径
        /// </summary>
        public abstract string SublimePath { get; }
        /// <summary>
        /// Notepad文件路径
        /// </summary>
        public abstract string NotepadPath { get; }
        /// <summary>
        /// 图集保存路径
        /// </summary>
        public abstract string AtlasSavePath { get; }
        /// <summary>
        /// 动画提取路径
        /// </summary>
        public abstract string AnimatorExtractPath { get; }
        #endregion

        #region Auto Bind Setting
        /// <summary>
        /// 默认脚本保存路径
        /// </summary>
        public abstract string DefaultScriptSavePath { get; }
        /// <summary>
        /// 默认预制件保存路径
        /// </summary>
        public abstract string DefaultPrefabSavePath { get; }
        /// <summary>
        /// 组件规则设置
        /// </summary>
        public abstract string SetRulePrefixes { get; }
        /// <summary>
        /// 命名空间
        /// </summary>
        public abstract string ScriptNamespace { get; }
        /// <summary>
        /// 默认命名空间
        /// </summary>
        public abstract string DefaultScriptNamespace { get; }
        /// <summary>
        /// 类名
        /// </summary>
        public abstract string ScriptClassName { get; }
        /// <summary>
        /// 类名提示
        /// </summary>
        public abstract string ScriptClassNameTips { get; }
        /// <summary>
        /// 生成UI预制件
        /// </summary>
        public abstract string CreatedUIPrefabSavePath { get; }
        /// <summary>
        /// 不生成UI预制件
        /// </summary>
        public abstract string NoCreatedUIPrefabSavePath { get; }
        /// <summary>
        /// 自动绑定
        /// </summary>
        public abstract string AutoBindingComponents { get; }
        /// <summary>
        /// 生成
        /// </summary>
        public abstract string BindingGenerate { get; }
        /// <summary>
        /// 卸载绑定脚本
        /// </summary>
        public abstract string UnloadBindingScripts { get; }

        #endregion

        #region Sprite Collection
        public abstract string Atlas { get; }
        public abstract string AtlasPath { get; }
        public abstract string CreateAtlas { get; }
        public abstract string PackPreview { get; }
        public abstract string AtlasXButtonTips { get; }
        public abstract string AtlasDelButton { get; }
        public abstract string AtlasDelButtonTips { get; }
        public abstract string AtlasClearAllTips { get; }
        public abstract string AtlasDeleteAllTips { get; }
        public abstract string AtlasOverwriteTips { get; }
        public abstract string AtlasSelectFolder { get; }
        public abstract string AtlasExistInCollection { get; }
        public abstract string AtlasExistAlsoOverwrite { get; }
        #endregion
    }

    public class English : LanguageBase
    {
        #region Common
        public override string Ok => "Ok";
        public override string Cancel => "Cancel";
        public override string ConfirmDelete => "Confirm Deletion";
        public override string DeleteAll => "Delete All";
        public override string ClearAll => "Clear All";

        public override string Hints => "Hints";
        public override string Overwrite => "Overwrite";
        public override string DefaultSetting => "Default Setting";


        public override string PathSelect => "Select Path";
        public override string PathDefault => "Default Path";
        public override string PathSelecteError => "Path Selecte Error";
        public override string PathSelecteErrorContent => "Please configure the correct path.";

        #endregion

        #region Project Setting
        public override string EditorLanguage => "EF Editor Language";
        public override string ScriptAuthor => "Script Author Name";
        public override string ScriptVersion => "Script Version";

        #endregion

        #region Path Config Setting
        public override string FrameworkPath => "Framework Path:";
        public override string SublimePath => "Sublime Path";
        public override string NotepadPath => "Notepad Path";
        public override string AtlasSavePath => "Atlas Save Path";
        public override string AnimatorExtractPath => "Animator Extract Path";

        #endregion

        #region Auto Bind Setting
        public override string ScriptNamespace => "The Namespace:";
        public override string ScriptClassName => "The Class Name";
        public override string ScriptClassNameTips => "  (Same as object name)";
        public override string DefaultScriptNamespace => "Script Namespace:";
        public override string SetRulePrefixes => "Set Rule Prefixes";
        public override string DefaultScriptSavePath => "Default Script Save Path：";
        public override string DefaultPrefabSavePath => "Default UI Prefab Save Path：";
        public override string CreatedUIPrefabSavePath => "  Created UI prefab, if exist well be directly modified";
        public override string NoCreatedUIPrefabSavePath => "  The UI prefab save path:";
        public override string AutoBindingComponents => "  Auto Binding Components";
        public override string BindingGenerate => "Start Create";
        public override string UnloadBindingScripts => "  Unload scripts after the UI created";

        #endregion

        #region Sprite Collection
        public override string Atlas => "Atlas";
        public override string AtlasPath => "Atlas Path";
        public override string CreateAtlas => "Create Atlas";
        public override string PackPreview => "Pack Preview";
        public override string AtlasXButtonTips => "Remove the atlas in current collection";
        public override string AtlasDelButton => "Del";
        public override string AtlasDelButtonTips => "Delete the atlas in asset and Remove the atlas in collection";
        public override string AtlasClearAllTips => "Clear the all atlas in current collection";
        public override string AtlasDeleteAllTips => "Delete the all atlas in asset";
        public override string AtlasOverwriteTips => "Overwrite all atlas in current collection";
        public override string AtlasSelectFolder => "Please select the Atlas generation folder first";
        public override string AtlasExistInCollection => "Atlas already exists in the current collector. Check it!";
        public override string AtlasExistAlsoOverwrite => "Atlas already exists, whether to overwrite?";

        #endregion
    }

    public class Chinese : LanguageBase
    {
        #region Common
        public override string Ok => "好的";
        public override string Cancel => "取消";
        public override string ConfirmDelete => "确认删除";
        public override string DeleteAll => "删除全部";
        public override string ClearAll => "清除全部";

        public override string Hints => "提示";
        public override string Overwrite => "覆盖";
        public override string DefaultSetting => "默认设置";


        public override string PathSelect => "选择路径";
        public override string PathDefault => "默认路径";
        public override string PathSelecteError => "路径错误";
        public override string PathSelecteErrorContent => "请配置正确的路径";





        #endregion

        #region Project Setting
        public override string EditorLanguage => "EF框架面板语言";
        public override string ScriptAuthor => "脚本作者名";
        public override string ScriptVersion => "脚本版本号";
        #endregion

        #region Path Config Setting
        public override string FrameworkPath => "框架地址：";
        public override string SublimePath => "Sublime文件路径：";
        public override string NotepadPath => "Notepad++文件路径：";
        public override string AtlasSavePath => "图集保存路径：";
        public override string AnimatorExtractPath => "提取压缩后的动画保存路径：";

        #endregion

        #region Auto Bind Setting
        public override string SetRulePrefixes => "组件规则设置";
        public override string DefaultScriptNamespace => "脚本默认命名空间：";
        public override string DefaultScriptSavePath => "默认组件代码保存路径：";
        public override string DefaultPrefabSavePath => "默认UI预制件的保存路径：";
        public override string ScriptNamespace => "命名空间：";
        public override string ScriptClassName => "类型命名：";
        public override string ScriptClassNameTips => "  (与对象名相一致)";
        public override string CreatedUIPrefabSavePath => "  同时生成UI的预制件，如果已存在则会修改";
        public override string NoCreatedUIPrefabSavePath => "  UI预制件的保存路径：";
        public override string AutoBindingComponents => "自动绑定组件";
        public override string BindingGenerate => "确定生成";
        public override string UnloadBindingScripts => "  生成UI后卸载该脚本";
        #endregion

        #region Sprite Collection
        public override string Atlas => "图集";
        public override string AtlasPath => "图集路径";
        public override string CreateAtlas => "创建图集";
        public override string PackPreview => "预览图集";
        public override string AtlasXButtonTips => "从收集器中移除这个图集";
        public override string AtlasDelButton => "删除";
        public override string AtlasDelButtonTips => "删除这个图集，并从收集器中移除";
        public override string AtlasClearAllTips => "清空当前收集器中的全部图集";
        public override string AtlasDeleteAllTips => "从资产文件夹中删除当前收集器里的全部图集";
        public override string AtlasOverwriteTips => "覆写当前收集器里的全部图集";
        public override string AtlasSelectFolder => "请先选择图集生成文件夹";
        public override string AtlasExistInCollection => "当前收集器中已经存在图集，请检查！";
        public override string AtlasExistAlsoOverwrite => "  图集已存在，是否覆盖？";


        #endregion
    }
}
