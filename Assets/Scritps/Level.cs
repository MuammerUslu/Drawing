using UnityEngine;

namespace Drawing
{
    public class Level : MonoBehaviour
    {
        [SerializeField] private LineRenderer[] lineRenderers;
        [SerializeField]  LayerMask raycastLayer;

        private LevelExtractor _levelExtractor;

        private void OnValidate()
        {
            lineRenderers = GetComponentsInChildren<LineRenderer>();
        }

        public void Start()
        {
            _levelExtractor = new LevelExtractor();
            _levelExtractor.ExtractLevelData(lineRenderers);
        }

        private void OnEnable()
        {
            Constants.OnClickPosition += FindClosestNode;
        }

        private void OnDisable()
        {
            Constants.OnClickPosition += FindClosestNode;
        }

        private void FindClosestNode(Vector3 worldPosition)
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

            if (closestNode != null)
            {
                Debug.Log("Closest Node Position: " + closestNode.Position);
            }
        }
    }
}