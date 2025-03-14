#region UNITY_EDITOR

using System.Collections.Generic;
using Drawing.Data;
using UnityEngine;
using UnityEditor;

public class LevelEditor : MonoBehaviour
{
    [SerializeField] private LevelDataSo levelData;
    [SerializeField] private LineRenderer linePrefab;
    [SerializeField] private float minDistanceBetweenPoints = 0.2f;
    [SerializeField] private float snapDistance = 0.15f;

    private readonly List<Vector3> _currentLine = new List<Vector3>();
    private readonly List<LineRenderer> _activeLines = new List<LineRenderer>();

    private LineRenderer _currentLineRenderer = null;
    private Vector3 _lastAddedPoint;
    private bool _isDrawing = false;
    private bool _isStraightLineMode = false;

    private void Start()
    {
        if (levelData != null)
        {
            LoadLevel();
        }
    }

    private void OnDestroy()
    {
        ClearExistingLines();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            StartNewLine(true);
        }
        else if (Input.GetMouseButton(2) && _isStraightLineMode)
        {
            Vector3 worldPos = GetMouseWorldPosition();
            if (FindSnapPosition(worldPos, out Vector3 snappedPos))
            {
                worldPos = snappedPos;
            }
            UpdateTemporarySecondPoint(worldPos);
        }
        else if (Input.GetMouseButtonUp(2) && _isStraightLineMode)
        {
            Vector3 worldPos = GetMouseWorldPosition();
            if (FindSnapPosition(worldPos, out Vector3 snappedPos))
            {
                worldPos = snappedPos;
            }
            AddPoint(worldPos);
            SubdivideLine();
            FinishCurrentLine();
        }

        if (Input.GetMouseButtonDown(0))
        {
            StartNewLine(false);
        }
        else if (Input.GetMouseButton(0) && _isDrawing)
        {
            Vector3 worldPos = GetMouseWorldPosition();
            if (FindSnapPosition(worldPos, out Vector3 snappedPos))
            {
                worldPos = snappedPos;
            }

            if (!_isStraightLineMode && Vector3.Distance(_lastAddedPoint, worldPos) >= minDistanceBetweenPoints)
            {
                AddPoint(worldPos);
            }
        }
        else if (Input.GetMouseButtonUp(0) && _isDrawing)
        {
            SubdivideLine();
            FinishCurrentLine();
        }

        if (Input.GetMouseButtonDown(1))
        {
            RemoveLastLine();
        }
    }

    private void StartNewLine(bool straightLineMode)
    {
        if (levelData == null) return;

        _isDrawing = true;
        _isStraightLineMode = straightLineMode;
        _currentLine.Clear();

        Vector3 startPos = GetMouseWorldPosition();
        if (FindSnapPosition(startPos, out Vector3 snappedPos))
        {
            startPos = snappedPos;
        }

        _currentLine.Add(startPos);
        _lastAddedPoint = startPos;

        _currentLineRenderer = Instantiate(linePrefab, transform);
        _currentLineRenderer.positionCount = 1;
        _currentLineRenderer.SetPosition(0, startPos);
    }

    private void UpdateTemporarySecondPoint(Vector3 point)
    {
        if (_currentLine.Count < 1) return;

        if (_currentLine.Count == 1)
        {
            _currentLine.Add(point);
            _currentLineRenderer.positionCount = 2;
        }
        else
        {
            _currentLine[_currentLine.Count - 1] = point;
        }
        _currentLineRenderer.SetPositions(_currentLine.ToArray());
    }

    private void AddPoint(Vector3 point)
    {
        if (_currentLine.Count > 0 &&
            Vector3.Distance(_currentLine[_currentLine.Count - 1], point) < 0.001f)
        {
            return;
        }

        _currentLine.Add(point);
        _lastAddedPoint = point;
        _currentLineRenderer.positionCount = _currentLine.Count;
        _currentLineRenderer.SetPositions(_currentLine.ToArray());
    }

    private void FinishCurrentLine()
    {
        if (_currentLine.Count < 2)
        {
            if (_currentLineRenderer != null)
            {
                DestroyImmediate(_currentLineRenderer.gameObject);
            }
            _currentLineRenderer = null;
            _isDrawing = false;
            _isStraightLineMode = false;
            return;
        }

        if (levelData == null)
        {
            if (_currentLineRenderer != null)
            {
                DestroyImmediate(_currentLineRenderer.gameObject);
            }
            _currentLineRenderer = null;
            _isDrawing = false;
            _isStraightLineMode = false;
            return;
        }

        if (_currentLine.Count > 1 &&
            Vector3.Distance(_currentLine[_currentLine.Count - 2], _currentLine[_currentLine.Count - 1]) < 0.001f)
        {
            _currentLine.RemoveAt(_currentLine.Count - 1);
        }

        List<LineData> allLines = new List<LineData>();
        if (levelData.linePositions != null)
            allLines.AddRange(levelData.linePositions);

        allLines.Add(new LineData { points = _currentLine.ToArray() });
        levelData.linePositions = allLines.ToArray();
        EditorUtility.SetDirty(levelData);

        _activeLines.Add(_currentLineRenderer);
        _currentLineRenderer = null;

        _isDrawing = false;
        _isStraightLineMode = false;
    }

    private void SubdivideLine()
    {
        if (_currentLine.Count < 2) return;

        List<Vector3> newPoints = new List<Vector3> { _currentLine[0] };

        for (int i = 1; i < _currentLine.Count; i++)
        {
            Vector3 start = _currentLine[i - 1];
            Vector3 end = _currentLine[i];
            float distance = Vector3.Distance(start, end);

            int segmentCount = Mathf.FloorToInt(distance / minDistanceBetweenPoints);

            if (segmentCount > 1)
            {
                for (int j = 1; j < segmentCount; j++)
                {
                    float t = (float)j / segmentCount;
                    newPoints.Add(Vector3.Lerp(start, end, t));
                }
            }

            newPoints.Add(end);
        }

        _currentLine.Clear();
        _currentLine.AddRange(newPoints);

        if (_currentLineRenderer != null)
        {
            _currentLineRenderer.positionCount = _currentLine.Count;
            _currentLineRenderer.SetPositions(_currentLine.ToArray());
        }
    }

    private void RemoveLastLine()
    {
        if (_activeLines.Count == 0) return;

        LineRenderer lastLine = _activeLines[_activeLines.Count - 1];
        _activeLines.RemoveAt(_activeLines.Count - 1);
        DestroyImmediate(lastLine.gameObject);

        if (levelData != null && levelData.linePositions != null && levelData.linePositions.Length > 0)
        {
            List<LineData> allLines = new List<LineData>(levelData.linePositions);
            if (allLines.Count > 0)
            {
                allLines.RemoveAt(allLines.Count - 1);
                levelData.linePositions = allLines.ToArray();
                EditorUtility.SetDirty(levelData);
            }
        }
    }

    private void LoadLevel()
    {
        if (levelData == null || levelData.linePositions == null) return;

        ClearExistingLines();

        foreach (LineData line in levelData.linePositions)
        {
            if (line.points == null || line.points.Length < 2) continue;

            LineRenderer lr = Instantiate(linePrefab, transform);
            lr.positionCount = line.points.Length;
            lr.SetPositions(line.points);
            _activeLines.Add(lr);
        }
    }

    private void ClearExistingLines()
    {
        foreach (var line in _activeLines)
        {
            if (line != null)
                DestroyImmediate(line.gameObject);
        }
        _activeLines.Clear();
    }

    public void ClearLevelData()
    {
        if (levelData == null) return;
        levelData.linePositions = new LineData[0];
        EditorUtility.SetDirty(levelData);
        ClearExistingLines();
    }

    public void SaveLevelData()
    {
        if (levelData == null) return;
        EditorUtility.SetDirty(levelData);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; 
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    private bool FindSnapPosition(Vector3 position, out Vector3 snapPosition)
    {
        snapPosition = position;
        float closestDistance = snapDistance;
        bool foundSnap = false;

        foreach (var line in _activeLines)
        {
            if (line == _currentLineRenderer) continue;
            if (line.positionCount < 2) continue;

            Vector3 startPos = line.GetPosition(0);
            Vector3 endPos = line.GetPosition(line.positionCount - 1);

            float startDistance = Vector3.Distance(position, startPos);
            float endDistance = Vector3.Distance(position, endPos);

            if (startDistance < closestDistance)
            {
                closestDistance = startDistance;
                snapPosition = startPos;
                foundSnap = true;
            }
            if (endDistance < closestDistance)
            {
                closestDistance = endDistance;
                snapPosition = endPos;
                foundSnap = true;
            }
        }

        return foundSnap;
    }
}


#endregion