namespace EasyFramework.Windows
{
    namespace PrefabsCompare
    {
        public abstract class CompareInfo
        {
            /// <summary>
            /// 每个信息的ID值
            /// </summary>
            public int ID { get; set; }

            /// <summary>
            /// 每个信息的名称
            /// GameObject：GameObject的名字
            /// Component：Component的类型
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 所在树的深度
            /// </summary>
            public int Depth { get; set; }

            /// <summary>
            /// 父节点
            /// </summary>
            public CompareInfo Parent { get; set; }

            /// <summary>
            /// 对比时，左右对象缺失情况
            /// </summary>
            public MissType MissType { get; set; }

            /// <summary>
            /// 是否全部相同
            /// </summary>
            public abstract bool AllEqual();

            public CompareInfo(string name, int depth, int id)
            {
                Name = name;
                ID = id;
                Depth = depth;
            }
        }
    }
}