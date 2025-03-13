using System;
using System.Collections.Generic;
using System.Linq;
using Drawing.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Drawing
{
    [Serializable]
    public class LevelHandler
    {
        private readonly LineRenderer _lineRendererPrefab;
        private readonly LevelExtractor _levelExtractor;
        private readonly Transform _parentTransform;

        private readonly List<Node> _selectedNodes = new List<Node>();
        private readonly List<Connection> _selectedConnections = new List<Connection>();

        public bool LevelCompleted { get; private set; }

        private LineRenderer _targetShapeLineRenderer;
        private LineRenderer _drawingLineRenderer;

        private const int MAX_CONNECTION_DEPTH = 10;
        private const float FIRST_NODE_SELECTION_CONNECTION_COUNT_FACTOR = 0.0001f;

        public LevelHandler(LineRenderer lineRendererPrefab, Transform parentTransform)
        {
            _lineRendererPrefab = lineRendererPrefab;

            _levelExtractor = new LevelExtractor();
            _parentTransform = parentTransform;
        }

        public void Subscribe()
        {
            Constants.OnClickPosition += OnNodeClick;
            Constants.OnDragPosition += OnNodeDrag;
            Constants.OnPointerUpPosition += OnNodeRelease;
        }

        public void Unsubscribe()
        {
            Constants.OnClickPosition -= OnNodeClick;
            Constants.OnDragPosition -= OnNodeDrag;
            Constants.OnPointerUpPosition -= OnNodeRelease;
        }

        public void LoadLevel(LevelDataSo levelDataSo)
        {
            if (levelDataSo == null || levelDataSo.linePositions == null) return;

            ClearExistingLevel();
            LevelCompleted = false;

            List<Vector3> allPoints = new List<Vector3>();

            foreach (var lineData in levelDataSo.linePositions)
            {
                if (lineData.points == null || lineData.points.Length < 2)
                    continue;

                foreach (var position in lineData.points)
                {
                    if (allPoints.Count == 0 || allPoints[^1] != position)
                        allPoints.Add(position);
                }
            }

            _targetShapeLineRenderer = Object.Instantiate(_lineRendererPrefab, _parentTransform);
            _drawingLineRenderer = Object.Instantiate(_lineRendererPrefab, _parentTransform);
            _drawingLineRenderer.gameObject.name = "DrawingLine";
            _drawingLineRenderer.startWidth *= 3f;

            Vector3[] linePositions = allPoints.ToArray();
            _targetShapeLineRenderer.positionCount = allPoints.Count;
            _targetShapeLineRenderer.SetPositions(linePositions);

            _levelExtractor.ExtractLevelData(linePositions);
        }


        private bool IsNodeFullyUsed(Node node)
        {
            if (node.Connections.Count == 0)
                return true;

            foreach (Connection c in node.Connections)
            {
                if (!_selectedConnections.Contains(c))
                {
                    return false;
                }
            }

            return true;
        }

        private void ClearExistingLevel()
        {
            if (_targetShapeLineRenderer != null)
                Object.Destroy(_targetShapeLineRenderer.gameObject);

            if (_drawingLineRenderer != null)
                Object.Destroy(_drawingLineRenderer.gameObject);

            _selectedNodes.Clear();
            _selectedConnections.Clear();
        }


        private void OnNodeClick(Vector3 worldPosition)
        {
            if (LevelCompleted)
                return;

            Node closestNode = GetClosestNodeWithConnectionAdvantage(worldPosition);
            if (closestNode != null)
            {
                _selectedNodes.Add(closestNode);
            }
        }

        private void OnNodeDrag(Vector3 worldPosition)
        {
            if (LevelCompleted)
                return;

            if (_selectedNodes.Count == 0)
                return;

            Node lastNode = _selectedNodes[^1];

            Dictionary<Node, Node> parents;
            HashSet<Node> reachableNodes = LimitedDepth_BFS(lastNode, MAX_CONNECTION_DEPTH, out parents);

            if (reachableNodes == null || reachableNodes.Count == 0)
                return;

            Node closestNode = GetClosestNodeFromSet(reachableNodes, worldPosition);
            if (closestNode == null || closestNode == lastNode)
                return;

            List<Node> bfsNodePath = ReconstructPath(parents, lastNode, closestNode);
            if (bfsNodePath.Count < 2)
                return;

            List<Connection> bfsConnections = BuildConnectionList(bfsNodePath);
            if (bfsConnections.Count == 0)
                return;

            MergeConnections(_selectedConnections, bfsConnections);

            List<Node> finalNodePath = BuildNodeListFromConnections(_selectedNodes[0], _selectedConnections);
            _selectedNodes.Clear();
            _selectedNodes.AddRange(finalNodePath);

            DrawPath(finalNodePath);

            CheckIfLevelCompleted();

            if (!LevelCompleted && finalNodePath.Count > 0)
            {
                Node lastReachedNode = finalNodePath[^1];
                if (IsNodeFullyUsed(lastReachedNode))
                {
                    Constants.FailedTry?.Invoke();
                    OnNodeRelease(Vector3.zero);
                }
            }
        }

        private HashSet<Node> LimitedDepth_BFS(Node start, int maxDepth, out Dictionary<Node, Node> parents)
        {
            parents = new Dictionary<Node, Node>();
            Dictionary<Node, int> depthMap = new Dictionary<Node, int>();
            Queue<Node> queue = new Queue<Node>();
            HashSet<Node> visited = new HashSet<Node>();

            queue.Enqueue(start);
            visited.Add(start);
            parents[start] = null;
            depthMap[start] = 0;

            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();
                int currentDepth = depthMap[current];

                if (currentDepth >= maxDepth)
                    continue;

                foreach (Connection connection in current.Connections)
                {
                    Node neighbor = (connection.StartNode == current)
                        ? connection.EndNode
                        : connection.StartNode;

                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parents[neighbor] = current;
                        depthMap[neighbor] = currentDepth + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited;
        }

        private void CheckIfLevelCompleted()
        {
            if (LevelCompleted)
                return;

            int totalConnectionsNeeded = _targetShapeLineRenderer.positionCount - 1;

            if (_selectedConnections.Count >= totalConnectionsNeeded)
            {
                LevelCompleted = true;
                GameManager.Instance.CompleteLevel();
            }
        }

        private List<Node> ReconstructPath(Dictionary<Node, Node> parents, Node start, Node end)
        {
            List<Node> path = new List<Node>();
            Node current = end;

            while (current != null)
            {
                path.Add(current);

                if (!parents.TryGetValue(current, out var parent))
                    break;

                current = parent;
            }

            path.Reverse();
            return path;
        }

        private List<Connection> BuildConnectionList(List<Node> nodePath)
        {
            List<Connection> connections = new List<Connection>();

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
                    connections.Add(conn);
                }
            }

            return connections;
        }

        private void MergeConnections(List<Connection> selectedConnections, List<Connection> bfsConnections)
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

        private List<Node> BuildNodeListFromConnections(Node firstNode, List<Connection> selectedConnections)
        {
            if (selectedConnections.Count == 0)
                return new List<Node>();

            List<Node> nodeList = new List<Node>();

            Connection firstConn = selectedConnections[0];
            if (firstNode == firstConn.StartNode)
            {
                nodeList.Add(firstConn.StartNode);
                nodeList.Add(firstConn.EndNode);
            }
            else
            {
                nodeList.Add(firstConn.EndNode);
                nodeList.Add(firstConn.StartNode);
            }

            for (int i = 1; i < selectedConnections.Count; i++)
            {
                Node lastNodeInList = nodeList[^1];
                Connection conn = selectedConnections[i];

                if (conn.StartNode == lastNodeInList)
                {
                    nodeList.Add(conn.EndNode);
                }
                else if (conn.EndNode == lastNodeInList)
                {
                    nodeList.Add(conn.StartNode);
                }
            }

            return nodeList;
        }

        private void DrawPath(List<Node> nodeList)
        {
            if (nodeList == null || nodeList.Count < 2)
            {
                _drawingLineRenderer.positionCount = 0;
                return;
            }

            _drawingLineRenderer.positionCount = nodeList.Count;
            for (int i = 0; i < nodeList.Count; i++)
            {
                _drawingLineRenderer.SetPosition(i, nodeList[i].Position);
            }

            _drawingLineRenderer.startColor = Color.black;
            _drawingLineRenderer.endColor = Color.black;
            _drawingLineRenderer.sortingOrder = 100;
        }

        private void OnNodeRelease(Vector3 worldPosition)
        {
            if (LevelCompleted)
                return;

            _drawingLineRenderer.positionCount = 0;
            _selectedNodes.Clear();
            _selectedConnections.Clear();
        }

        private Node GetClosestNodeWithConnectionAdvantage(Vector3 worldPosition)
        {
            Node bestNode = null;
            float bestScore = float.MaxValue;

            foreach (Node node in _levelExtractor.Nodes)
            {
                float dist = Vector3.Distance(node.Position, worldPosition);
                float score = dist - FIRST_NODE_SELECTION_CONNECTION_COUNT_FACTOR * node.Connections.Count;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestNode = node;
                }
            }

            return bestNode;
        }

        private Node GetClosestNodeFromSet(HashSet<Node> nodeSet, Vector3 worldPosition)
        {
            Node closestNode = null;
            float closestDist = float.MaxValue;

            foreach (Node node in nodeSet)
            {
                float dist = Vector3.Distance(node.Position, worldPosition);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestNode = node;
                }
            }

            return closestNode;
        }
    }
}