/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-10 15:43:25
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-30 10:55:00
 * ScriptVersion: 0.3
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
        /// 界面绘制（模板方法：为子类绘制预留统一的窗口级横切入口，实际绘制在 OnPanelGUI）
        /// <para>Panel drawing (template method): a unified window-level hook for subclasses;
        /// the actual content lives in OnPanelGUI.</para>
        /// </summary>
        public void OnGUI()
        {
            OnPanelGUI();
        }

        /// <summary>
        /// 面板绘制内容（子类实现）
        /// <para>Panel content to be implemented by subclasses.</para>
        /// </summary>
        protected abstract void OnPanelGUI();

        /// <summary>
        /// 当销毁时
        /// </summary>
        public virtual void OnDestroy()
        {
        }
    }
}
