using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace PrefabsCompare
    {
        public class CompareTreeViewItem<T> : UnityEditor.IMGUI.Controls.TreeViewItem where T : CompareInfo
        {
            public T Info { get; set; }
        }

        public class GameObjectCompareInfo : CompareInfo
        {
            /// <summary>
            /// GameObject的对比状态
            /// </summary>
            public GameObjectCompareType GameObjectCompare { get; set; }
            /// <summary>
            /// 子对象的对比信息数组
            /// </summary>
            public List<GameObjectCompareInfo> Children { get; set; } = new List<GameObjectCompareInfo>();
            /// <summary>
            /// 当前对象的Component数组
            /// </summary>
            public List<ComponentCompareInfo> Components { get; set; } = new List<ComponentCompareInfo>();

            /// <summary>
            /// 左边的GameObject对象
            /// </summary>
            public GameObject LeftGameObject { get; set; }

            /// <summary>
            /// 右边的GameObject对象
            /// </summary>
            public GameObject RightGameObject { get; set; }

            /// <summary>
            /// 是否全部相等
            /// </summary>
            public override bool AllEqual()
            {
                return GameObjectCompare == GameObjectCompareType.allEqual;
            }

            public GameObjectCompareInfo(string name, int depth, int id) : base(name, depth, id)
            {

            }
        }

        public class ComponentCompareInfo : CompareInfo
        {
            /// <summary>
            /// Component对比的状态
            /// </summary>
            public ComponentCompareType ComponentCompare { get; set; }
            /// <summary>
            /// 左边的Component对象
            /// </summary>
            public Component LeftComponent { get; set; }

            /// <summary>
            /// 右边的Component对象
            /// </summary>
            public Component RightComponent { get; set; }

            /// <summary>
            /// 是否全部相等
            /// </summary>
            public override bool AllEqual()
            {
                return ComponentCompare == ComponentCompareType.allEqual;
            }

            public ComponentCompareInfo(string name, int depth, int id) : base(name, depth, id)
            {

            }
        }
    }
}