using System;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace PrefabsCompare
    {
        public class CompareData
        {
            /// <summary>
            /// 所选择的对象ID
            /// </summary>
            public static int SelectedGameObjectID { get; set; } = -1;

            /// <summary>
            /// 是否显示相等项
            /// </summary>
            public static bool ShowEqual { get; set; } = true;

            /// <summary>
            /// 是否显示丢失的对象
            /// </summary>
            public static bool ShowMiss { get; set; } = true;

            /// <summary>
            /// GameObject视图的滚动进度
            /// </summary>
            public static Vector2 GameObjectTreeScroll { get; set; }


            public static string LeftPrefabPath { get; set; }
            public static string RightPrefabPath { get; set; }

            public static GameObject LeftPrefabContent { get; set; }
            public static GameObject RightPrefabContent { get; set; }


            /// <summary>
            /// 根节点对比信息
            /// </summary>
            public static GameObjectCompareInfo RootInfo { get; set; }

            /// <summary>
            /// 显示状态更改
            /// </summary>
            public static Action onShowStateChange { get; set; }

            /// <summary>
            /// 重新对比
            /// </summary>
            public static Action CompareCall { get; set; }
        }
    }
}
