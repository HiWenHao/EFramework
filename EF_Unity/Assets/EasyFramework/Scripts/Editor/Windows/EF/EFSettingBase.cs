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
        public abstract class EFSettingBase
        {
            public abstract void OnEnable(string assetsPath);
            public abstract void OnGUI();
            public abstract void OnDestroy();
        }
    }
}
