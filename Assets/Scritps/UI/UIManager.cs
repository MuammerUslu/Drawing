using System;
using UnityEngine;
using UnityEngine.UI;

namespace Drawing
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private Button nextLevelButton;

        private void Awake()
        {
            SetUpSingleton();
        }

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
            nextLevelButton.interactable = false;
        }

        public void OpenLevelCompletePanel()
        {
            nextLevelButton.interactable = true;
            nextLevelButton.gameObject.SetActive(true);
        }  
        
        public void SetUpInGameUI()
        {           
            nextLevelButton.gameObject.SetActive(false);
        }
        
        private void SetUpSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}