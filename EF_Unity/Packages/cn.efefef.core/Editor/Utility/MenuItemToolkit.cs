/*
 * ================================================
 * Describe:        规范菜单下拉列表的全部内容
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 15:49:48
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 15:49:48
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Edit
{
    /// <summary>
    /// 菜单工具包
    /// </summary>
    public sealed class MenuItemToolkit
    {
        // 下边是按钮根节点的展示信息
        public const string EfRoot = "EFTools/";
        public const string Settings = EfRoot + "⚙️ Settings &E";
        public const string Tools = EfRoot + "🛠️ Tools/";
        public const string Utility = EfRoot + "🎈 Utility/";
        public const string Folders = EfRoot + "📂 Folders/";
        public const string About = EfRoot + "💬 About Us/";


        // 下边是按钮根节点的优先级开始数值
        public const int SettingPriority = 0;
        public const int ToolsPriority = 10000;
        public const int UtilityPriority = 20000;
        public const int FoldersPriority = 90000;
        public const int AboutPriority = 100000;
    }
}