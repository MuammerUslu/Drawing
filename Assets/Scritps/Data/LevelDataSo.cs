using System;

namespace Drawing.Data
{
    using UnityEngine;
    using System;

    [Serializable]
    public class LineData
    {
        public Vector3[] points;
    }

    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Level Data")]
    public class LevelDataSo : ScriptableObject
    {
        public LineData[] linePositions;
    }
}