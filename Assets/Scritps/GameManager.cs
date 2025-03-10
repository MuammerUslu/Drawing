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
        private LevelHandler _levelHandler;

        private const int TotalLevelCount = 3;

        private LevelDataSo _currentLevelData;

        private void Awake()
        {
            SetUpSingleton();
            _addressableLevelLoader = new AddressableLevelLoader();
            _levelHandler = new LevelHandler(lineRendererPrefab, transform);
        }

        private void Start()
        {
            LoadedLevel();
        }

        private void OnEnable()
        {
            _levelHandler?.Subscribe();
        }

        private void OnDisable()
        {
            _levelHandler?.Unsubscribe();
        }

        public void LoadNextLevel()
        {
            int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            currentLevel++;
            PlayerPrefs.SetInt("CurrentLevel", currentLevel);

            LoadedLevel();
        }

        public void CompleteLevel()
        {
            UIManager.Instance.OpenLevelCompletePanel();
        }

        private void LoadedLevel()
        {
            int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            currentLevel = 1 + currentLevel % TotalLevelCount;

            _addressableLevelLoader.UnloadAll();

            _addressableLevelLoader.LoadLevel(currentLevel, (level) =>
            {
                _levelHandler.LoadLevel(level);
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