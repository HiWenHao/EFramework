/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-10 15:43:25
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 16:47:08
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// 设置面板基类
    /// </summary>
    public abstract class EFConfigPanelBase
    {
        /// <summary>
        /// 面板排序优先级, -1时不做排序, 默认最后
        /// </summary>
        public virtual int Priority { get; } = -1;
        
        /// <summary>
        /// 面板名称
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// 当首次进入
        /// </summary>
        /// <param name="assetsPath">配置总路径</param>
        public abstract void OnEnable(string assetsPath);

        /// <summary>
        /// 加载窗口数据
        /// </summary>
        public virtual void LoadWindowData()
        {
        }

        /// <summary>
        /// 界面绘制
        /// </summary>
        public abstract void OnGUI();

        /// <summary>
        /// 当销毁时
        /// </summary>
        public virtual void OnDestroy()
        {
        }
    }
}