using System;
using System.Collections;
using System.Collections.Generic;
using Drawing.Data;
using UnityEngine;

namespace Drawing
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] LineRenderer lineRendererPrefab;

        public static GameManager Instance { get; private set; }

        private AddressableLevelLoader _addressableLevelLoader;
        private GameLogic _gameLogic;

        private const int TotalLevelCount = 5;

        private LevelDataSo _currentLevelData;

        private void Awake()
        {
            SetUpSingleton();
            _addressableLevelLoader = new AddressableLevelLoader();
            _gameLogic = new GameLogic(lineRendererPrefab, transform);
        }

        private void Start()
        {
            LoadedLevel();
        }

        private void OnEnable()
        {
            _gameLogic?.Subscribe();
        }

        private void OnDisable()
        {
            _gameLogic?.Unsubscribe();
        }

        public void CompleteLevel()
        {
            int currentLevel = PlayerPrefs.GetInt("CurrentLevel");
            currentLevel++;
            PlayerPrefs.SetInt("CurrentLevel", currentLevel);
            
            UIManager.Instance.OpenLevelCompletePanel();
        }

        public void LoadedLevel()
        {
            int currentLevel = PlayerPrefs.GetInt("CurrentLevel");
            currentLevel = 1 + currentLevel % TotalLevelCount;

            _addressableLevelLoader.UnloadAll();

            _addressableLevelLoader.LoadLevel(currentLevel, (level) =>
            {
                _gameLogic.LoadLevel(level);
                UIManager.Instance.SetUpInGameUI();
            });
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


#if UNITY_EDITOR

        [ContextMenu("Add and Load Level")]
        public void AddLevel()
        {
            int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            currentLevel++;
            PlayerPrefs.SetInt("CurrentLevel", currentLevel);

            LoadedLevel();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Break();
            }
        }

#endif
    }
}