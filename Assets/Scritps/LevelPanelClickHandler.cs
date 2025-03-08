using UnityEngine;
using UnityEngine.EventSystems;

namespace Drawing
{
    public class LevelPanelClickHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private RectTransform _panel;
        private Camera _uiCamera;

        private void Awake()
        {
            _uiCamera = Camera.main;
            _panel = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Vector3 worldPosition = ConvertToWorldPosition(eventData.position);
            Constants.OnClickPosition?.Invoke(worldPosition);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 worldPosition = ConvertToWorldPosition(eventData.position);
            Constants.OnDragPosition?.Invoke(worldPosition);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Vector3 worldPosition = ConvertToWorldPosition(eventData.position);
            Constants.OnPointerUpPosition?.Invoke(worldPosition);
        }

        private Vector3 ConvertToWorldPosition(Vector2 screenPosition)
        {
            Vector3 worldPosition;
        
            if (_uiCamera != null)
            {
                RectTransformUtility.ScreenPointToWorldPointInRectangle(_panel, screenPosition, _uiCamera, out worldPosition);
            }
            else
            {
                worldPosition = _uiCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
            }

            return worldPosition;
        }
    }
}