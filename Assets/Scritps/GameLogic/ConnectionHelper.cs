using System.Collections.Generic;
using System.Linq;

namespace Drawing
{
    public class ConnectionHelper
    {
        private readonly List<Connection> _tempConnections = new List<Connection>();
        private readonly List<Node> _tempNodes = new List<Node>();

        public List<Connection> BuildConnectionList(List<Node> nodePath)
        {
            _tempConnections.Clear();

            for (int i = 0; i < nodePath.Count - 1; i++)
            {
                Node n1 = nodePath[i];
                Node n2 = nodePath[i + 1];

                Connection conn = n1.Connections
                    .FirstOrDefault(c =>
                        (c.StartNode == n1 && c.EndNode == n2) ||
                        (c.StartNode == n2 && c.EndNode == n1));

                if (conn != null)
                {
                    _tempConnections.Add(conn);
                }
            }

            return _tempConnections;
        }

        public void MergeConnections(List<Connection> selectedConnections, List<Connection> bfsConnections)
        {
            foreach (var newConn in bfsConnections)
            {
                int index = selectedConnections.IndexOf(newConn);
                if (index == -1)
                {
                    selectedConnections.Add(newConn);
                }
                else
                {
                    if (index == selectedConnections.Count - 1)
                    {
                        selectedConnections.RemoveAt(selectedConnections.Count - 1);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public List<Node> BuildNodeListFromConnections(Node firstNode, List<Connection> connections)
        {
            _tempNodes.Clear();
            if (connections.Count == 0)
                return _tempNodes;

            Connection firstConn = connections[0];
            if (firstNode == firstConn.StartNode)
            {
                _tempNodes.Add(firstConn.StartNode);
                _tempNodes.Add(firstConn.EndNode);
            }
            else
            {
                _tempNodes.Add(firstConn.EndNode);
                _tempNodes.Add(firstConn.StartNode);
            }

            for (int i = 1; i < connections.Count; i++)
            {
                Node lastNodeInList = _tempNodes[_tempNodes.Count - 1];
                Connection conn = connections[i];

                if (conn.StartNode == lastNodeInList)
                {
                    _tempNodes.Add(conn.EndNode);
                }
                else if (conn.EndNode == lastNodeInList)
                {
                    _tempNodes.Add(conn.StartNode);
                }
            }

            return _tempNodes;
        }

        public bool IsNodeFullyUsed(Node node, List<Connection> selectedConnections)
        {
            if (node.Connections.Count == 0)
                return true;

            foreach (Connection c in node.Connections)
            {
                if (!selectedConnections.Contains(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}