using System.Collections.Generic;
using System.Linq;

namespace EasyFramework
{
    /// <summary>
    /// 依赖关系绘制
    /// </summary>
    internal class DependencyGraph
    {
        /// <summary>
        /// 访问状态
        /// </summary>
        private enum VisitState
        {
            Unvisited,
            Visiting,
            Visited
        }

        private readonly Dictionary<string, HashSet<string>> _edges = new();

        public void AddDependency(string from, string to)
        {
            if (!_edges.TryGetValue(from, out var set))
            {
                set = new HashSet<string>();
                _edges[from] = set;
            }

            set.Add(to);
        }

        /// <summary>
        /// 返回所有环的列表，每个环是字符串列表 [A, B, C, A]
        /// </summary>
        public List<List<string>> FindAllCycles()
        {
            var cycles = new List<List<string>>();
            var state = new Dictionary<string, VisitState>();

            foreach (var node in _edges.Keys)
            {
                var nodeState = state.TryGetValue(node, out var s) ? s : VisitState.Unvisited;
                if (nodeState == VisitState.Visited) continue;
                var path = new List<string>();
                FindCycle(node, state, path, cycles);
            }

            return cycles;
        }

        // 查找循环
        private bool FindCycle(string current, Dictionary<string, VisitState> state, List<string> path, List<List<string>> cycles)
        {
            state[current] = VisitState.Visiting;
            path.Add(current);

            if (_edges.TryGetValue(current, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    var neighborState = state.TryGetValue(neighbor, out var ns) ? ns : VisitState.Unvisited;
                    if (neighborState == VisitState.Unvisited)
                    {
                        if (FindCycle(neighbor, state, path, cycles))
                            return true;
                    }
                    else if (neighborState == VisitState.Visiting)
                    {
                        // 发现环，提取子路径
                        int idx = path.IndexOf(neighbor);
                        var cycle = new List<string>(path.Skip(idx)) { neighbor };
                        cycles.Add(cycle);
                    }
                }
            }

            state[current] = VisitState.Visited;
            path.RemoveAt(path.Count - 1);
            return false;
        }
    }
}