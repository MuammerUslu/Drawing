using System.Collections.Generic;
using Drawing.Data;
using UnityEngine;

namespace Drawing
{
    public class GameLogic
    {
        private readonly BFSHelper _bfsHelper;
        private readonly ConnectionHelper _connectionHelper;
        private readonly NodeSelector _nodeSelector;

        private readonly LineRenderer _lineRendererPrefab;
        private readonly LevelExtractor _levelExtractor;
        private readonly Transform _parentTransform;

        private readonly List<Node> _selectedNodes = new List<Node>();
        private readonly List<Connection> _selectedConnections = new List<Connection>();

        private bool LevelCompleted { get; set; }

        private LineRenderer _targetShapeLineRenderer;
        private LineRenderer _drawingLineRenderer;

        private const int MAX_CONNECTION_DEPTH = 10;
        private const float FIRST_NODE_SELECTION_CONNECTION_COUNT_FACTOR = 0.0001f;

        public GameLogic(LineRenderer lineRendererPrefab, Transform parentTransform)
        {
            _lineRendererPrefab = lineRendererPrefab;
            _levelExtractor = new LevelExtractor();
            _parentTransform = parentTransform;

            _bfsHelper = new BFSHelper();
            _connectionHelper = new ConnectionHelper();
            _nodeSelector = new NodeSelector();
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

            // Instantiate line renderers
            _targetShapeLineRenderer = Object.Instantiate(_lineRendererPrefab, _parentTransform);
            _drawingLineRenderer = Object.Instantiate(_lineRendererPrefab, _parentTransform);
            _drawingLineRenderer.gameObject.name = "DrawingLine";
            _drawingLineRenderer.startWidth *= 3f;

            // Set positions for target shape
            Vector3[] linePositions = allPoints.ToArray();
            _targetShapeLineRenderer.positionCount = allPoints.Count;
            _targetShapeLineRenderer.SetPositions(linePositions);

            // Extract graph info
            _levelExtractor.ExtractLevelData(linePositions);
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

            Node closestNode = _nodeSelector.GetClosestNodeWithAdvantage(
                _levelExtractor.Nodes,
                worldPosition,
                FIRST_NODE_SELECTION_CONNECTION_COUNT_FACTOR
            );

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

            HashSet<Node> reachable = _bfsHelper.LimitedDepth_BFS(
                lastNode,
                MAX_CONNECTION_DEPTH,
                out var parents
            );
            if (reachable == null || reachable.Count == 0)
                return;

            // Closest point to drag position inside reachable nodes
            Node closestNode = _nodeSelector.GetClosestNode(reachable, worldPosition);
            if (closestNode == null || closestNode == lastNode)
                return;

            // BFS path reconstruct
            List<Node> bfsNodePath = _bfsHelper.ReconstructPath(parents, lastNode, closestNode);
            if (bfsNodePath.Count < 2)
                return;

            // Connection list
            List<Connection> bfsConnections = _connectionHelper.BuildConnectionList(bfsNodePath);
            if (bfsConnections.Count == 0)
                return;

            // Merge
            _connectionHelper.MergeConnections(_selectedConnections, bfsConnections);

            // Yeni node listesi
            List<Node> finalNodePath = _connectionHelper.BuildNodeListFromConnections(_selectedNodes[0], _selectedConnections);

            _selectedNodes.Clear();
            _selectedNodes.AddRange(finalNodePath);

            DrawPath(finalNodePath);

            CheckIfLevelCompleted();

            CheckFail(finalNodePath);
        }

        private void CheckFail(List<Node> finalNodePath)
        {
            if (!LevelCompleted && finalNodePath.Count > 0)
            {
                Node lastReachedNode = finalNodePath[^1];
                if (_connectionHelper.IsNodeFullyUsed(lastReachedNode, _selectedConnections))
                {
                    Constants.FailedTry?.Invoke();
                    OnNodeRelease(Vector3.zero);
                }
            }
        }

        private void OnNodeRelease(Vector3 worldPosition)
        {
            if (LevelCompleted)
                return;

            _drawingLineRenderer.positionCount = 0;
            _selectedNodes.Clear();
            _selectedConnections.Clear();
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
    }
}