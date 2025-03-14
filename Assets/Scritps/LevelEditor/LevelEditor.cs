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

    // Tweak these to your liking:
    [Header("Smoothing Settings")]
    [Tooltip("Used by RDP to remove small peaks. Smaller => keep more points, bigger => remove more detail.")]
    public float rdpTolerance = 0.1f;
    [Tooltip("How many Catmull-Rom subdivisions per segment.")]
    public int catmullSubdivisions = 8;

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
        // Press S to smooth the last line with "RDP + Catmull-Rom"
        if (Input.GetKeyDown(KeyCode.S))
        {
            SmoothLastLine();
        }

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

    // --------------------------------
    //           SMOOTHING
    // --------------------------------

    /// <summary>
    /// Press 'S' -> Take the last line, apply Ramer–Douglas–Peucker to remove small peaks,
    /// then Catmull–Rom interpolation to get a smooth curve. Endpoints stay fixed.
    /// </summary>
    private void SmoothLastLine()
    {
        if (_activeLines.Count == 0) return;

        // 1) Get last line
        LineRenderer lastLine = _activeLines[_activeLines.Count - 1];
        if (lastLine.positionCount < 2) return;

        // 2) Original positions
        Vector3[] original = new Vector3[lastLine.positionCount];
        lastLine.GetPositions(original);

        // 3) Simplify with RDP
        List<Vector3> simplified = RamerDouglasPeucker(original, rdpTolerance);

        // 4) Interpolate with Catmull–Rom
        List<Vector3> finalPoints = CatmullRomSpline(simplified, catmullSubdivisions);

        // 5) Update the line renderer
        lastLine.positionCount = finalPoints.Count;
        lastLine.SetPositions(finalPoints.ToArray());

        // 6) Also update the last line in levelData
        if (levelData != null && levelData.linePositions != null && levelData.linePositions.Length > 0)
        {
            int lastIndex = levelData.linePositions.Length - 1;
            LineData ld = levelData.linePositions[lastIndex];
            ld.points = finalPoints.ToArray();
            EditorUtility.SetDirty(levelData);
        }
    }

    /// <summary>
    /// Ramer–Douglas–Peucker to remove points within 'tolerance' of the line.
    /// Preserves endpoints exactly.
    /// </summary>
    private List<Vector3> RamerDouglasPeucker(Vector3[] points, float tolerance)
    {
        if (points.Length < 2)
            return new List<Vector3>(points);

        // Convert to List for recursion
        List<Vector3> pointList = new List<Vector3>(points);
        return RDPRecursive(pointList, 0, pointList.Count - 1, tolerance);
    }

    private List<Vector3> RDPRecursive(List<Vector3> pts, int startIndex, int endIndex, float tol)
    {
        // We always keep the first and last
        Vector3 first = pts[startIndex];
        Vector3 last = pts[endIndex];

        float maxDistance = -1f;
        int indexFarthest = -1;

        // Find the point farthest from the line [first->last]
        for (int i = startIndex + 1; i < endIndex; i++)
        {
            float dist = PerpDist(pts[i], first, last);
            if (dist > maxDistance)
            {
                maxDistance = dist;
                indexFarthest = i;
            }
        }

        // If that distance is above tolerance, keep it
        if (maxDistance > tol)
        {
            // Recurse on each segment
            List<Vector3> leftSide = RDPRecursive(pts, startIndex, indexFarthest, tol);
            List<Vector3> rightSide = RDPRecursive(pts, indexFarthest, endIndex, tol);

            // Merge, removing the duplicate middle
            leftSide.RemoveAt(leftSide.Count - 1);
            leftSide.AddRange(rightSide);
            return leftSide;
        }
        else
        {
            // No point is far enough to keep, so just keep endpoints
            return new List<Vector3> { first, last };
        }
    }

    /// <summary>
    /// Returns perpendicular distance of 'pt' to line segment [p1->p2].
    /// </summary>
    private float PerpDist(Vector3 pt, Vector3 p1, Vector3 p2)
    {
        // If p1 and p2 are the same point, distance is trivial
        if (p1 == p2) return Vector3.Distance(pt, p1);

        // standard formula
        float t = Vector3.Dot(pt - p1, p2 - p1) / (p2 - p1).sqrMagnitude;
        t = Mathf.Clamp01(t);
        Vector3 proj = p1 + t * (p2 - p1);
        return Vector3.Distance(pt, proj);
    }

    /// <summary>
    /// Catmull–Rom interpolation among 'points'.
    /// This ensures the endpoints remain exactly, and yields a smooth curve in-between.
    /// Subdivisions define how many extra points per segment.
    /// 
    /// For N points, we consider segments [i..i+1], but CR needs i-1 and i+2 for tangents.
    /// We clamp edges so the first and last remain fixed.
    /// </summary>
    private List<Vector3> CatmullRomSpline(List<Vector3> points, int subdivisionsPerSeg)
    {
        // If not enough points to curve, just return them
        if (points.Count < 2 || subdivisionsPerSeg < 1)
            return new List<Vector3>(points);

        List<Vector3> result = new List<Vector3>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            // p0: prev or itself if none
            Vector3 p0 = (i == 0) ? points[i] : points[i - 1];
            // p1: current
            Vector3 p1 = points[i];
            // p2: next
            Vector3 p2 = points[i + 1];
            // p3: next next or itself if none
            Vector3 p3 = (i + 2 < points.Count) ? points[i + 2] : points[i + 1];

            // We start with p1 exactly
            if (i == 0)
                result.Add(p1);

            for (int s = 1; s <= subdivisionsPerSeg; s++)
            {
                float t = s / (float)subdivisionsPerSeg;
                Vector3 pos = CatmullRomPos(p0, p1, p2, p3, t);
                result.Add(pos);
            }
        }

        return result;
    }

    /// <summary>
    /// Standard Catmull–Rom formula (alpha=0.5 if you want centripetal).
    /// This version uses uniform CR for simplicity.
    /// Adjust as needed for a "smoother" approach (centripetal).
    /// </summary>
    private Vector3 CatmullRomPos(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // For uniform Catmull–Rom, we can define basis:
        //  (-t^3 + 2t^2 - t) p0 + (3t^3 - 5t^2 + 2) p1 + (-3t^3 + 4t^2 + t) p2 + (t^3 - t^2) p3
        // or we can compute via standard "blend" approach. Let's do a straightforward approach:

        float t2 = t * t;
        float t3 = t2 * t;

        float b0 = 0.5f * (-t3 + 2f * t2 - t);
        float b1 = 0.5f * (3f * t3 - 5f * t2 + 2f);
        float b2 = 0.5f * (-3f * t3 + 4f * t2 + t);
        float b3 = 0.5f * (t3 - t2);

        return (b0 * p0) + (b1 * p1) + (b2 * p2) + (b3 * p3);
    }

    // --------------------------------
    //       Existing Methods
    // --------------------------------

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
