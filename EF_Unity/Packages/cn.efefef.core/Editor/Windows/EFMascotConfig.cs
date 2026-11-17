/*
 * ================================================
 * Describe:      编辑器吉祥物素材配置：自定义目录路径、模板生成与素材规则校验。
 * Author:        WorkBuddy
 * CreationTime:  2026-11-17 17:55:00
 * ScriptVersion: 1.0
 * ===============================================
 */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows
{
    /// <summary>
    /// 编辑器吉祥物素材配置：管理 EFTools 下自定义素材目录的生成与校验。
    /// <para>Editor mascot asset configuration: creates and validates custom asset folders under EFTools.</para>
    /// </summary>
    public static class EFMascotConfig
    {
        #region 常量 / Constants

        /// <summary>工程内自定义素材目录（相对工程根）</summary>
        private const string CustomDirName = "EFTools/Mascots";

        /// <summary>工程内自定义素材目录（相对工程根，供 UI 展示）</summary>
        public const string CustomFolderRelativePath = CustomDirName;

        /// <summary>自定义素材说明文件名</summary>
        private const string ReadmeFileName = "README.txt";

        /// <summary>素材建议像素尺寸（与内置素材一致）</summary>
        public const int RecommendedSize = 32;

        /// <summary>素材命名示例（{0} 为帧序号，从 0 开始；实际前缀可自定）</summary>
        public const string FileNameFormat = "RunCat_{0}.png";

        /// <summary>素材文件扩展名</summary>
        public const string FileExtension = ".png";

        #endregion

        #region 持久化设置 / Persisted settings

        /// <summary>吉祥物选择的 EditorPrefs 键（按工程 AppPrefix 隔离）</summary>
        private const string SelectedMascotPrefKey = "EFCatMascot.Selected";

        /// <summary>
        /// 读取上次选择的吉祥物名（空串表示使用内置素材）
        /// <para>Read the last selected mascot name (empty means the builtin set).</para>
        /// </summary>
        /// <returns>素材名；未记录过时返回空串<para>The mascot name, empty when never set.</para></returns>
        public static string GetSelectedMascot()
        {
            return EditorPrefs.GetString(GetSelectedMascotPrefKey(), string.Empty);
        }

        /// <summary>
        /// 记录当前选择的吉祥物名（空串表示使用内置素材）
        /// <para>Store the selected mascot name (empty means the builtin set).</para>
        /// </summary>
        /// <param name="mascotName">素材名<para>The mascot name.</para></param>
        public static void SetSelectedMascot(string mascotName)
        {
            EditorPrefs.SetString(GetSelectedMascotPrefKey(), mascotName ?? string.Empty);
        }

        // 组装带工程前缀的 EditorPrefs 键，保证不同工程互不干扰
        private static string GetSelectedMascotPrefKey()
        {
            string prefix = ConfigManager.Project?.AppConst?.AppPrefix ?? string.Empty;
            return prefix + SelectedMascotPrefKey + "." + GetProjectHash();
        }

        // 取工程路径的稳定哈希，作为 EditorPrefs 键的一部分用于隔离不同工程
        // 注意：不能使用 string.GetHashCode()，它在部分运行时下是随机化的，会导致键不稳定
        private static string GetProjectHash()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            const uint fnvOffset = 2166136261u;
            const uint fnvPrime = 16777619u;
            uint hash = fnvOffset;
            for (int i = 0; i < path.Length; i++)
            {
                hash ^= path[i];
                hash *= fnvPrime;
            }
            return hash.ToString("X8");
        }

        #endregion

        #region 路径 / Paths

        /// <summary>
        /// 获取工程内自定义素材根目录
        /// <para>Get the custom mascot root directory under the project.</para>
        /// </summary>
        /// <returns>{projectRoot}/EFTools/Mascots<para>The directory path.</para></returns>
        public static string GetCustomRootDirectory()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            return projectRoot == null
                ? CustomDirName
                : Path.Combine(projectRoot.FullName, CustomDirName);
        }

        /// <summary>
        /// 获取指定名称的自定义素材目录
        /// <para>Get the custom mascot directory of the given name.</para>
        /// </summary>
        /// <param name="mascotName">素材名称<para>Mascot name.</para></param>
        /// <returns>目录完整路径<para>The full directory path.</para></returns>
        public static string GetCustomDirectory(string mascotName)
        {
            return Path.Combine(GetCustomRootDirectory(), mascotName);
        }

        /// <summary>
        /// 扫描自定义素材目录，按文件名中的序号排序返回全部帧文件
        /// <para>Scan the custom mascot folder and return all frame files ordered by their name index.</para>
        /// </summary>
        /// <param name="mascotName">素材名称<para>Mascot name.</para></param>
        /// <returns>帧文件路径列表，按序号升序<para>Frame file paths ordered by index.</para></returns>
        public static string[] ScanCustomFrameFiles(string mascotName)
        {
            string directory = GetCustomDirectory(mascotName);
            if (!Directory.Exists(directory))
                return new string[0];

            var files = Directory.GetFiles(directory, "*" + FileExtension);
            var list = new System.Collections.Generic.List<string>(files.Length);
            foreach (string file in files)
            {
                if (TryParseFrameIndex(Path.GetFileNameWithoutExtension(file), out int index))
                    list.Add(file);
            }

            // 按序号排序；序号相同则按文件名，保证顺序稳定
            list.Sort((a, b) =>
            {
                int ia = ParseFrameIndex(Path.GetFileNameWithoutExtension(a));
                int ib = ParseFrameIndex(Path.GetFileNameWithoutExtension(b));
                return ia != ib ? ia.CompareTo(ib) : string.CompareOrdinal(a, b);
            });
            return list.ToArray();
        }

        /// <summary>
        /// 解析文件名末尾的帧序号（如 RunCat_3 → 3）
        /// <para>Parse the trailing frame index of a file name (e.g. RunCat_3 → 3).</para>
        /// </summary>
        /// <param name="fileNameWithoutExtension">不含扩展名的文件名<para>The file name without extension.</para></param>
        /// <param name="index">解析出的序号<para>The parsed index.</para></param>
        /// <returns>解析成功返回 true<para>True on success.</para></returns>
        private static bool TryParseFrameIndex(string fileNameWithoutExtension, out int index)
        {
            index = ParseFrameIndex(fileNameWithoutExtension);
            return index >= 0;
        }

        /// <summary>
        /// 解析文件名末尾的数字序号，解析不出返回 -1
        /// <para>Parse the trailing numeric index of a file name, or -1 when absent.</para>
        /// </summary>
        /// <param name="fileNameWithoutExtension">不含扩展名的文件名<para>The file name without extension.</para></param>
        /// <returns>序号或 -1<para>The index, or -1.</para></returns>
        private static int ParseFrameIndex(string fileNameWithoutExtension)
        {
            if (string.IsNullOrEmpty(fileNameWithoutExtension))
                return -1;
            int end = fileNameWithoutExtension.Length;
            int start = end;
            while (start > 0 && char.IsDigit(fileNameWithoutExtension[start - 1]))
                start--;
            if (start == end)
                return -1;
            return int.TryParse(fileNameWithoutExtension.Substring(start, end - start), out int value) ? value : -1;
        }

        /// <summary>
        /// 获取包内置素材目录
        /// <para>Get the builtin mascot directory in the package.</para>
        /// </summary>
        /// <returns>包内相对路径<para>The package-relative path.</para></returns>
        public static string GetBuiltinAssetDirectory()
        {
            return Utility.Path.GetEfAssetsPath() + "/Mascots/RunCat";
        }

        /// <summary>
        /// 从磁盘读取 PNG 并构造纹理（自定义素材位于 Assets 之外，AssetDatabase 无法加载）
        /// <para>Load a PNG from disk into a texture (custom assets live outside Assets, so AssetDatabase cannot load them).</para>
        /// </summary>
        /// <param name="filePath">图片文件路径<para>The image file path.</para></param>
        /// <returns>纹理；读取失败返回 null<para>The texture, null on failure.</para></returns>
        public static Texture2D LoadTextureFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(filePath));
                texture.hideFlags = HideFlags.HideAndDontSave;
                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LC.Tr($"[EF] 读取吉祥物素材失败 '{filePath}'：{ex.Message}",
                                       $"[EF] Failed to read mascot asset '{filePath}': {ex.Message}"));
                return null;
            }
        }

        #endregion

        #region 模板生成 / Template generation

        /// <summary>
        /// 生成自定义素材目录与说明文件
        /// <para>Create the custom mascot folder along with its readme.</para>
        /// </summary>
        /// <param name="mascotName">素材名称（用作目录名）<para>Mascot name used as the folder name.</para></param>
        /// <param name="error">失败原因<para>Failure reason.</para></param>
        /// <returns>成功返回 true<para>True on success.</para></returns>
        public static bool CreateMascotTemplate(string mascotName, out string error)
        {
            error = null;
            mascotName = mascotName?.Trim();
            if (string.IsNullOrEmpty(mascotName))
            {
                error = LC.Combine("Core.MascotNameEmpty");
                return false;
            }
            if (mascotName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = LC.Format("Core.MascotNameInvalid", mascotName);
                return false;
            }

            string directory = GetCustomDirectory(mascotName);
            if (Directory.Exists(directory))
            {
                error = LC.Format("Core.MascotFolderExists", directory);
                return false;
            }

            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, ReadmeFileName), BuildReadme(mascotName));
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                error = LC.Format("Core.MascotCreateFailed", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 构建素材说明文本：告知用户帧数自定、像素尺寸、命名规则与配色要求（跟随当前语言）
        /// <para>Build the asset readme: flexible frame count, pixel size, naming rules and coloring notes.</para>
        /// </summary>
        /// <param name="mascotName">素材名称<para>Mascot name.</para></param>
        /// <returns>说明文本<para>The readme content.</para></returns>
        private static string BuildReadme(string mascotName)
        {
            string example0 = string.Format(FileNameFormat, 0);
            string example1 = string.Format(FileNameFormat, 1);
            string example2 = string.Format(FileNameFormat, 2);
            string example3 = string.Format(FileNameFormat, 3);
            string example4 = string.Format(FileNameFormat, 4);
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            return LC.Tr(
$@"自定义吉祥物素材说明 —— {mascotName}
============================================================

本目录用于放置编辑器面板吉祥物的自定义动画帧。
面板会优先加载本目录的素材；若本目录没有可用的帧，则回退到包内置素材。当前文档可以删除。

【需要放几张】
  帧数由你决定，放几张就播几帧循环，不限张数。
  例如：3 张（走、跑、跳）、5 张（一个流畅循环）、12 张（细腻动作）都可以。
  只要是连续编号的 PNG 就会被识别，数量不足 1 张则视为无素材。

【每张多大】
  建议 {RecommendedSize} × {RecommendedSize} 像素（RGBA 透明背景）。
  尺寸不一致也可以，但所有帧必须保持相同尺寸，否则动画会跳动。

【命名规则】
  文件名必须以数字结尾，程序按末尾数字从小到大排序播放。序号从 0 开始连续编号。
  例如（5 张）：
  {example0}
  {example1}
  {example2}
  {example3}
  {example4}
  前缀可以自定（Runner / mycat / cat 都行），只要以 .png 结尾且末尾带序号即可。
  注意：序号不可跳号，例如 0、1、3、4 缺 2 时，第 3 张之后将不再参与播放。

【绘制要求】
  · 背景必须完全透明（PNG 带 Alpha）。
  · 建议使用纯色剪影：程序按角色主色统一着色，图片本身用黑色或白色单色即可，
    这样换配色时无需重新绘制素材。
  · 主体请水平居中，四周留出少量空白，避免裁切。
  · 帧与帧之间保持同一朝向，程序会在需要掉头时自动水平镜像。

【放在哪里】
  直接把 PNG 放进本目录即可，Unity 导入后立即生效，无需改配置。

------------------------------------------------------------
生成时间：{stamp}
",
$@"Custom Mascot Asset Guide — {mascotName}
============================================================

This folder holds custom animation frames for the editor panel mascot.
The panel loads this folder first; if no usable frame is found it falls back to the builtin set. This document can be deleted.

【How many frames】
  The frame count is up to you: put N PNGs and they play as an N-frame loop, with no upper limit.
  For example: 3 (walk, run, jump), 5 (one smooth loop) or 12 (detailed motion) all work.
  Any PNG with a consecutive index is recognized; fewer than 1 means no asset.

【Frame size】
  Recommended: {RecommendedSize} × {RecommendedSize} pixels (RGBA with transparent background).
  Different sizes are tolerated, but all frames must share the same size or the animation will jitter.

【Naming】
  File names must end with a digit; frames are sorted and played by that trailing number. Indexing starts at 0 and must be consecutive.
  For example (5 frames):
  {example0}
  {example1}
  {example2}
  {example3}
  {example4}
  The prefix is up to you (Runner / mycat / cat); the only requirement is a .png extension ending with an index.
  Note: do not skip indices — with 0, 1, 3, 4 the frames from 3 onwards will not play.

【Drawing】
  · The background must be fully transparent (PNG with Alpha).
  · A single-colour silhouette is recommended: the panel tints it with the mascot colour, so the image
    itself can be plain black or white and never needs repainting when the colour changes.
  · Keep the subject horizontally centred with a little padding to avoid clipping.
  · Keep every frame facing the same way; the panel mirrors it automatically when the mascot turns around.

【Where to put them】
  Just drop the PNGs into this folder. Unity imports them and they take effect immediately — no config needed.

------------------------------------------------------------
Generated at: {stamp}
");
        }

        #endregion

        #region 校验 / Validation

        /// <summary>
        /// 校验自定义素材目录是否包含可用的帧序列（至少 1 张且序号从 0 连续）
        /// <para>Validate whether the custom folder holds a usable frame sequence (at least one, indices contiguous from 0).</para>
        /// </summary>
        /// <param name="mascotName">素材名称<para>Mascot name.</para></param>
        /// <param name="missing">序号缺口说明；无缺口返回空数组<para>Description of index gaps; empty when contiguous.</para></param>
        /// <returns>可用返回 true<para>True when usable.</para></returns>
        public static bool ValidateFrames(string mascotName, out string[] missing)
        {
            missing = new string[0];
            string[] files = ScanCustomFrameFiles(mascotName);
            if (files.Length == 0)
                return false;

            // 序号应从 0 开始连续；出现缺口时给出提示但不阻断播放
            var gaps = new System.Collections.Generic.List<string>();
            int expected = 0;
            foreach (string file in files)
            {
                int index = ParseFrameIndex(Path.GetFileNameWithoutExtension(file));
                if (index > expected)
                    gaps.Add($"缺少序号 {expected}~{index - 1}");
                expected = index + 1;
            }
            missing = gaps.ToArray();
            return true;
        }

        /// <summary>
        /// 列出工程内已有的自定义素材名称
        /// <para>List the custom mascot names already present in the project.</para>
        /// </summary>
        /// <returns>名称列表；目录不存在时返回空列表<para>The names, empty when the root folder is absent.</para></returns>
        public static string[] GetExistingMascotNames()
        {
            string root = GetCustomRootDirectory();
            if (!Directory.Exists(root))
                return new string[0];
            var dirs = Directory.GetDirectories(root);
            var names = new string[dirs.Length];
            for (int i = 0; i < dirs.Length; i++)
                names[i] = Path.GetFileName(dirs[i]);
            return names;
        }

        #endregion
    }
}
