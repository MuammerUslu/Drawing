using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Drawing
{
    public class AddressableAssetLoader<T>
    {
        private readonly Dictionary<string, AsyncOperationHandle> loadedAssets = new();

        public void LoadAsset(string path, Action<T> onComplete)
        {
            if (loadedAssets.TryGetValue(path, out var existingHandle))
            {
                // already loaded
                onComplete?.Invoke((T)existingHandle.Result);
                return;
            }

            // loading
            var handle = Addressables.LoadAssetAsync<T>(path);
            loadedAssets[path] = handle;
            handle.Completed += (operation) =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    onComplete?.Invoke(operation.Result);
                }
                else
                {
                    Debug.LogError($"Failed to load asset at: {path}");
                    loadedAssets.Remove(path);
                    onComplete?.Invoke(default);
                }
            };
        }

        public void LoadAssetBlocking(string path, Action<T> onComplete)
        {
            if (loadedAssets.TryGetValue(path, out var existingHandle))
            {
                // already loaded
                onComplete?.Invoke((T)existingHandle.Result);
                return;
            }

            // loading synchronously (blocking the thread)
            var handle = Addressables.LoadAssetAsync<T>(path);
            handle.WaitForCompletion(); // This blocks until the load is complete

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedAssets[path] = handle;
                onComplete?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogError($"Failed to load asset at: {path}");
                onComplete?.Invoke(default);
            }
        }

        public void UnloadAsset(string path)
        {
            if (loadedAssets.TryGetValue(path, out var handle))
            {
                Addressables.Release(handle);
                loadedAssets.Remove(path);
                Debug.Log($"Unloaded asset at: {path}");
            }
            else
            {
                Debug.LogWarning($"Asset at {path} is not loaded.");
            }
        }

        public void UnloadAll()
        {
            foreach (var handle in loadedAssets.Values)
            {
                Addressables.Release(handle);
            }

            loadedAssets.Clear();
            Debug.Log("Unloaded all loaded assets.");
        }
    }
}