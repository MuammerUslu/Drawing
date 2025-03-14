using System.Collections.Generic;
using UnityEngine;

namespace Drawing
{
    public class LevelExtractor
    {
        public List<Node> Nodes { get; } = new List<Node>();
        public List<Connection> Connections { get; } = new List<Connection>();

        public void ExtractLevelData(Vector3[] positions)
        {
            Nodes.Clear();
            Connections.Clear();
            
            Dictionary<Vector3, Node> nodeLookup = new Dictionary<Vector3, Node>();
            Node previousNode = null;

            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 position = positions[i];
                
                if (!nodeLookup.TryGetValue(position, out Node currentNode))
                {
                    currentNode = new Node(position);
                    Nodes.Add(currentNode);
                    nodeLookup[position] = currentNode;
                }

                if (previousNode != null)
                {
                    Connection connection = new Connection(previousNode, currentNode);
                    Connections.Add(connection);
                    
                    previousNode.Connections.Add(connection);
                    currentNode.Connections.Add(connection);
                }

                previousNode = currentNode;
            }

            Debug.Log($"Level Extracted! Nodes: {Nodes.Count}, Connections: {Connections.Count}");
        }
    }
}