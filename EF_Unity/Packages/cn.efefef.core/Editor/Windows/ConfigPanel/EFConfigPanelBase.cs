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

using UnityEditor;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// 设置面板基类
    /// </summary>
    public abstract class EFConfigPanelBase
    {
        /// <summary>
        /// 配置总路径（由 OnEnable 默认实现保存，子类可直接读取）
        /// <para>Config root path saved by the default OnEnable implementation; readable by subclasses.</para>
        /// </summary>
        protected string AssetsPath { get; private set; }

        /// <summary>
        /// 面板名称
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// 当首次进入（默认保存路径并加载数据；有额外初始化需求的子类可重写）
        /// <para>On enable (default: save the path and load data; override for extra initialization).</para>
        /// </summary>
        /// <param name="assetsPath">配置总路径<para>Config root path.</para></param>
        public virtual void OnEnable(string assetsPath)
        {
            AssetsPath = assetsPath;
            LoadWindowData();
        }

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
        /// 保存并刷新资源数据库（统一入口，替代各面板私有的 SaveAssetsInfo）
        /// <para>Save and refresh the asset database (shared entry, replaces per-panel SaveAssetsInfo).</para>
        /// </summary>
        protected static void SaveAndRefresh()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 当销毁时
        /// </summary>
        public virtual void OnDestroy()
        {
        }
    }
}