/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 09:59:22
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-30 15:02:01
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using EasyFramework.Edit;
using UnityEngine;

namespace EasyFramework.Managers.Pool
{
    public class PoolConfig : ScriptableObject
    {
        [HeaderPro("对象池元素列表", "List of object pool elements")]
        public List<PoolItem> PooledObjectsList = new();

        [System.Serializable]
        public class PoolItem
        {
            /// <summary>
            /// 池名
            /// </summary>
            [HeaderPro("池名字", "Pool name")]
            public string PoolName;
            
            /// <summary>
            /// 元素预制件
            /// </summary>
            [HeaderPro("元素预制件", "Element prefabrication component")]
            public GameObject Prefab;
            
            /// <summary>
            /// 初始缓存数量
            /// </summary>
            [HeaderPro("初始缓存数量", "Initial cache quantity")]
            public int Initial = 5;
            
            /// <summary>
            /// 最大存储数量
            /// </summary>
            [HeaderPro("最大存储数量", "Maximum storage capacity")]
            public int Max = 100;

            /// <summary>
            /// 开启日志
            /// </summary>
            [HeaderPro("开启日志", "Open debug")] public bool OpenDebug = false;
        }
    }
}