using System.Collections.Generic;
using System.Linq;
using Drawing.Data;
using UnityEngine;

namespace Drawing
{
    public class LevelHandler
    {
        private readonly LineRenderer _lineRendererPrefab;
        private readonly LevelExtractor _levelExtractor;
        private readonly Transform _parentTransform;

        private readonly List<Node> _selectedNodes = new List<Node>();
        private readonly Dictionary<Connection, bool> _selectedConnections = new Dictionary<Connection, bool>();
        private readonly Dictionary<Connection, LineRenderer> _activeLines = new Dictionary<Connection, LineRenderer>();

        private readonly List<LineRenderer> _lineRenderers;

        public bool LevelCompleted { get; private set; }

        public LevelHandler(LineRenderer lineRendererPrefab,Transform parentTransform)
        {
            _lineRendererPrefab = lineRendererPrefab;

            _lineRenderers = new List<LineRenderer>();
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

            foreach (var lineData in levelDataSo.linePositions)
            {
                if (lineData.points == null || lineData.points.Length < 2) continue;

                LineRenderer newLine = Object.Instantiate(_lineRendererPrefab, _parentTransform);
                newLine.positionCount = lineData.points.Length;
                newLine.SetPositions(lineData.points);
                _lineRenderers.Add(newLine);
            }

            _levelExtractor.ExtractLevelData(_lineRenderers.ToArray());
        }

        private void ClearExistingLevel()
        {
            foreach (var line in _lineRenderers)
            {
                if (line != null)
                    Object.Destroy(line.gameObject);
            }

            _lineRenderers.Clear();
            _bestConnection = null;
            
            foreach (var line in _activeLines.Values)
            {
                if (line != null)
                    Object.Destroy(line.gameObject);
            }

            _activeLines.Clear();
            _selectedNodes.Clear();
            _selectedConnections.Clear();
        }


        private void OnNodeClick(Vector3 worldPosition)
        {
            Node closestNode = GetClosestNode(worldPosition);
            if (closestNode != null)
            {
                _selectedNodes.Add(closestNode);
            }
        }

        private Connection _bestConnection;

        private void OnNodeDrag(Vector3 worldPosition)
        {
            if (_selectedNodes.Count == 0) return;

            Node lastNode = _selectedNodes[_selectedNodes.Count - 1];

            if (_bestConnection == null)
            {
                _bestConnection = GetBestDirectionalConnection(lastNode, worldPosition);
                return;
            }

            Node endNode = (_bestConnection.EndNode == lastNode) ? _bestConnection.StartNode : _bestConnection.EndNode;
            Node startNode = (_bestConnection.EndNode == lastNode) ? _bestConnection.EndNode : _bestConnection.StartNode;

            if (Vector3.Distance(startNode.Position, worldPosition) <= Vector3.Distance(endNode.Position, worldPosition))
            {
                Connection connection = GetBestDirectionalConnection(lastNode, worldPosition);

                if (connection != _bestConnection)
                {
                    RemoveLastUncompleteConnection();
                    _bestConnection = connection;
                }
            }

            // Set up dictionary values
            if (!_activeLines.TryGetValue(_bestConnection, out LineRenderer newLine))
            {
                newLine = Object.Instantiate(_lineRendererPrefab, _parentTransform);
                _activeLines[_bestConnection] = newLine;
                _selectedConnections[_bestConnection] = false;
            }

            if (_selectedConnections.Count >= 1 && _selectedConnections[_selectedConnections.Keys.Last()] == false && _bestConnection != _selectedConnections.Keys.Last())
            {
                RemoveLastUncompleteConnection();
            }

            // Draw line end check if completed
            bool connectionCompleted = DrawPartialLine(_bestConnection, worldPosition, newLine);

            if (connectionCompleted)
            {
                _selectedConnections[_bestConnection] = true;
                _selectedNodes.Add(endNode);
                _bestConnection = null;

                if (_levelExtractor.Connections.All(conn => _selectedConnections.ContainsKey(conn) && _selectedConnections[conn]))
                {
                    LevelCompleted = true;
                    GameManager.Instance.CompleteLevel();
                }
            }
        }

        private void RemoveLastUncompleteConnection()
        {
            if (_selectedConnections.TryGetValue(_bestConnection, out bool completed))
            {
                if (completed)
                    return;

                Object.Destroy(_activeLines.Values.Last().gameObject);

                _selectedConnections.Remove(_selectedConnections.Keys.Last());
                _activeLines.Remove(_activeLines.Keys.Last());
            }
        }

        private void OnNodeRelease(Vector3 worldPosition)
        {
            if(LevelCompleted)
                return;
            
            foreach (var line in _activeLines.Values)
            {
                Object.Destroy(line.gameObject);
            }

            _bestConnection = null;
            _activeLines.Clear();
            _selectedNodes.Clear();
            _selectedConnections.Clear();
        }

        private bool DrawPartialLine(Connection connection, Vector3 worldPosition, LineRenderer newLine)
        {
            if (_selectedConnections.TryGetValue(connection, out bool completed) && completed)
            {
                return false;
            }

            LineRenderer originalLine = connection.Line;
            if (originalLine == null || originalLine.positionCount < 2)
                return false;

            Node lastNode = _selectedNodes[_selectedNodes.Count - 1];
            bool isReversed = (connection.EndNode == lastNode);

            Vector3[] originalPositions = new Vector3[originalLine.positionCount];
            originalLine.GetPositions(originalPositions);

            if (isReversed)
                System.Array.Reverse(originalPositions);

            List<Vector3> interpolatedPositions = new List<Vector3> { originalPositions[0] };
            bool isComplete = false;

            int closestSegmentIndex = -1;
            float minDistance = float.MaxValue;

            for (int i = 1; i < originalPositions.Length; i++)
            {
                Vector3 segmentStart = originalPositions[i - 1];
                Vector3 segmentEnd = originalPositions[i];

                float distanceToSegment = Vector3.Distance(segmentStart, worldPosition) + Vector3.Distance(segmentEnd, worldPosition);

                if (distanceToSegment < minDistance)
                {
                    minDistance = distanceToSegment;
                    closestSegmentIndex = i - 1;
                }
            }

            if (closestSegmentIndex == -1)
                return false;

            for (int i = 1; i <= closestSegmentIndex; i++)
            {
                interpolatedPositions.Add(originalPositions[i]);
            }

            Vector3 start = originalPositions[closestSegmentIndex];
            Vector3 end = originalPositions[closestSegmentIndex + 1];

            Vector3 interpolatedPoint = Vector3.Lerp(start, end, GetInterpolationRatio(start, end, worldPosition));
            interpolatedPositions.Add(interpolatedPoint);

            if (Vector3.Distance(interpolatedPoint, end) < 0.01f && closestSegmentIndex + 1 == originalPositions.Length - 1)
            {
                isComplete = true;
                interpolatedPositions = new List<Vector3>(originalPositions);
            }

            newLine.positionCount = interpolatedPositions.Count;
            newLine.SetPositions(interpolatedPositions.ToArray());
            newLine.startColor = Color.red;
            newLine.endColor = Color.red;

            return isComplete;
        }

        private float GetInterpolationRatio(Vector3 start, Vector3 end, Vector3 target)
        {
            Vector3 startToEnd = end - start;
            Vector3 startToTarget = target - start;

            float projection = Vector3.Dot(startToTarget, startToEnd.normalized);
            float totalDistance = startToEnd.magnitude;

            return Mathf.Clamp01(projection / totalDistance);
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

        private Connection GetBestDirectionalConnection(Node fromNode, Vector3 worldPosition)
        {
            Connection bestConnection = null;
            float bestAngle = float.MaxValue;
            Vector3 movementDirection = (worldPosition - fromNode.Position).normalized;

            foreach (var connection in fromNode.Connections)
            {
                LineRenderer lineRenderer = connection.Line;
                if (lineRenderer == null || lineRenderer.positionCount < 2) continue;

                Vector3 secondPoint = (lineRenderer.positionCount > 2) ? lineRenderer.GetPosition(1) : ((connection.StartNode == fromNode) ? connection.EndNode.Position : connection.StartNode.Position);

                Vector3 directionToSecondPoint = (secondPoint - fromNode.Position).normalized;

                if (directionToSecondPoint == Vector3.zero) continue;

                float angle = Vector3.Angle(movementDirection, directionToSecondPoint);

                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    bestConnection = connection;
                }
            }

            return bestConnection;
        }
    }
}