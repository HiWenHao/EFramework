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

using System.Collections.Generic;
using EasyFramework.Manager.UI;
using UnityEngine;

namespace EasyFramework.UI
{
    /// <summary>
    /// UI视窗自动绑定代码
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/UiBinding", 100)]
    public sealed class UiBinding : MonoBehaviour
    {
        /// <summary>
        /// 绑定数据
        /// </summary>
        public class BindData
        {
            /// <summary> 组件全称 </summary>
            public string RealName;

            /// <summary> 脚本名 </summary>
            public string ScriptName;

            /// <summary> 组件 </summary>
            public Component BindCom;
        }

        private UiBinding()
        {
        }

        /// <summary>
        /// 命名空间
        /// </summary>
        public string Namespace;

        /// <summary>
        /// 脚本说明
        /// </summary>
        public string Describe = "Please modify the description.";
        
        /// <summary>
        /// 页面自动销毁
        /// </summary>
        public bool AutoDestroy = true;
        
        /// <summary>
        /// 自动销毁时间
        /// </summary>
        public float AutoDestroyCountdown = 10.0f;
        
        /// <summary>
        /// 页面类型
        /// </summary>
        public UIViewType ViewType = UIViewType.Page;

        /// <summary>
        /// 待绑定列表
        /// </summary>
        public List<BindData> BindDatas = new List<BindData>();

        /// <summary>
        /// 预制件路径
        /// </summary>
        public string PrefabPath;

        /// <summary>
        /// 脚本路径
        /// </summary>
        public string ScriptPath;

        /// <summary>
        /// 是否创建预制件
        /// </summary>
        public bool CreatePrefab;

        /// <summary>
        /// 绑定列表是否收起
        /// </summary>
        public bool PackUpBindList;

        /// <summary>
        /// 通过类型排序
        /// </summary>
        public bool SortByType;

        /// <summary>
        /// 通过名字长度排序
        /// </summary>
        public bool SortByNameLength;

        /// <summary>
        /// 生成后是否卸载该脚本
        /// </summary>
        public bool DeleteScript;
    }
}