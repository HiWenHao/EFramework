/*
 * ================================================
 * Describe:        UI 视窗运行时绑定数据持有者。编辑器期由 UiBindingEditor 收集组件引用并序列化到 BindDatas；
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2023-02-13 16:32:16
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-07-14
 * ScriptVersion:   0.6
 * ===============================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// UI 视窗运行时绑定数据持有者（存在于运行时，非 editor-only）。
    /// <para>运行时仅暴露 BindDatas + Resolve 解析能力；代码生成配置用 #if UNITY_EDITOR 包裹，不进运行时包。</para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/UiBinding", 100)]
    public sealed class UiBinding : MonoBehaviour
    {
        /// <summary>
        /// 绑定数据（运行时）
        /// </summary>
        [Serializable]
        public class BindData
        {
#if UNITY_EDITOR
            /// <summary> 是否禁用该绑定 </summary>
            [SerializeField] public bool Disabled;
#endif
            /// <summary> 组件全称 </summary>
            [SerializeField] public string RealName;

            /// <summary> 脚本名 - Resolve 的查找 key </summary>
            [SerializeField] public string ScriptName;

            /// <summary> 组件 </summary>
            [SerializeField] public Component BindCom;
        }

        /// <summary> 待绑定列表 </summary>
        [SerializeField] public List<BindData> BindDatas = new List<BindData>();

        /// <summary> 页面自动销毁 </summary>
        [SerializeField] public bool AutoDestroy = true;

        /// <summary> 自动销毁时间 </summary>
        [SerializeField] public float AutoDestroyCountdown = 10.0f;

        /// <summary> 页面类型 </summary>
        [SerializeField] public UIViewType ViewType = UIViewType.Page;

        /// <summary> 页面关闭时反播打开动画 </summary>
        [SerializeField] public bool CloseViewReverseAnimation;

        /// <summary> 页面打开时的动画类型 </summary>
        [SerializeField] public UiViewAnimationType ViewAnimationType = UiViewAnimationType.Scale;

#if UNITY_EDITOR
        /// <summary> 命名空间 </summary>
        [SerializeField] public string Namespace;

        /// <summary> 脚本说明 </summary>
        [SerializeField] public string Describe = "Please modify the description.";

        /// <summary> 预制件路径 </summary>
        [SerializeField] public string PrefabPath;

        /// <summary> 脚本路径 </summary>
        [SerializeField] public string ScriptPath;

        /// <summary> 是否创建预制件 </summary>
        [SerializeField] public bool CreatePrefab;

        /// <summary> 绑定列表是否收起（编辑器 UI 状态，不序列化） </summary>
        [NonSerialized] public bool PackUpBindList = true;

        /// <summary> 通过类型排序（编辑器 UI 状态，不序列化） </summary>
        [NonSerialized] public bool SortByType = true;

        /// <summary> 通过名字长度排序（编辑器 UI 状态，不序列化） </summary>
        [NonSerialized] public bool SortByNameLength = true;
#endif

        #region Resolve - 运行时按 ScriptName 取已绑定组件

        /// <summary>
        /// 按 ScriptName 从 BindDatas 取出已序列化的精确组件引用。
        /// <para>· key 用 ScriptName，不用 RealName → 运行时改名 GameObject 不影响绑定。</para>
        /// </summary>
        /// <param name="scriptName">绑定的脚本字段名（如 "Img_Head"）</param>
        public T Resolve<T>(string scriptName) where T : Component
            => BindDatas.Find(d => d.ScriptName == scriptName)?.BindCom as T;

        #endregion
    }
}