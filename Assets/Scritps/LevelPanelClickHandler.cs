using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Drawing
{
    public class LevelPanelClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private RectTransform _panel;
        private Camera _uiCamera;

        private void Awake()
        {
            _uiCamera = Camera.main;
            _panel = GetComponent<RectTransform>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Vector3 worldPosition = ConvertToWorldPosition(eventData.position);

            Constants.OnClickPosition.Invoke(worldPosition);
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