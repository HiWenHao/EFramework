using System;
namespace EasyFramework.Windows
{
    namespace PrefabsCompare
    {
        public enum MissType
        {
            none,
            missLeft, //左边没有对应的对象
            missRight, //右边没有对应的对象
            allExist, //左右都存在
        }

        [Flags]
        public enum ComponentCompareType
        {
            none = 0,
            contentEqual = 1 << 1, //内容相等
            allEqual = contentEqual,
        }

        [Flags]
        public enum GameObjectCompareType
        {
            none = 0,
            activeEqual = 1 << 0, //GameObject的ActiveSelf是否相等
            tagEqual = 1 << 1, //GameObject的Tag是否相等
            layerEqual = 1 << 2, //GameObject的Layer是否相等
            childCountEqual = 1 << 3, //子对象数量是否相等
            childContentEqual = 1 << 4, //子对象内容是否相等
            componentCountEqual = 1 << 5, //GameObject的Component数量是否相等
            componentContentEqual = 1 << 6, //GameObject的内容是否相等
            allEqual = activeEqual + tagEqual + layerEqual + childCountEqual +
                childContentEqual + componentCountEqual + componentContentEqual,
        }
    }
}