/*
 * ================================================
 * Describe:      This script is used to building component current game object scripts.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-13 16:32:16
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-13 16:32:16
 * ScriptVersion: 0.1
 * ===============================================
 */

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// UI视窗自动绑定代码 (仅编辑器使用，不打入构建包)
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/UiBinding", 100)]
    public sealed class UiBinding : MonoBehaviour
    {
        /// <summary>
        /// 绑定数据
        /// </summary>
        [Serializable]
        public class BindData
        {
            /// <summary> 组件全称 </summary>
            [SerializeField] public string RealName;

            /// <summary> 脚本名 </summary>
            [SerializeField] public string ScriptName;

            /// <summary> 组件 </summary>
            [SerializeField] public Component BindCom;
        }

        private UiBinding()
        {
        }

        /// <summary>
        /// 命名空间
        /// </summary>
        [SerializeField] public string Namespace;

        /// <summary>
        /// 脚本说明
        /// </summary>
        [SerializeField] public string Describe = "Please modify the description.";

        /// <summary>
        /// 页面自动销毁
        /// </summary>
        [SerializeField] public bool AutoDestroy = true;

        /// <summary>
        /// 自动销毁时间
        /// </summary>
        [SerializeField] public float AutoDestroyCountdown = 10.0f;

        /// <summary>
        /// 页面类型
        /// </summary>
        [SerializeField] public UIViewType ViewType = UIViewType.Page;

        /// <summary>
        /// 待绑定列表
        /// </summary>
        [SerializeField] public List<BindData> BindDatas = new List<BindData>();

        /// <summary>
        /// 预制件路径
        /// </summary>
        [SerializeField] public string PrefabPath;

        /// <summary>
        /// 脚本路径
        /// </summary>
        [SerializeField] public string ScriptPath;

        /// <summary>
        /// 是否创建预制件
        /// </summary>
        [SerializeField] public bool CreatePrefab;

        /// <summary>
        /// 绑定列表是否收起 (Inspector UI 状态，不序列化)
        /// </summary>
        [System.NonSerialized] public bool PackUpBindList;

        /// <summary>
        /// 通过类型排序 (EditorPrefs 管理，不序列化)
        /// </summary>
        [System.NonSerialized] public bool SortByType;

        /// <summary>
        /// 通过名字长度排序 (EditorPrefs 管理，不序列化)
        /// </summary>
        [System.NonSerialized] public bool SortByNameLength;

        /// <summary>
        /// 生成后是否卸载该脚本
        /// </summary>
        [SerializeField] public bool DeleteScript;
    }
}

#endif