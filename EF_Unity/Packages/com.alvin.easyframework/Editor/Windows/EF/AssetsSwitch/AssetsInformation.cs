/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-11 17:04:13
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-11 17:04:13
 * ScriptVersion: 0.1
 * ===============================================
*/

using System;
using System.Collections.Generic;

namespace EasyFramework.Windows
{
    namespace SettingPanel
    {
        /// <summary>
        /// Information about all managers, including whether to import and depend on modules.
        /// <para>所有管理器的信息，包含是否导入、以及依赖模块</para>
        /// </summary>
        [Serializable]
        public class AssetsInformation
        {
            /// <summary>
            /// 所有管理器的信息
            /// </summary>
            public List<ManagerInfo> Managers;

            /// <summary>
            /// 管理器列表开关
            /// </summary>
            public bool ManagerListSwitch;

            /// <summary>
            /// 插件列表开关
            /// </summary>
            public bool PluginsListSwitch;

            /// <summary>
            /// 示例开关
            /// </summary>
            public bool ExampleSwitch;

            /// <summary>
            /// 所有插件信息
            /// </summary>
            public List<PluginsInfo> Plugins;
        }

        public class InfoBase
        {
            /// <summary>
            /// 名称
            /// </summary>
            public string Name;
            /// <summary>
            /// 加载
            /// </summary>
            public bool IsLoad;

            /// <summary>
            /// 描述
            /// </summary>
            public List<string> Des;
        }

        /// <summary>
        /// 所有管理器的信息
        /// </summary>
        [Serializable]
        public class ManagerInfo : InfoBase
        {
            /// <summary>
            /// 行数索引
            /// </summary>
            public int MonoIndex;

            /// <summary>
            /// 依赖
            /// </summary>
            public List<string> Rely;
        }


        /// <summary>
        /// 所有插件信息
        /// </summary>
        [Serializable]
        public class PluginsInfo : InfoBase
        {

        }
    }
}
