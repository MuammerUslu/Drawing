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

    private void OnDisable()
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
            Vector3 snappedPos;
            if (FindSnapPosition(worldPos, out snappedPos))
            {
                worldPos = snappedPos;
            }

            UpdateTemporarySecondPoint(worldPos);
        }
        else if (Input.GetMouseButtonUp(2) && _isStraightLineMode)
        {
            Vector3 worldPos = GetMouseWorldPosition();
            Vector3 snappedPos;
            if (FindSnapPosition(worldPos, out snappedPos))
            {
                worldPos = snappedPos;
            }

            AddPoint(worldPos);
            FinishCurrentLine();
        }

        if (Input.GetMouseButtonDown(0))
        {
            StartNewLine(false);
        }
        else if (Input.GetMouseButton(0) && _isDrawing)
        {
            Vector3 worldPos = GetMouseWorldPosition();
            Vector3 snappedPos;
            if (FindSnapPosition(worldPos, out snappedPos))
            {
                worldPos = snappedPos;
            }

            if (!_isStraightLineMode && Vector3.Distance(_lastAddedPoint, worldPos) >= minDistanceBetweenPoints)
            {
                AddPoint(worldPos);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            FinishCurrentLine();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            RemoveLastLine();
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    private void StartNewLine(bool straightLineMode)
    {
        if (levelData == null) return;

        _isDrawing = true;
        _isStraightLineMode = straightLineMode;
        _currentLine.Clear();

        Vector3 startPos = GetMouseWorldPosition();
        Vector3 snappedPos;
        if (FindSnapPosition(startPos, out snappedPos))
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
            _currentLine[1] = point;
        }

        _currentLineRenderer.SetPositions(_currentLine.ToArray());
    }

    private void AddPoint(Vector3 point)
    {
        _currentLine.Add(point);
        _lastAddedPoint = point;

        _currentLineRenderer.positionCount = _currentLine.Count;
        _currentLineRenderer.SetPositions(_currentLine.ToArray());
    }

    private void FinishCurrentLine()
    {
        if (_currentLine.Count < 2 || levelData == null) return;

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

    private void RemoveLastLine()
    {
        if (_activeLines.Count == 0) return;

        LineRenderer lastLine = _activeLines[_activeLines.Count - 1];
        _activeLines.RemoveAt(_activeLines.Count - 1);
        DestroyImmediate(lastLine.gameObject);

        if (levelData != null && levelData.linePositions.Length > 0)
        {
            List<LineData> allLines = new List<LineData>(levelData.linePositions);
            allLines.RemoveAt(allLines.Count - 1);
            levelData.linePositions = allLines.ToArray();
            EditorUtility.SetDirty(levelData);
        }
    }

    private void LoadLevel()
    {
        if (levelData == null || levelData.linePositions == null) return;

        ClearExistingLines();

        foreach (LineData line in levelData.linePositions)
        {
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