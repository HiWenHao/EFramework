/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-10 15:43:25
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-10 15:43:25
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace EasyFramework.Windows
{
    namespace SettingPanel
    {
        /// <summary>
        /// 设置面板基类
        /// </summary>
        internal abstract class EFSettingBase
        {
            internal EFSettingBase(string name)
            {
                Name = name;
            }

            protected bool m_IsInitialzed;
            public string Name { get; private set; }

            /// <summary>
            /// 当首次进入
            /// </summary>
            /// <param name="assetsPath">配置总路径</param>
            internal abstract void OnEnable(string assetsPath);

            /// <summary>
            /// 界面绘制
            /// </summary>
            internal abstract void OnGUI();

            /// <summary>
            /// 当销毁时
            /// </summary>
            internal virtual void OnDestroy() { }
        }
    }
}
