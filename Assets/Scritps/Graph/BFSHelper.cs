using System.Collections.Generic;

namespace Drawing
{
    public class BFSHelper
    {
        private Queue<Node> _queue;
        private HashSet<Node> _visited;
        private Dictionary<Node, Node> _parents;
        private List<Node> _reconstructList;
        private Dictionary<Node, int> _depthMap;

        public BFSHelper()
        {
            _queue = new Queue<Node>();
            _visited = new HashSet<Node>();
            _parents = new Dictionary<Node, Node>();
            _reconstructList = new List<Node>();
            _depthMap = new Dictionary<Node, int>();
        }

        public List<Node> FindPath_BFS(Node start, Node target)
        {
            _queue.Clear();
            _visited.Clear();
            _parents.Clear();
            _reconstructList.Clear();

            _queue.Enqueue(start);
            _visited.Add(start);
            _parents[start] = null;

            while (_queue.Count > 0)
            {
                Node current = _queue.Dequeue();
                if (current == target)
                    break;

                foreach (var conn in current.Connections)
                {
                    Node neighbor = (conn.StartNode == current)
                        ? conn.EndNode
                        : conn.StartNode;

                    if (!_visited.Contains(neighbor))
                    {
                        _visited.Add(neighbor);
                        _parents[neighbor] = current;
                        _queue.Enqueue(neighbor);
                    }
                }
            }

            if (!_parents.ContainsKey(target))
            {
                return null;
            }

            Node b = target;
            while (b != null)
            {
                _reconstructList.Add(b);
                b = _parents[b];
            }

            _reconstructList.Reverse();

            return _reconstructList;
        }

        public HashSet<Node> LimitedDepth_BFS(Node start, int maxDepth, out Dictionary<Node, Node> parents)
        {
            _queue.Clear();
            _visited.Clear();
            _parents.Clear();
            _depthMap.Clear();

            _queue.Enqueue(start);
            _visited.Add(start);
            _parents[start] = null;
            _depthMap[start] = 0;

            while (_queue.Count > 0)
            {
                Node current = _queue.Dequeue();
                int d = _depthMap[current];
                if (d >= maxDepth)
                    continue;

                foreach (var conn in current.Connections)
                {
                    Node neighbor = (conn.StartNode == current) ? conn.EndNode : conn.StartNode;
                    if (!_visited.Contains(neighbor))
                    {
                        _visited.Add(neighbor);
                        _parents[neighbor] = current;
                        _depthMap[neighbor] = d + 1;
                        _queue.Enqueue(neighbor);
                    }
                }
            }

            parents = _parents;
            return _visited;
        }

        public List<Node> ReconstructPath(Dictionary<Node, Node> parents, Node start, Node end)
        {
            _reconstructList.Clear();
            Node c = end;
            while (c != null)
            {
                _reconstructList.Add(c);
                if (!parents.TryGetValue(c, out var p))
                    break;
                c = p;
            }

            _reconstructList.Reverse();
            return _reconstructList;
        }
    }
}