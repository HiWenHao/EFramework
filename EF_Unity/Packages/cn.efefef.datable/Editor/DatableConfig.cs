/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 18:07:16
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 18:07:16
 * ScriptVersion:   0.1
 * ================================================
 */

using System.IO;
using UnityEngine;

namespace EasyFramework.Edit.Windows.Dataable
{
    /// <summary>
    /// 数据配置
    /// </summary>
    //[CreateAssetMenu(fileName = "DatableConfig", menuName = "EasyFramework/Config")]
    public class DatableConfig : ScriptableObject
    {
        /// <summary>
        /// Excel 源表默认目录（相对工程根）
        /// <para>Default Excel source directory, relative to the project root.</para>
        /// </summary>
        public const string DefaultSourceDir = "EFTools/Datable/Datas/";

        [SerializeField, HeaderPro("生成的数据类型", "The type of generated data")]
        public DataType LubanDataType = DataType.Json;

        [SerializeField, HeaderPro("生成什么端的数据", "Generate data for which type of end?")]
        public DataTargetType LubanDataTargetType = DataTargetType.Both;

        [SerializeField, HeaderPro("生成的管理器脚本名", "The generated manager script name")]
        public string LubanManagerName = "DatableMgr";

        [SerializeField, HeaderPro("生成脚本的命名空间", "The namespace for generating the script")]
        public string LubanNamespace = "Cfg";

        [SerializeField, HeaderPro("电子表格存储文件夹路径（工程根相对）", "Excel Save Folder Path (project-relative)")]
        public string LubanSourcePath = DefaultSourceDir;

        [SerializeField, HeaderPro("导表工具生成的代码路径", "Luban Code Path")]
        public string LubanCodePath = "Assets/luban/Code";

        [SerializeField, HeaderPro("导表工具生成的数据路径", "Luban Data Path")]
        public string LubanDataPath = "Assets/luban/Data";

        /// <summary>
        /// 把 <see cref="LubanSourcePath"/> 换算为本机绝对路径：相对值按工程根拼接，历史遗留的绝对值直接沿用。
        /// <para>Resolve <see cref="LubanSourcePath"/> to an absolute path; relative values are combined with the project root, legacy absolute values are kept.</para>
        /// </summary>
        public string ResolveSourcePath()
        {
            string source = string.IsNullOrWhiteSpace(LubanSourcePath) ? DefaultSourceDir : LubanSourcePath;
            source = source.Replace('\\', '/').Trim();
            return Path.IsPathRooted(source)
                ? Path.GetFullPath(source)
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", source));
        }
    }
}