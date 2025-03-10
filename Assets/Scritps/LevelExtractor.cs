using System.Collections.Generic;
using UnityEngine;

namespace Drawing
{
    public class LevelExtractor
    {
        public List<Node> Nodes { get; } = new List<Node>();
        public List<Connection> Connections { get; } = new List<Connection>();

        public void ExtractLevelData(LineRenderer[] lineRenderers)
        {
            Nodes.Clear();
            Connections.Clear();
            
            Dictionary<Vector3, Node> nodeLookup = new Dictionary<Vector3, Node>();

            foreach (var line in lineRenderers)
            {
                if (line.positionCount < 2) continue;

                Vector3 startPos = line.GetPosition(0);
                Vector3 endPos = line.GetPosition(line.positionCount - 1);

                if (!nodeLookup.TryGetValue(startPos, out Node startNode))
                {
                    startNode = new Node(startPos);
                    Nodes.Add(startNode);
                    nodeLookup[startPos] = startNode;
                }

                if (!nodeLookup.TryGetValue(endPos, out Node endNode))
                {
                    endNode = new Node(endPos);
                    Nodes.Add(endNode);
                    nodeLookup[endPos] = endNode;
                }

                Connection connection = new Connection(startNode, endNode, line);
                Connections.Add(connection);

                startNode.Connections.Add(connection);
                endNode.Connections.Add(connection);
            }

            Debug.Log($"Level Extracted! Nodes: {Nodes.Count}, Connections: {Connections.Count}");
        }

    }
}