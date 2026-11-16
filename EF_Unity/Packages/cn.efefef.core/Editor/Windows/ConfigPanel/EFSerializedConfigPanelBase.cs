/*
 * ================================================
 * Describe:      序列化配置面板基类：封装 SerializedObject 生命周期、滚动位置与统一的改动应用。
 * Author:        Alvin8412
 * CreationTime:  2026-11-16 16:10:00
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
    /// 序列化配置面板基类：由 SerializedObject 持有 ScriptableObject 配置资产的面板共用。
    /// <para>Base class for config panels backed by a SerializedObject over a ScriptableObject asset;
    /// centralizes the serialized object lifecycle, scroll position and unified change application.</para>
    /// </summary>
    /// <typeparam name="T">配置资产类型（ScriptableObject）<para>The config asset type (ScriptableObject).</para></typeparam>
    public abstract class EFSerializedConfigPanelBase<T> : EFConfigPanelBase where T : ScriptableObject
    {
        #region Fields

        /// <summary>配置资产的序列化对象（LoadWindowData 中创建）<para>The serialized config asset, created in LoadWindowData.</para></summary>
        protected SerializedObject SerializedConfig;

        /// <summary>面板滚动位置<para>The panel scroll position.</para></summary>
        protected Vector2 ScrollPosition;

        #endregion

        #region Methods

        /// <summary>
        /// 用指定配置资产初始化序列化对象（子类 LoadWindowData 中调用）
        /// <para>Initialize the serialized object with the given config asset (call from LoadWindowData).</para>
        /// </summary>
        /// <param name="target">配置资产<para>The config asset.</para></param>
        protected void BindConfig(T target)
        {
            SerializedConfig = new SerializedObject(target);
            SerializedConfig.Update();
        }

        /// <summary>
        /// 查找配置字段（SerializedConfig 未绑定时返回 null）
        /// <para>Find a config property; returns null when SerializedConfig is not bound.</para>
        /// </summary>
        /// <param name="propertyPath">字段路径<para>Property path.</para></param>
        /// <returns>序列化字段或 null<para>The property or null.</para></returns>
        protected SerializedProperty FindConfigProperty(string propertyPath)
        {
            return SerializedConfig?.FindProperty(propertyPath);
        }

        /// <summary>
        /// 若本帧有改动则应用 SerializedConfig 并保存刷新（统一收口）
        /// <para>Apply SerializedConfig and save/refresh when changed this frame.</para>
        /// </summary>
        /// <param name="changed">ChangeCheckScope.changed<para>The ChangeCheckScope.changed flag.</para></param>
        /// <param name="withoutUndo">是否无撤销记录<para>Whether to apply without an undo entry.</para></param>
        protected void ApplyConfigIfChanged(bool changed, bool withoutUndo = false)
        {
            if (!changed)
                return;
            if (withoutUndo)
                SerializedConfig?.ApplyModifiedPropertiesWithoutUndo();
            SerializedConfig?.ApplyModifiedProperties();
            SaveAndRefresh();
        }

        /// <summary>
        /// 保存并刷新（OnDestroy 中调用，避免未落盘的改动丢失）
        /// <para>Save and refresh; call from OnDestroy to flush pending changes.</para>
        /// </summary>
        protected void FlushConfig()
        {
            SerializedConfig?.ApplyModifiedProperties();
            SaveAndRefresh();
        }

        #endregion
    }
}
