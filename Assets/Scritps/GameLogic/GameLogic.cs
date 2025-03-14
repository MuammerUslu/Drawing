using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Drawing.Data;
using UnityEngine;
using Object = UnityEngine.Object;

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

        private readonly List<LineRenderer> _targetShapeLineRenderers;
        private readonly LineRenderer _drawingLineRenderer;

        private const int MAX_CONNECTION_DEPTH = 10;
        private const float FIRST_NODE_SELECTION_CONNECTION_COUNT_FACTOR = 0.0001f;

        private bool _tryFailed;

        public GameLogic(LineRenderer lineRendererPrefab, Transform parentTransform)
        {
            _lineRendererPrefab = lineRendererPrefab;
            _levelExtractor = new LevelExtractor();
            _parentTransform = parentTransform;
            _targetShapeLineRenderers = new List<LineRenderer>();

            _bfsHelper = new BFSHelper();
            _connectionHelper = new ConnectionHelper();
            _nodeSelector = new NodeSelector();

            // Instantiate drawing line renderer
            _drawingLineRenderer = Object.Instantiate(_lineRendererPrefab, _parentTransform);
            _drawingLineRenderer.gameObject.name = "DrawingLine";
            _drawingLineRenderer.numCapVertices = 0;
            _drawingLineRenderer.numCornerVertices = 0;
            _drawingLineRenderer.startWidth *= 1.5f;
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

                _targetShapeLineRenderers.Add(Object.Instantiate(_lineRendererPrefab, _parentTransform));
                _targetShapeLineRenderers[^1].positionCount = lineData.points.Length;
                _targetShapeLineRenderers[^1].SetPositions(lineData.points);
            }

            // Set positions for target shape
            Vector3[] linePositions = allPoints.ToArray();

            // Extract graph info
            _levelExtractor.ExtractLevelData(linePositions);
        }

        private void ClearExistingLevel()
        {
            if (_targetShapeLineRenderers != null)
            {
                foreach (var targetShapeLine in _targetShapeLineRenderers)
                {
                    Object.Destroy(targetShapeLine.gameObject);
                }

                _targetShapeLineRenderers.Clear();
            }

            _drawingLineRenderer.positionCount = 0;

            _selectedNodes.Clear();
            _selectedConnections.Clear();
        }

        private void OnNodeClick(Vector3 worldPosition)
        {
            if (!CanMakeMove())
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
            if (!CanMakeMove())
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
                    DelayedNodeRelease(0.25f);
                }
            }
        }

        private async void DelayedNodeRelease(float delaySeconds)
        {
            _tryFailed = true;
            _drawingLineRenderer.startColor = Color.red;
            _drawingLineRenderer.endColor = Color.red;
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            _drawingLineRenderer.startColor = Color.black;
            _drawingLineRenderer.endColor = Color.black;
            _tryFailed = false;
            OnNodeRelease(Vector3.zero);
        }

        private bool CanMakeMove()
        {
            if (_tryFailed || LevelCompleted)
                return false;

            return true;
        }

        private void OnNodeRelease(Vector3 worldPosition)
        {
            if (!CanMakeMove())
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

            int totalConnectionsNeeded = _levelExtractor.Connections.Count;
            if (_selectedConnections.Count >= totalConnectionsNeeded)
            {
                LevelCompleted = true;
                GameManager.Instance.CompleteLevel();
            }
        }
    }
}