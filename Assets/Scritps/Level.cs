using System.Collections.Generic;
using UnityEngine;

namespace Drawing
{
    public class Level : MonoBehaviour
    {
        [SerializeField] private LineRenderer[] lineRenderers;
        [SerializeField] private LineRenderer lineRendererPrefab;

        private LevelExtractor _levelExtractor;
        public List<Node> selectedNodes = new List<Node>();
        public List<Connection> selectedConnections = new List<Connection>();
        private LineRenderer activeLine;

        public List<Node> allNodes = new List<Node>();

        private Dictionary<Connection, LineRenderer> activeLines = new Dictionary<Connection, LineRenderer>();

        private void OnValidate()
        {
            lineRenderers = GetComponentsInChildren<LineRenderer>();
        }

        private void Start()
        {
            _levelExtractor = new LevelExtractor();
            _levelExtractor.ExtractLevelData(lineRenderers);
            allNodes = _levelExtractor.Nodes;
        }

        private void OnEnable()
        {
            Constants.OnClickPosition += OnNodeClick;
            Constants.OnDragPosition += OnNodeDrag;
            Constants.OnPointerUpPosition += OnNodeRelease;
        }

        private void OnDisable()
        {
            Constants.OnClickPosition -= OnNodeClick;
            Constants.OnDragPosition -= OnNodeDrag;
            Constants.OnPointerUpPosition -= OnNodeRelease;
        }

        private void OnNodeClick(Vector3 worldPosition)
        {
            Node closestNode = GetClosestNode(worldPosition);
            if (closestNode != null)
            {
                selectedNodes.Add(closestNode);
            }
        }

        private void OnNodeDrag(Vector3 worldPosition)
        {
            if (selectedNodes.Count == 0) return;

            Node lastNode = selectedNodes[selectedNodes.Count - 1];
            Node bestNode = GetBestDirectionalNode(lastNode, worldPosition);

            if (bestNode != null)
            {
                if (selectedNodes.Count > 1 && selectedNodes[selectedNodes.Count - 2] == bestNode)
                {
                    RemoveLastNode();
                    return;
                }

                if (!IsRecentDuplicate(bestNode))
                {
                    float bestNodeDistance = Vector2.Distance(bestNode.Position, worldPosition);

                    if (bestNodeDistance <= 0.1f)
                    {
                        Connection connection = GetConnectionBetweenNodes(lastNode, bestNode);
                        if (connection != null && !selectedConnections.Contains(connection))
                        {
                            selectedNodes.Add(bestNode);
                            selectedConnections.Add(connection);
                            DrawPartialLine(connection, worldPosition);
                        }
                    }
                }
            }
        }

        private void RemoveLastNode()
        {
            if (selectedNodes.Count < 2) return;

            Node lastNode = selectedNodes[selectedNodes.Count - 1];
            Node previousNode = selectedNodes[selectedNodes.Count - 2];

            Connection connection = GetConnectionBetweenNodes(previousNode, lastNode);

            if (connection != null)
            {
                selectedConnections.Remove(connection);

                if (activeLines.TryGetValue(connection, out LineRenderer line))
                {
                    Destroy(line.gameObject);
                    activeLines.Remove(connection);
                }
            }

            selectedNodes.RemoveAt(selectedNodes.Count - 1);
        }


        private void OnNodeRelease(Vector3 worldPosition)
        {
            foreach (var line in activeLines.Values)
            {
                Destroy(line.gameObject);
            }

            activeLines.Clear();
            selectedNodes.Clear();
            selectedConnections.Clear();
        }

        private bool IsRecentDuplicate(Node node)
        {
            if (selectedNodes.Count < 2) return false;
            return selectedNodes[selectedNodes.Count - 1] == node || selectedNodes[selectedNodes.Count - 2] == node;
        }

        private void DrawPartialLine(Connection connection, Vector3 worldPosition)
        {
            if (!activeLines.TryGetValue(connection, out LineRenderer newLine))
            {
                newLine = Instantiate(lineRendererPrefab, transform);
                activeLines[connection] = newLine;
            }

            Vector3 start = connection.StartNode.Position;
            Vector3 end = connection.EndNode.Position;
            float totalDistance = Vector3.Distance(start, end);
            float currentDistance = Vector3.Distance(start, worldPosition);
            float ratio = Mathf.Clamp01(currentDistance / totalDistance);

            Vector3 midpoint = Vector3.Lerp(start, end, ratio);

            newLine.positionCount = 2;
            newLine.SetPosition(0, start);
            newLine.SetPosition(1, midpoint);
            newLine.startColor = Color.red;
            newLine.endColor = Color.red;
        }


        private Node GetClosestNode(Vector3 worldPosition)
        {
            Node closestNode = null;
            float closestDistance = float.MaxValue;

            foreach (Node node in _levelExtractor.Nodes)
            {
                float distance = Vector3.Distance(node.Position, worldPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = node;
                }
            }

            return closestNode;
        }

        private Node GetBestDirectionalNode(Node fromNode, Vector3 worldPosition)
        {
            Node bestNode = null;
            float bestAngle = float.MaxValue;

            Vector3 movementDirection = (worldPosition - fromNode.Position).normalized;

            foreach (var connection in fromNode.Connections)
            {
                Node connectedNode = (connection.StartNode == fromNode) ? connection.EndNode : connection.StartNode;

                Vector3 directionToNode = (connectedNode.Position - fromNode.Position).normalized;
                float angle = Vector3.Angle(movementDirection, directionToNode);

                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    bestNode = connectedNode;
                }
            }

            return bestNode;
        }

        private Node GetClosestConnectedNode(Node fromNode, Vector3 targetPosition)
        {
            Node closestNode = null;
            float closestDistance = float.MaxValue;

            foreach (var connection in fromNode.Connections)
            {
                Node connectedNode = (connection.StartNode == fromNode) ? connection.EndNode : connection.StartNode;

                float distance = Vector3.Distance(connectedNode.Position, targetPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = connectedNode;
                }
            }

            return closestNode;
        }

        private Connection GetConnectionBetweenNodes(Node nodeA, Node nodeB)
        {
            foreach (var connection in _levelExtractor.Connections)
            {
                if ((connection.StartNode == nodeA && connection.EndNode == nodeB) ||
                    (connection.StartNode == nodeB && connection.EndNode == nodeA))
                {
                    return connection;
                }
            }

            return null;
        }
    }
}