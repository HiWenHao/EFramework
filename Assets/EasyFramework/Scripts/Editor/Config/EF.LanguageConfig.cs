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

using System;
using System.IO;
using UnityEditor;

namespace EasyFramework.Edit
{
    public enum ELanguage
    {
        English,
        中文,
    }
    /// <summary>
    /// The language config in editor panel.
    /// <para>编辑器面板下的语言配置</para>
    /// </summary>
    public static class LC
    {
        static bool m_init;
        static int m_currentIndex = -1;
        static ILanguageBase[] m_languages;

        public static ILanguageBase Language
        {
            get
            {
                if (!m_init)
                {
                    m_init = true;
                    m_languages = new ILanguageBase[]
                    {
                        new English(),
                        new Chinese(),
                    };
                    m_currentIndex = UnityEngine.PlayerPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
                }

                if (m_currentIndex != ProjectUtility.Project.LanguageIndex)
                {
                    if (ProjectUtility.Project.LanguageIndex >= 0 && ProjectUtility.Project.LanguageIndex <= 1)
                    {
                        ChangeLanguage(m_currentIndex, ProjectUtility.Project.LanguageIndex);
                        m_currentIndex = ProjectUtility.Project.LanguageIndex;
                    }
                }

                return m_languages[m_currentIndex];
            }
        }

        #region ChangeLanguage
        /*
         * Change the relevant description language under the Settings panel.
         * 改变设置面板下的相关说明语言
         */
        static void ChangeLanguage(int nowIndex, int nextIndex)
        {
            string _lcPath = Path.Combine(ProjectUtility.Path.FrameworkPath[7..], "Scripts/Runtime/Config/");
            string _path = Path.Combine(UnityEngine.Application.dataPath, _lcPath);
            string _nameNow = GetNameWithIndex(nowIndex);
            string _nameNext = GetNameWithIndex(nextIndex);

            try
            {
                File.Delete(Path.Combine(_path, $"{_nameNow}/LanguagAttribute.cs"));
                File.Delete(Path.Combine(_path, $"{_nameNow}/LanguagAttribute.cs.meta"));
                File.Copy(Path.Combine(_path, $"{_nameNext}~/LanguagAttribute.cs"), Path.Combine(_path, $"{_nameNext}/LanguagAttribute.cs"));
            }
            catch (Exception ex)
            {
                D.Exception(ex.Message);
            }
            AssetDatabase.Refresh();
        }
        static string GetNameWithIndex(int index)
        {
            return index switch
            {
                1 => "Chinese",
                _ => "English",
            };
        }
        #endregion
    }

    public interface ILanguageBase
    {
        #region Common
        #region Path
        /// <summary> 路径选择 </summary>
        public string PathSelect { get; }
        /// <summary> 默认路径 </summary>
        public string PathDefault { get; }
        /// <summary> 路径选择错误标题 </summary>
        public string PathSelecteError { get; }
        /// <summary> 路径选择错误描述 </summary>
        public string PathSelecteErrorContent { get; }
        #endregion

        /// <summary> 确认按钮 </summary>
        public string Ok { get; }
        /// <summary> 取消按钮 </summary>
        public string Cancel { get; }
        /// <summary> 确认删除 </summary>
        public string ConfirmDelete { get; }
        /// <summary> 清除全部 </summary>
        public string ClearAll { get; }
        /// <summary> 删除全部 </summary>
        public string DeleteAll { get; }
        /// <summary> 提示 </summary>
        public string Hints { get; }
        /// <summary> 覆写 </summary>
        public string Overwrite { get; }
        /// <summary> 默认设置 </summary>
        public string DefaultSetting { get; }
        /// <summary> 错误 </summary>
        public string Error { get; }
        /// <summary> 缺少相关资源包 </summary>
        public string ResourcePackageAbsent { get; }
        /// <summary> 导入 </summary>
        public string Import { get; }
        /// <summary> 查找 </summary>
        public string Find { get; }
        /// <summary> 结束 </summary>
        public string End { get; }
        /// <summary> 丢失 </summary>
        public string Lost { get; }
        /// <summary> 对比 </summary>
        public string Compare { get; }
        #endregion

        #region SystemInfo
        /// <summary> 操作系统 </summary>
        public string OperatingSystem { get; }
        /// <summary> 系统内存 </summary>
        public string SystemMemorySize { get; }
        /// <summary> 处理器 </summary>
        public string ProcessorType { get; }
        /// <summary> 处理器数量 </summary>
        public string ProcessorCount { get; }

        /// <summary> 显卡 </summary>
        public string GraphicsDeviceName { get; }
        /// <summary> 显卡类型 </summary>
        public string GraphicsDeviceType { get; }
        /// <summary> 显存 </summary>
        public string GraphicsMemorySize { get; }
        /// <summary> 显卡标识 </summary>
        public string GraphicsDeviceID { get; }
        /// <summary> 显卡供应商 </summary>
        public string GraphicsDeviceVendor { get; }
        /// <summary> 显卡供应商标识码 </summary>
        public string GraphicsDeviceVendorID { get; }

        /// <summary> 设备模式 </summary>
        public string DeviceModel { get; }
        /// <summary> 设备名称 </summary>
        public string DeviceName { get; }
        /// <summary> 设备类型 </summary>
        public string DeviceType { get; }
        /// <summary> 设备标识 </summary>
        public string DeviceUniqueIdentifier { get; }

        /// <summary> DPI </summary>
        public string ScreenDpi { get; }
        /// <summary> 分辨率 </summary>
        public string ScreenCurrentResolution { get; }
        #endregion

        #region Project Setting
        /// <summary> 编辑器语言 </summary>
        public string EditorLanguage { get; }
        /// <summary> 编辑器用户名 </summary>
        public string EditorUser {  get; }
        /// <summary> 脚本作者名 </summary>
        public string ScriptAuthor { get; }
        /// <summary> 脚本版本号 </summary>
        public string ScriptVersion { get; }
        /// <summary> 渲染类型 </summary>
        public string RenderingType { get; }
        #endregion

        #region Path Config Setting
        /// <summary> 在项目路径下 </summary>
        public string UnderProjectPath { get; }
        /// <summary> 非项目路径下 </summary>
        public string NonProjectPath { get; }
        /// <summary> 框架路径 </summary>
        public string FrameworkPath { get; }
        /// <summary> Sublime文件路径 </summary>
        public string SublimePath { get; }
        /// <summary> Notepad文件路径 </summary>
        public string NotepadPath { get; }
        /// <summary> 图集保存路径 </summary>
        public string AtlasSavePath { get; }
        /// <summary> 默认UI预制件路径 </summary>
        public string DefaultUIPrefabSavePath { get; }
        /// <summary> 默认UI代码路径 </summary>
        public string DefaultUICodeSavePath { get; }
        /// <summary> 动画提取路径 </summary>
        public string AnimatorExtractPath { get; }
        #endregion

        #region Auto Bind Setting
        /// <summary> 组件规则设置 </summary>
        public string SetRulePrefixes { get; }
        /// <summary> 命名空间 </summary>
        public string ScriptNamespace { get; }
        /// <summary> 默认命名空间 </summary>
        public string DefaultScriptNamespace { get; }
        /// <summary> 类名 </summary>
        public string ScriptClassName { get; }
        /// <summary> 类名提示 </summary>
        public string ScriptClassNameTips { get; }
        /// <summary> 生成UI预制件 </summary>
        public string CreatedUIPrefabSavePath { get; }
        /// <summary> 不生成UI预制件 </summary>
        public string NoCreatedUIPrefabSavePath { get; }
        /// <summary> 自动绑定 </summary>
        public string AutoBindingComponents { get; }
        /// <summary> 生成 </summary>
        public string BindingGenerate { get; }
        /// <summary> 卸载绑定脚本 </summary>
        public string UnloadBindingScripts { get; }
        /// <summary> 按类型排序 </summary>
        public string SortByType {  get; }
        /// <summary> 按名字长度排序 </summary>
        public string SortByNameLength {  get; }

        #endregion

        #region Sprite Collection
        /// <summary> 图集 </summary>
        public string Atlas { get; }
        /// <summary> 图集路径 </summary>
        public string AtlasPath { get; }
        /// <summary> 创建图集 </summary>
        public string CreateAtlas { get; }
        /// <summary> 预览图集 </summary>
        public string PackPreview { get; }
        /// <summary> 从收集器中移除这个图集 </summary>
        public string AtlasXButtonTips { get; }
        /// <summary> 删除 </summary>
        public string AtlasDelButton { get; }
        /// <summary> 删除这个图集，并从收集器中移除 </summary>
        public string AtlasDelButtonTips { get; }
        /// <summary> 清空当前收集器中的全部图集 </summary>
        public string AtlasClearAllTips { get; }
        /// <summary> 从资产文件夹中删除当前收集器里的全部图集 </summary>
        public string AtlasDeleteAllTips { get; }
        /// <summary> 覆写当前收集器里的全部图集 </summary>
        public string AtlasOverwriteTips { get; }
        /// <summary> 请先选择图集生成文件夹 </summary>
        public string AtlasSelectFolder { get; }
        /// <summary> 当前收集器中已经存在图集，请检查！ </summary>
        public string AtlasExistInCollection { get; }
        /// <summary> 图集已存在，是否覆盖？ </summary>
        public string AtlasExistAlsoOverwrite { get; }
        #endregion

        #region ScriptToolsWindow
        /// <summary> 脚本工具 - 标题 </summary>
        public string Stw_Title { get; }
        /// <summary> 脚本工具 - 选择查找类型 </summary>
        public string Stw_SelectionFindType { get; }
        /// <summary> 脚本工具 -  依赖该脚本的预制件 </summary>
        public string Stw_ScriptDependencies{ get; }
        /// <summary> 脚本工具 - 丢失脚本的对象 </summary>
        public string Stw_ScriptMissing{ get; }
        /// <summary> 脚本工具 - 选择查询脚本</summary>
        public string Stw_SelectionTargetScript { get; }
        /// <summary> 脚本工具 - 递归查找 </summary>
        public string Stw_RecurseDependencies{ get; }
        /// <summary> 脚本工具 - 查找脚本依赖项 </summary>
        public string Stw_FindDependencies{ get; }
        /// <summary> 脚本工具 - 依赖数量 </summary>
        public string Stw_DependenciesCount{ get; }
        /// <summary> 脚本工具 - 在所有活动场景中 </summary>
        public string Stw_InAllActivityScenarios{ get; }
        /// <summary> 脚本工具 - 在全部预制件上 </summary>
        public string Stw_OnAllPrefabs{ get; }
        /// <summary> 脚本工具 - 丢失数量 </summary>
        public string Stw_MissingCount{ get; }
        /// <summary> 脚本工具 - 未找到匹配项 </summary>
        public string Stw_NoMatchesFound { get; }
        /// <summary> 脚本工具 - 根 </summary>
        public string Stw_RootObject{ get; }
        /// <summary> 脚本工具 - 根对象 </summary>
        public string Stw_TargetRootObject { get; }
        /// <summary> 脚本工具 - 层数 </summary>
        public string Stw_TargetLayers { get; }


        #endregion

        #region PrefabsCompare
        /// <summary> 预制件对比 - 左边缺少该物体，或位置不同 </summary>
        public string Pc_MissObjectLeft { get; }
        /// <summary> 预制件对比 - 右边缺少该物体，或位置不同 </summary>
        public string Pc_MissObjectRight { get; }
        /// <summary> 预制件对比 - 左边内容缺失 </summary>
        public string Pc_MissContentsLeft { get; }
        /// <summary> 预制件对比 - 右边内容缺失 </summary>
        public string Pc_MissContentsRight { get; }
        /// <summary> 预制件对比 - 显示一致的 </summary>
        public string Pc_ShowEqual { get; }
        /// <summary> 预制件对比 - 显示单一的 </summary>
        public string Pc_ShowMiss { get; }
        
        #endregion
    }

    public struct English : ILanguageBase
    {
        public readonly string PathSelect => "Select Path";

        public readonly string PathDefault => "Default Path";

        public readonly string PathSelecteError => "Path Selecte Error";

        public readonly string PathSelecteErrorContent => "Please configure the correct path";

        public readonly string Ok => "Ok";

        public readonly string Cancel => "Cancel";

        public readonly string ConfirmDelete => "Confirm Deletion";

        public readonly string ClearAll => "Clear All";

        public readonly string DeleteAll => "Delete All";

        public readonly string Hints => "Hints";

        public readonly string Overwrite => "Overwrite";

        public readonly string DefaultSetting => "Default Setting";

        public readonly string Error => "Error";

        public readonly string ResourcePackageAbsent => "The related resource pack does not exist or is missing in the project. Import it first.";

        public readonly string Import => "Import";

        public readonly string Find => "Find";

        public readonly string End => "End";

        public readonly string Lost => "Lost";

        public readonly string Compare => "Compare";

        public readonly string OperatingSystem => "Operating system name with version: ";

        public readonly string SystemMemorySize => "Amount of system memory present: ";

        public readonly string ProcessorType => "Processor name: ";

        public readonly string ProcessorCount => "Number of processors present: ";

        public readonly string GraphicsDeviceName => "The name of the graphics device: ";

        public readonly string GraphicsDeviceType => "The graphics API type used by the graphics device: ";

        public readonly string GraphicsMemorySize => "Amount of video memory present: ";

        public readonly string GraphicsDeviceID => "The identifier code of the graphics device: ";

        public readonly string GraphicsDeviceVendor => "The vendor of the graphics device: ";

        public readonly string GraphicsDeviceVendorID => "The identifier code of the graphics device vendor: ";

        public readonly string DeviceModel => "The model of the device: ";

        public readonly string DeviceName => "The user defined name of the device: ";

        public readonly string DeviceType => "Returns the kind of device the application is running on: ";

        public readonly string DeviceUniqueIdentifier => "A unique device identifier: ";

        public readonly string ScreenDpi => "The current DPI of the device: ";

        public readonly string ScreenCurrentResolution => "The current screen resolution: ";

        public readonly string EditorLanguage => "EF Editor Language";

        public readonly string ScriptAuthor => "Script Author Name";

        public readonly string EditorUser => "Editor User";

        public readonly string ScriptVersion => "Script Version";

        public readonly string RenderingType => "Rendering pipeline type";

        public readonly string UnderProjectPath => "Under The Project Path";

        public readonly string NonProjectPath => "Non-Project Path";

        public readonly string FrameworkPath => "Framework Path:";

        public readonly string SublimePath => "Sublime Path:";

        public readonly string NotepadPath => "Notepad++ Path:";

        public readonly string AtlasSavePath => "Atlas Save Path:";

        public readonly string DefaultUIPrefabSavePath => "Default UI Prefab Save Path: ";

        public readonly string DefaultUICodeSavePath => "Default Script Save Path:";

        public readonly string AnimatorExtractPath => "Animator Extract Path:";

        public readonly string SetRulePrefixes => "Set Rule Prefixes";

        public readonly string ScriptNamespace => "The Namespace:";

        public readonly string DefaultScriptNamespace => "Script Namespace:";

        public readonly string ScriptClassName => "The Class Name";

        public readonly string ScriptClassNameTips => "  (Same as object name)";

        public readonly string CreatedUIPrefabSavePath => "  Created UI prefab.If exist well be directly modified";

        public readonly string NoCreatedUIPrefabSavePath => "  The UI prefab save path:";

        public readonly string AutoBindingComponents => "  Auto Binding Components";

        public readonly string BindingGenerate => "Start Create";

        public readonly string UnloadBindingScripts => "  Unload scripts after the UI created";

        public readonly string SortByType => "Sort by component type";

        public readonly string SortByNameLength => "Sort by name length";

        public readonly string Atlas => "Atlas";

        public readonly string AtlasPath => "Atlas Path";

        public readonly string CreateAtlas => "Create Atlas";

        public readonly string PackPreview => "Pack Preview";

        public readonly string AtlasXButtonTips => "Remove the atlas in current collection";

        public readonly string AtlasDelButton => "Del";

        public readonly string AtlasDelButtonTips => "Delete the atlas in asset and Remove the atlas in collection";

        public readonly string AtlasClearAllTips => "Clear the all atlas in current collection";

        public readonly string AtlasDeleteAllTips => "Delete the all atlas in asset";

        public readonly string AtlasOverwriteTips => "Overwrite all atlas in current collection";

        public readonly string AtlasSelectFolder => "Please select the Atlas generation folder first";

        public readonly string AtlasExistInCollection => "Atlas already exists in the current collector. Check it!";

        public readonly string AtlasExistAlsoOverwrite => "Atlas already exists. Whether to overwrite?";

        public readonly string Stw_Title => "Script Tools";

        public readonly string Stw_SelectionFindType => "Selection find type: ";

        public readonly string Stw_ScriptDependencies => "Prefab that relies on this script";

        public readonly string Stw_ScriptMissing => "Script Missing";

        public readonly string Stw_SelectionTargetScript => "Selection Target Script: ";

        public readonly string Stw_RecurseDependencies => "Recurse Dependencies.    (Warning: Very slow with too many resources)";

        public readonly string Stw_InAllActivityScenarios => "In All Activity Scenes";

        public readonly string Stw_OnAllPrefabs => "On All Prefabs";

        public readonly string Stw_FindDependencies => "Find Dependencies";

        public readonly string Stw_DependenciesCount => "Dependencies count: ";

        public readonly string Stw_MissingCount => "Missing count: ";

        public readonly string Stw_NoMatchesFound => "No matches found.";

        public readonly string Stw_RootObject => "Root";

        public readonly string Stw_TargetRootObject => "Target Root";

        public readonly string Stw_TargetLayers => "Layer";

        public readonly string Pc_MissObjectLeft => "The left object is missing, or in a different position.";

        public readonly string Pc_MissObjectRight => "The right object is missing, or in a different position";

        public readonly string Pc_MissContentsLeft => "The content on the left is missing";

        public readonly string Pc_MissContentsRight => "The content on the right is missing";

        public readonly string Pc_ShowEqual => "Display consistent";

        public readonly string Pc_ShowMiss => "Display a single";
    }

    public struct Chinese : ILanguageBase
    {
        public readonly string PathSelect => "选择路径";

        public readonly string PathDefault => "默认路径";

        public readonly string PathSelecteError => "路径选择错误";

        public readonly string PathSelecteErrorContent => "请配置正确路径";

        public readonly string Ok => "好的";

        public readonly string Cancel => "取消";

        public readonly string ConfirmDelete => "确认删除";

        public readonly string ClearAll => "清除全部";

        public readonly string DeleteAll => "删除全部";

        public readonly string Hints => "提示";

        public readonly string Overwrite => "覆写";

        public readonly string DefaultSetting => "默认设置";

        public readonly string Error => "错误";

        public readonly string ResourcePackageAbsent => "项目中不存在或缺少相关资源包，请先引入。";

        public readonly string Import => "导入";

        public readonly string Find => "查找";

        public readonly string End => "结束";

        public readonly string Lost => "丢失";

        public readonly string Compare => "对比";

        public readonly string OperatingSystem => "操作系统：";

        public readonly string SystemMemorySize => "系统内存：";

        public readonly string ProcessorType => "处理器：";

        public readonly string ProcessorCount => "处理器数量：";

        public readonly string GraphicsDeviceName => "显卡：";

        public readonly string GraphicsDeviceType => "显卡类型：";

        public readonly string GraphicsMemorySize => "显存：";

        public readonly string GraphicsDeviceID => "显卡标识：";

        public readonly string GraphicsDeviceVendor => "显卡供应商：";

        public readonly string GraphicsDeviceVendorID => "显卡供应商标识码：";

        public readonly string DeviceModel => "设备模式：";

        public readonly string DeviceName => "设备名称：";

        public readonly string DeviceType => "设备类型：";

        public readonly string DeviceUniqueIdentifier => "设备标识：";

        public readonly string ScreenDpi => "屏幕当前DPI：";

        public readonly string ScreenCurrentResolution => "分辨率：";

        public readonly string EditorLanguage => "EF框架面板语言";

        public readonly string ScriptAuthor => "脚本作者";

        public readonly string EditorUser => "编辑器用户";

        public readonly string ScriptVersion => "脚本版本号";

        public readonly string RenderingType => "渲染管线类型";

        public readonly string UnderProjectPath => "在项目路径下";

        public readonly string NonProjectPath => "非项目路径下";

        public readonly string FrameworkPath => "框架地址：";

        public readonly string SublimePath => "Sublime文件路径：";

        public readonly string NotepadPath => "Notepad++文件路径：";

        public readonly string AtlasSavePath => "图集保存路径：";

        public readonly string DefaultUIPrefabSavePath => "默认UI预制件的保存路径：";

        public readonly string DefaultUICodeSavePath => "默认UI代码保存路径：";

        public readonly string AnimatorExtractPath => "提取压缩后的动画保存路径：";

        public readonly string SetRulePrefixes => "组件规则设置";

        public readonly string ScriptNamespace => "命名空间：";

        public readonly string DefaultScriptNamespace => "脚本默认命名空间：";

        public readonly string ScriptClassName => "类型命名：";

        public readonly string ScriptClassNameTips => "   (与对象名相一致)";

        public readonly string CreatedUIPrefabSavePath => "  同时生成UI的预制件，如果已存在则会修改";

        public readonly string NoCreatedUIPrefabSavePath => "  UI预制件的保存路径：";

        public readonly string AutoBindingComponents => "  自动绑定组件";

        public readonly string BindingGenerate => "确定生成";

        public readonly string UnloadBindingScripts => "  生成UI后卸载该脚本";

        public readonly string SortByType => "按类型排序";

        public readonly string SortByNameLength => "按名字长度排序";

        public readonly string Atlas => "图集";

        public readonly string AtlasPath => "图集路径";

        public readonly string CreateAtlas => "创建图集";

        public readonly string PackPreview => "预览图集";

        public readonly string AtlasXButtonTips => "从收集器中移除这个图集";

        public readonly string AtlasDelButton => "删除";

        public readonly string AtlasDelButtonTips => "删除这个图集，并从收集器中移除";

        public readonly string AtlasClearAllTips => "清空当前收集器中的全部图集";

        public readonly string AtlasDeleteAllTips => "从资产文件夹中删除当前收集器里的全部图集";

        public readonly string AtlasOverwriteTips => "覆写当前收集器里的全部图集";

        public readonly string AtlasSelectFolder => "请先选择图集生成文件夹";

        public readonly string AtlasExistInCollection => "当前收集器中已经存在图集，请检查！";

        public readonly string AtlasExistAlsoOverwrite => "  图集已存在，是否覆盖？";
        
        public readonly string Stw_Title => "脚本工具";

        public readonly string Stw_SelectionFindType => "选择查找类型：";

        public readonly string Stw_ScriptDependencies => "依赖该脚本的预制件";

        public readonly string Stw_ScriptMissing => "丢失脚本的对象";

        public readonly string Stw_SelectionTargetScript => "选择目标脚本：";

        public readonly string Stw_RecurseDependencies => "递归查找, 资源过多时非常慢。";

        public readonly string Stw_InAllActivityScenarios => "在所有活动场景中";

        public readonly string Stw_OnAllPrefabs => "在全部预制件上";

        public readonly string Stw_FindDependencies => "查找脚本依赖项";

        public readonly string Stw_DependenciesCount => "依赖数量：";

        public readonly string Stw_MissingCount => "丢失数量：";

        public readonly string Stw_NoMatchesFound => "未找到匹配项。";

        public readonly string Stw_RootObject => "根";

        public readonly string Stw_TargetRootObject => "根目标";

        public readonly string Stw_TargetLayers => "层";

        public readonly string Pc_MissObjectLeft => "左边缺少该物体，或位置不同。";

        public readonly string Pc_MissObjectRight => "右边缺少该物体，或位置不同。";

        public readonly string Pc_MissContentsLeft => "左边缺失内容";

        public readonly string Pc_MissContentsRight => "右边缺失内容";

        public readonly string Pc_ShowEqual => "显示一致的";

        public readonly string Pc_ShowMiss => "显示单一的";
    }
}
