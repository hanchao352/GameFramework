using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using YooAsset;

public sealed class AssetPool<T> : IAssetPool<T> where T : UnityEngine.Object
{
    private readonly Dictionary<string, T> _assetCache = 
        new Dictionary<string, T>(64, StringComparer.Ordinal);
    
    private readonly Dictionary<string, AssetHandle> _assetHandles = 
        new Dictionary<string, AssetHandle>(64, StringComparer.Ordinal);

    public int ResourceCount => _assetCache.Count;

    public UniTask<T> GetAsync(string path)
    {
        if (_assetCache.TryGetValue(path, out var asset))
        {
            return UniTask.FromResult(asset);
        }
        return LoadAssetAsync(path);
    }

    private async UniTask<T> LoadAssetAsync(string path)
    {
        var handle = YooAssets.LoadAssetAsync<T>(path);
        await handle.Task;
        
        if (handle.Status != EOperationStatus.Succeed)
        {
            throw new Exception($"Failed to load {typeof(T).Name}: {path}");
        }
        
        var asset = handle.AssetObject as T;
        _assetCache[path] = asset;
        _assetHandles[path] = handle;
        
        return asset;
    }

    public void Release(string path)
    {
        if (_assetHandles.TryGetValue(path, out var handle))
        {
            handle.Release();
            _assetHandles.Remove(path);
        }
        _assetCache.Remove(path);
    }

    public void Clear()
    {
        foreach (var handle in _assetHandles.Values)
        {
            handle.Release();
        }
        _assetCache.Clear();
        _assetHandles.Clear();
    }
}