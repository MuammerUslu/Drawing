using System;
using UnityEngine;
using UnityEngine.UI;

namespace Drawing
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private Button nextLevelButton;

        private void OnEnable()
        {
            nextLevelButton.onClick.AddListener(OnClickNextButton);
        }

        private void OnDisable()
        {
            nextLevelButton.onClick.RemoveListener(OnClickNextButton);
        }

        private void OnClickNextButton()
        {
            GameManager.Instance.LoadNextLevel();
        }
    }
}