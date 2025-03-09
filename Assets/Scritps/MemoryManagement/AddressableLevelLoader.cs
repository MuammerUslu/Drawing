using System;
using Drawing.Data;
using UnityEngine;

namespace Drawing
{
    public class AddressableLevelLoader
    {
        private readonly AddressableAssetLoader<LevelDataSo> _assetLoader = new();

        public void LoadLevel(int level, Action<LevelDataSo> onComplete)
        {
            string path = GetPath(level);
            _assetLoader.LoadAsset(path, onComplete);
        }

        public void UnloadLevel(int level)
        {
            string path = GetPath(level);
            _assetLoader.UnloadAsset(path);
        }

        public void UnloadAll()
        {
            _assetLoader.UnloadAll();
        }

        private string GetPath(int level)
        {
            return $"Assets/RawData/Levels/Level{level}.asset";
        }
    }
}