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

using UnityEngine;

namespace EasyFramework.Edit.Windows.Dataable
{
    /// <summary>
    /// 数据配置
    /// </summary>
    //[CreateAssetMenu(fileName = "DatableConfig", menuName = "EasyFramework/Config")]
    public class DatableConfig : ScriptableObject
    {
        [SerializeField, HeaderPro("生成的数据类型", "The type of generated data")]
        public DataType LubanDataType = DataType.Json;

        [SerializeField, HeaderPro("生成什么端的数据", "Generate data for which type of end?")]
        public DataTargetType LubanDataTargetType = DataTargetType.Both;

        [SerializeField, HeaderPro("生成脚本的命名空间", "The namespace for generating the script")]
        public string LubanNamespace = "cfg";

        [SerializeField, HeaderPro("电子表格存储文件夹路径", "Excel Save Folder Path")]
        public string LubanSourcePath = "";

        [SerializeField, HeaderPro("导表工具生成的代码路径", "Luban Code Path")]
        public string LubanCodePath = "Assets/luban/Code";

        [SerializeField, HeaderPro("导表工具生成的数据路径", "Luban Data Path")]
        public string LubanDataPath = "Assets/luban/Data";
    }
}