/*
 * ================================================
 * Describe:      资产配置面板基类：统一「配置资产缺失 → 引导创建」骨架与统一保存入口。
 * Author:        Alvin8412
 * CreationTime:  2026-11-16 16:18:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-16
 * ScriptVersion: 1.0
 * ===============================================
 */

using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// 资产配置面板基类：为「配置资产不存在时显示引导创建」的面板提供统一骨架。
    /// <para>Base class for config panels that show a "create asset" guide when the config asset is missing.</para>
    /// </summary>
    /// <remarks>
    /// 子类只需实现 <see cref="OnPanelGUI"/>，在配置缺失时调用 <see cref="DrawMissingConfigGuide{T}"/>
    /// 即可获得一致的空态 UI 与创建逻辑；配置存在时的绘制完全由子类掌控。
    /// <para>Subclasses implement OnPanelGUI and call DrawMissingConfigGuide when the asset is missing
    /// for a consistent empty-state UI and creation flow; the loaded state is fully subclass-controlled.</para>
    /// </remarks>
    public abstract class EFAssetConfigPanelBase : EFConfigPanelBase
    {
        #region Methods

        /// <summary>
        /// 绘制「配置不存在」引导：留白 + 警告 + 创建按钮（创建后自动 Ping 并返回 true）
        /// <para>Draw the missing-config guide: spacing, warning and a create button (pings the new asset
        /// and returns true when created).</para>
        /// </summary>
        /// <typeparam name="T">配置资产类型<para>The config asset type.</para></typeparam>
        /// <param name="message">提示文案<para>The warning message.</para></param>
        /// <param name="createLabel">创建按钮文案<para>The create button label.</para></param>
        /// <param name="persistent">是否保存到 Runtime 配置目录（否则 Edit 目录）<para>Whether to save into the runtime config folder.</para></param>
        /// <returns>用户点击并创建了资产则返回该资产，否则 null<para>The created asset when the user clicked, otherwise null.</para></returns>
        protected T DrawMissingConfigGuide<T>(string message, string createLabel, bool persistent = true) where T : ScriptableObject
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.HelpBox(message, MessageType.Warning);
            EditorGUILayout.Space(12f);
            if (!GUILayout.Button(createLabel))
                return null;

            string folder = persistent ? ConfigManager.ConfigRuntimePath : ConfigManager.ConfigEditPath;
            T asset = Create.CreateSettings.Instance<T>(true, folder);
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        #endregion
    }
}