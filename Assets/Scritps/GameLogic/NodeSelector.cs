using System.Collections.Generic;
using UnityEngine;

namespace Drawing
{
    public class NodeSelector
    {
        public Node GetClosestNodeWithAdvantage(IEnumerable<Node> allNodes, Vector3 worldPosition, float advantageFactor)
        {
            Node bestNode = null;
            float bestScore = float.MaxValue;

            foreach (Node node in allNodes)
            {
                float dist = Vector3.Distance(node.Position, worldPosition);
                float score = dist - advantageFactor * node.Connections.Count;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestNode = node;
                }
            }

            return bestNode;
        }

        public Node GetClosestNode(IEnumerable<Node> nodeSet, Vector3 worldPosition)
        {
            Node closest = null;
            float closestDist = float.MaxValue;

            foreach (Node node in nodeSet)
            {
                float dist = Vector3.Distance(node.Position, worldPosition);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = node;
                }
            }

            return closest;
        }
    }
}