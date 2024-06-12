/* 
 * ================================================
 * Describe:      This script is used to set the editor panel language.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-28 16:14:49
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-07 17:12:32
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
        static int m_currentIndex;
        static ILanguageBase m_languages;
        static English m_English;
        static Chinese m_Chinese;

        public static ILanguageBase Language
        {
            get
            {
                if (!m_init)
                {
                    m_init = true;
                    m_currentIndex = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
                    m_languages = CreateLanguage(m_currentIndex);
                }

                if (m_currentIndex != ProjectUtility.Project.LanguageIndex)
                {
                    if (ProjectUtility.Project.LanguageIndex >= 0 && ProjectUtility.Project.LanguageIndex <= 1)
                    {
                        ChangeLanguage(m_currentIndex, ProjectUtility.Project.LanguageIndex);
                        m_currentIndex = ProjectUtility.Project.LanguageIndex;
                        m_languages = CreateLanguage(m_currentIndex);
                    }
                }

                return m_languages;
            }
        }

        #region Combine
        public static string Combine(string text1, string text2)
        {
            if (m_currentIndex == 1)
                return text1 + text2;
            else
                return $"{text1} {text2}";
        }
        public static string Combine(string text1, string text2, string text3)
        {
            if (m_currentIndex == 1)
                return text1 + text2 + text3;
            else
                return $"{text1} {text2} {text3}";
        }
        public static string Combine(string text1, string text2, string text3, string text4)
        {
            if (m_currentIndex == 1)
                return text1 + text2 + text3 + text4;
            else
                return $"{text1} {text2} {text3} {text4}";
        }
        public static string Combine(string text1, string text2, string text3, string text4, string text5)
        {
            if (m_currentIndex == 1)
                return text1 + text2 + text3 + text4 + text5;
            else
                return $"{text1} {text2} {text3} {text4} {text5}";
        }
        #endregion

        #region ChangeLanguage
        /*
         * Change the relevant description language.
         * 改变说明语言
         */
        static ILanguageBase CreateLanguage(int index)
        {
            switch (index)
            {
                case 0:
                    m_English = new English();
                    return m_English;
                case 1:
                    m_Chinese = new Chinese();
                    return m_Chinese;
                default:
                    D.Exception("There's a serious problem with the language system. Check it..!!!!!!!!!!!!!!!!!!!!");
                    break;
            }
            return null;
        }

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
        /// <summary> 向上 </summary> 
        public string Up { get; }
        //==========================================================================    3
        /// <summary> 添加 </summary>
        public string Add { get; }
        /// <summary> 结束 </summary>
        public string End { get; }
        /// <summary> 键 </summary>
        public string Key { get; }
        /// <summary> 确认按钮 </summary>
        public string Yes { get; }
        //==========================================================================    4
        /// <summary> 完成 </summary>
        public string Done { get; }
        /// <summary> 向下 </summary>
        public string Down { get; }
        /// <summary> 查找 </summary>
        public string Find { get; }
        /// <summary> 文件 </summary>
        public string File { get; }
        /// <summary> 丢失 </summary>
        public string Lost { get; }
        /// <summary> 加载 </summary>
        public string Load { get; }
        /// <summary> 列表 </summary>
        public string List { get; }
        /// <summary> 标记 </summary>
        public string Mark { get; }
        /// <summary> 名称 </summary>
        public string Name { get; }
        /// <summary> 加载 </summary>
        public string Open { get; }
        /// <summary> 保存 </summary>
        public string Save { get; }
        /// <summary> 尺寸 </summary>
        public string Size { get; }
        /// <summary> 任务 </summary>
        public string Task { get; }
        /// <summary> 类型 </summary>
        public string Type { get; }
        //==========================================================================    5
        /// <summary> 数量 </summary>
        public string Count { get; }
        /// <summary> 清空 </summary>
        public string Clear { get; }
        /// <summary> 颜色 </summary>
        public string Color { get; }
        /// <summary> 正在做 </summary>
        public string Doing { get; }
        /// <summary> 错误 </summary>
        public string Error { get; }
        /// <summary> 提示 </summary>
        public string Hints { get; }
        /// <summary> 模型 </summary>
        public string Model { get; }
        /// <summary> 查询 </summary>
        public string Query { get; }
        /// <summary> 重置 </summary>
        public string Reset { get; }
        /// <summary> 分数 </summary>
        public string Score { get; }
        /// <summary> 标题、题目 </summary>
        public string Title { get; }
        /// <summary> 值 </summary>
        public string Value { get; }
        /// <summary> 宽度 </summary>
        public string Width { get; }
        //==========================================================================    6
        /// <summary> 资产 </summary>
        public string Assets { get; }
        /// <summary> 配置 </summary>
        public string Config { get; }
        /// <summary> 取消按钮 </summary>
        public string Cancel { get; }
        /// <summary> 删除 </summary>
        public string Delete { get; }
        /// <summary> 高度 </summary>
        public string Height { get; }
        /// <summary> 稍等 </summary>
        public string Holdon { get; }
        /// <summary> 导入 </summary>
        public string Import { get; }
        /// <summary> 内存消耗 </summary>
        public string Memory { get; }
        /// <summary> 提升、上移 </summary>
        public string MoveUp { get; }
        /// <summary> 删除 </summary>
        public string Remove { get; }
        /// <summary> 顶点数 </summary>
        public string Vertex { get; }
        //==========================================================================    7
        /// <summary> 对齐至 </summary>
        public string AlignAt { get; }
        /// <summary> 放弃、遗弃 </summary>
        public string Abandon { get; }
        /// <summary> 对比 </summary>
        public string Compare { get; }
        /// <summary> 详情 </summary>
        public string Details { get; }
        /// <summary> 加密 </summary>
        public string Encrypt { get; }
        /// <summary> 特效 </summary>
        public string Effects { get; }
        /// <summary> 最大尺寸 </summary>
        public string MaxSize { get; }
        /// <summary> 下移、后退 </summary>
        public string MoveDown { get; }
        /// <summary> 刷新 </summary>
        public string Refresh { get; }
        /// <summary> 贴图、图片 </summary>
        public string Texture { get; }
        /// <summary> 超时 </summary>
        public string Timeout { get; }
        //==========================================================================    8
        /// <summary> 清除全部 </summary>
        public string ClearAll { get; }
        /// <summary> 绘制次数 </summary>
        public string DrawCall { get; }
        /// <summary> 筛选 </summary>
        public string Filtrate { get; }
        /// <summary> 总览 </summary>
        public string Overview { get; }
        /// <summary> 粒子 </summary>
        public string Particle { get; }
        /// <summary> 进展、进度、任务进度 </summary>
        public string Progress { get; }
        /// <summary> 设置 </summary>
        public string Settings { get; }
        //==========================================================================    9
        /// <summary> 骨骼数 </summary>
        public string BoneCount { get; }
        /// <summary> 目录 </summary>
        public string Catalogue { get; }
        /// <summary> 删除全部 </summary>
        public string DeleteAll { get; }
        /// <summary> 强调、突出 </summary>
        public string Highlight { get; }
        /// <summary> 删除全部 </summary>
        public string RemoveAll { get; }
        /// <summary> 强制保存 </summary>
        public string ForceSave { get; }
        /// <summary> 覆写 </summary>
        public string Overwrite { get; }
        //==========================================================================    10
        /// <summary> 三角面 </summary>
        public string Triangular { get; }
        //==========================================================================    11
        /// <summary> 压缩格式 </summary>
        public string Compression { get; }
        /// <summary> 描述 </summary>
        public string Description { get; }
        /// <summary> 更多选择 </summary>
        public string MoreOptions { get; }
        /// <summary> 预设、偏好 </summary>
        public string Preferences { get; }
        /// <summary> 信息 </summary>
        public string Information { get; }
        //==========================================================================    12
        /// <summary> 创建自定义 </summary>
        public string CreateCustom { get; }
        /// <summary> 资源类型 </summary>
        public string ResourceType { get; }
        //==========================================================================    13
        /// <summary> 确认删除 </summary>
        public string ConfirmDelete { get; }
        //==========================================================================    14
        /// <summary> 正在处理中 </summary>
        public string BeingProcessed { get; }
        /// <summary> 默认设置 </summary>
        public string DefaultSetting { get; }
        //==========================================================================    20
        /// <summary> 缺少相关资源包 </summary>
        public string ResourcePackageAbsent { get; }


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
        public string EditorUser { get; }
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
        public string SortByType { get; }
        /// <summary> 按名字长度排序 </summary>
        public string SortByNameLength { get; }

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
        public string Stw_ScriptDependencies { get; }
        /// <summary> 脚本工具 - 丢失脚本的对象 </summary>
        public string Stw_ScriptMissing { get; }
        /// <summary> 脚本工具 - 选择查询脚本</summary>
        public string Stw_SelectionTargetScript { get; }
        /// <summary> 脚本工具 - 递归查找 </summary>
        public string Stw_RecurseDependencies { get; }
        /// <summary> 脚本工具 - 查找脚本依赖项 </summary>
        public string Stw_FindDependencies { get; }
        /// <summary> 脚本工具 - 依赖数量 </summary>
        public string Stw_DependenciesCount { get; }
        /// <summary> 脚本工具 - 在所有活动场景中 </summary>
        public string Stw_InAllActivityScenarios { get; }
        /// <summary> 脚本工具 - 在全部预制件上 </summary>
        public string Stw_OnAllPrefabs { get; }
        /// <summary> 脚本工具 - 丢失数量 </summary>
        public string Stw_MissingCount { get; }
        /// <summary> 脚本工具 - 未找到匹配项 </summary>
        public string Stw_NoMatchesFound { get; }
        /// <summary> 脚本工具 - 根 </summary>
        public string Stw_RootObject { get; }
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

        #region PlayerPrefsEditor
        /// <summary> 存档数据 - 导入提示 </summary>
        public string Ppe_ImportHint { get; }
        /// <summary> 存档数据 - 游戏存档数据 </summary>
        public string Ppe_PlayerPrefs { get; }
        /// <summary> 存档数据 - 编辑器存档数据 </summary>
        public string Ppe_EditorPrefs { get; }
        /// <summary> 存档数据 - 自动解密 </summary>
        public string Ppe_AutoDecryption { get; }
        /// <summary> 存档数据 - 删除全部的提示 </summary>
        public string Ppe_DeleteAllHint { get; }
        /// <summary> 存档数据 - 动态密钥 </summary>
        public string Ppe_ActiveKey { get; }
        /// <summary> 存档数据 - 创建自定义提示 </summary>
        public string Ppe_CreateCustomHint { get; }

        #endregion

        #region AssetCheckerWindow
        /// <summary> 多级贴图 </summary>
        public string MipMaps { get; }
        /// <summary> 模型最大骨骼数量 </summary>
        public string ModelMaxBones { get; }
        /// <summary> 模型最大三角面数 </summary>
        public string ModelMaxTriangs { get; }
        /// <summary> 效果最大材质数 </summary>
        public string EffectMaxMatrials { get; }
        /// <summary> 效果最大粒子数 </summary>
        public string EffectMaxParticles { get; }
        #endregion

        #region TaskListPanel
        /// <summary>
        /// 任务列表的任务描述
        /// </summary>
        public string Tlp_TaskDesc {  get; }
        #endregion
    }

    public struct English : ILanguageBase
    {
        public readonly string PathSelect => "Select Path";

        public readonly string PathDefault => "Default Path";

        public readonly string PathSelecteError => "Path Selecte Error";

        public readonly string PathSelecteErrorContent => "Please configure the correct path";

        public readonly string Ok => "Ok";

        public readonly string Yes => "Yes";

        public readonly string Cancel => "Cancel";

        public readonly string ConfirmDelete => "Confirm Deletion";

        public readonly string ClearAll => "Clear All";

        public readonly string Delete => "Delete";

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

        public readonly string Add => "Add";

        public readonly string Key => "Key";

        public readonly string Value => "Value";

        public readonly string Type => "Type";

        public readonly string Count => "Count";

        public readonly string Encrypt => "Encrypt";

        public readonly string MoreOptions => "More Options";

        public readonly string Preferences => "Preferences";

        public readonly string ForceSave => "Force Save";

        public readonly string CreateCustom => "Create Custom";

        public readonly string Texture => "Texture";

        public readonly string Model => "Model";

        public readonly string Effects => "Effects";

        public readonly string Config => "Config";

        public readonly string Refresh => "Refresh";

        public readonly string Details => "Details";

        public readonly string Filtrate => "Filtrate";

        public readonly string Name => "Name";

        public readonly string Width => "Width";

        public readonly string Height => "Height";

        public readonly string ResourceType => "Resource Type";

        public readonly string Overview => "Overview";

        public readonly string Settings => "Settings";

        public readonly string MaxSize => "Max Size";

        public readonly string Memory => "Memory";

        public readonly string Compression => "Compression";

        public readonly string AlignAt => "Align At";

        public readonly string Holdon => "Hold On";

        public readonly string Reset => "Reset";

        public readonly string Save => "Save";

        public readonly string Information => "Information";

        public readonly string Color => "Color";

        public readonly string Score => "Score";

        public readonly string Vertex => "Vertex";

        public readonly string Triangular => "Triangular";

        public readonly string BoneCount => "Bone count";

        public readonly string Size => "Size";

        public readonly string DrawCall => "DrawCall";

        public readonly string Particle => "Particle";

        public readonly string Assets => "Assets";

        public readonly string Description => "Description";

        public readonly string File => "File";

        public readonly string Catalogue => "Catalogue";

        public readonly string Query => "Query";

        public readonly string Clear => "Clear";

        public readonly string Load => "Load";

        public readonly string Open => "Open";

        public readonly string Done => "Done";

        public readonly string List => "List";

        public readonly string Mark => "Mark";

        public readonly string Task => "Task";

        public readonly string Doing => "Doing";

        public readonly string Title => "Title";

        public readonly string Remove => "Remove";

        public readonly string Abandon => "Abandon";

        public readonly string Timeout => "Timeout";

        public readonly string Progress => "Progress";

        public readonly string RemoveAll => "RemoveAll";

        public readonly string Up => "Up";

        public readonly string Down => "Down";

        public readonly string MoveUp => "Move Up";

        public readonly string MoveDown => "Move Down";

        public readonly string Highlight => "Highlight";

        public readonly string BeingProcessed => "Being processed...";

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

        public readonly string Ppe_ImportHint => "Import PlayerPrefs from another project.\nAlso useful if you change product or company name";

        public readonly string Ppe_PlayerPrefs => "Player Prefs";

        public readonly string Ppe_EditorPrefs => "Editor Prefs";

        public readonly string Ppe_AutoDecryption => "Auto-Decryption";

        public readonly string Ppe_DeleteAllHint => "Are you sure you want to delete all preferences?";

        public readonly string Ppe_ActiveKey => "Active Key";

        public readonly string Ppe_CreateCustomHint => "Generate a script file in your project specifying a unique key to use.";

        public readonly string MipMaps => "MipMaps";

        public readonly string ModelMaxBones => "Model max bones";

        public readonly string ModelMaxTriangs => "Model max triangs";

        public readonly string EffectMaxMatrials => "Effect max matrials";

        public readonly string EffectMaxParticles => "Effect max particles";

        public readonly string Tlp_TaskDesc => "Please fill in the task description. ";
    }

    public struct Chinese : ILanguageBase
    {
        public readonly string PathSelect => "选择路径";

        public readonly string PathDefault => "默认路径";

        public readonly string PathSelecteError => "路径选择错误";

        public readonly string PathSelecteErrorContent => "请配置正确路径";

        public readonly string Ok => "好的";

        public readonly string Yes => "是的";

        public readonly string Cancel => "取消";

        public readonly string ConfirmDelete => "确认删除";

        public readonly string ClearAll => "清除全部";

        public readonly string Delete => "删除";

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

        public readonly string Add => "添加";

        public readonly string Key => "键";

        public readonly string Value => "值";

        public readonly string Type => "类型";

        public readonly string Count => "数量";

        public readonly string Encrypt => "加密";

        public readonly string MoreOptions => "更多设置";

        public readonly string Preferences => "预设";

        public readonly string ForceSave => "强制保存";

        public readonly string CreateCustom => "创建自定义";

        public readonly string Refresh => "刷新";

        public readonly string Details => "详情";

        public readonly string Filtrate => "筛选";

        public readonly string Name => "名称";

        public readonly string Width => "宽度";

        public readonly string Height => "高度";

        public readonly string ResourceType => "资源类型";

        public readonly string Texture => "纹理贴图";

        public readonly string Model => "模型";

        public readonly string Effects => "特效";

        public readonly string Config => "配置";

        public readonly string Overview => "总览";

        public readonly string Settings => "设置";

        public readonly string MaxSize => "最大尺寸";

        public readonly string Memory => "内存";

        public readonly string Compression => "压缩";

        public readonly string AlignAt => "对齐";

        public readonly string Holdon => "稍等";

        public readonly string Reset => "重置";

        public readonly string Save => "保存";

        public readonly string Information => "信息";

        public readonly string Color => "颜色";

        public readonly string Score => "分数";

        public readonly string Vertex => "顶点数";

        public readonly string Triangular => "三角面数";

        public readonly string BoneCount => "骨骼数";

        public readonly string Size => "尺寸";

        public readonly string DrawCall => "绘制次数";

        public readonly string Particle => "粒子";

        public readonly string Assets => "资产";

        public readonly string Description => "描述";

        public readonly string File => "文件";

        public readonly string Catalogue => "目录";

        public readonly string Query => "查询";

        public readonly string Clear => "清空";

        public readonly string Load => "加载";

        public readonly string Open => "打开";

        public readonly string Done => "完成";

        public readonly string List => "列表";

        public readonly string Mark => "标记";

        public readonly string Task => "任务";

        public readonly string Doing => "正在做";

        public readonly string Title => "标题";

        public readonly string Remove => "删除";

        public readonly string Abandon => "放弃";

        public readonly string Timeout => "超时";

        public readonly string Progress => "进度";

        public readonly string RemoveAll => "删除全部";

        public readonly string Up => "向上";

        public readonly string Down => "向下";

        public readonly string MoveUp => "上移";

        public readonly string MoveDown => "下移";

        public readonly string Highlight => "突出";

        public readonly string BeingProcessed => "正在处理中...";

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

        public readonly string Ppe_ImportHint => "从另一个项目导入存储数据。\n如果你更换了公司或产品，填写下边的信息将对你很有帮助。";

        public readonly string Ppe_PlayerPrefs => "游戏存储数据";

        public readonly string Ppe_EditorPrefs => "编辑器数据存储";

        public readonly string Ppe_AutoDecryption => "自动-解密";

        public readonly string Ppe_DeleteAllHint => "你确定你想要删除全部存档预设？";

        public readonly string Ppe_ActiveKey => "动态密钥";

        public readonly string Ppe_CreateCustomHint => "在项目中生成一个脚本文件，指定要使用的唯一密钥。";

        public readonly string MipMaps => "多级贴图";

        public readonly string ModelMaxBones => "模型最大骨骼数量";

        public readonly string ModelMaxTriangs => "模型最大三角面数";

        public readonly string EffectMaxMatrials => "特效最大材质数";

        public readonly string EffectMaxParticles => "特效最大粒子数";

        public readonly string Tlp_TaskDesc => "请填写任务描述。";
    }
}
