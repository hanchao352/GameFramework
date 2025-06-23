using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using YooAsset;

public sealed class AtlasPool : IAtlasPool
{
    // 图集缓存
    private readonly Dictionary<string, SpriteAtlas> _atlasCache = 
        new Dictionary<string, SpriteAtlas>(32, StringComparer.Ordinal);
    
    // 资源句柄
    private readonly Dictionary<string, AssetHandle> _assetHandles = 
        new Dictionary<string, AssetHandle>(32, StringComparer.Ordinal);
    
    // 图集加载任务缓存（避免重复加载）
    private readonly Dictionary<string, UniTask<SpriteAtlas>> _loadingTasks = 
        new Dictionary<string, UniTask<SpriteAtlas>>(32, StringComparer.Ordinal);

    public int ResourceCount => _atlasCache.Count;

    public UniTask<SpriteAtlas> GetAsync(string path)
    {
        // 如果图集已加载，直接返回
        if (_atlasCache.TryGetValue(path, out var atlas))
        {
            return UniTask.FromResult(atlas);
        }
        
        // 如果有正在进行的加载任务，返回该任务
        if (_loadingTasks.TryGetValue(path, out var loadingTask))
        {
            return loadingTask;
        }
        
        // 创建新的加载任务
        var task = LoadAtlasAsync(path);
        _loadingTasks[path] = task;
        return task;
    }

    public UniTask<Sprite> GetSpriteAsync(string atlasPath, string spriteName)
    {
        // 如果图集已加载，直接获取Sprite
        if (_atlasCache.TryGetValue(atlasPath, out var atlas))
        {
            return UniTask.FromResult(GetSpriteFromAtlas(atlas, spriteName));
        }
        
        // 否则加载图集后获取Sprite
        return GetSpriteAfterLoadingAsync(atlasPath, spriteName);
    }

    private async UniTask<Sprite> GetSpriteAfterLoadingAsync(string atlasPath, string spriteName)
    {
        // 加载图集
        var atlas = await GetAsync(atlasPath);
        
        // 从图集中获取Sprite
        return GetSpriteFromAtlas(atlas, spriteName);
    }

    public void Release(string path)
    {
        if (_assetHandles.TryGetValue(path, out var handle))
        {
            handle.Release();
            _assetHandles.Remove(path);
        }
        
        _atlasCache.Remove(path);
        _loadingTasks.Remove(path);
    }

    public void Clear()
    {
        foreach (var handle in _assetHandles.Values)
        {
            handle.Release();
        }
        
        _atlasCache.Clear();
        _assetHandles.Clear();
        _loadingTasks.Clear();
    }

    #region Private Methods
    private async UniTask<SpriteAtlas> LoadAtlasAsync(string path)
    {
        try
        {
            var handle = YooAssets.LoadAssetAsync<SpriteAtlas>(path);
            await handle.Task;
            
            if (handle.Status != EOperationStatus.Succeed)
            {
                throw new Exception($"Failed to load SpriteAtlas: {path}");
            }
            
            var atlas = handle.AssetObject as SpriteAtlas;
            _atlasCache[path] = atlas;
            _assetHandles[path] = handle;
            
            return atlas;
        }
        finally
        {
            // 加载完成后移除任务缓存
            _loadingTasks.Remove(path);
        }
    }

    private Sprite GetSpriteFromAtlas(SpriteAtlas atlas, string spriteName)
    {
        var sprite = atlas.GetSprite(spriteName);
        if (sprite == null)
        {
            throw new KeyNotFoundException($"Sprite '{spriteName}' not found in atlas '{atlas.name}'");
        }
        return sprite;
    }
    #endregion
}