using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

public sealed class GameObjectPool : IGameObjectPool
{
    // 资源路径 -> 预制体缓存
    private readonly Dictionary<string, GameObject> _prefabCache = 
        new Dictionary<string, GameObject>(32, StringComparer.Ordinal);
    
    // 资源路径 -> 对象池
    private readonly Dictionary<string, Stack<GameObject>> _instancePools = 
        new Dictionary<string, Stack<GameObject>>(32, StringComparer.Ordinal);
    
    // 实例 -> 资源路径
    private readonly Dictionary<GameObject, string> _instanceToPath = 
        new Dictionary<GameObject, string>();
    
    // 资源路径 -> 资源句柄
    private readonly Dictionary<string, AssetHandle> _assetHandles = 
        new Dictionary<string, AssetHandle>(32, StringComparer.Ordinal);

    public int ResourceCount => _prefabCache.Count;

    // 单参数重载
    public UniTask<GameObject> GetAsync(string path) => 
        GetAsync(path, null, false);

    public async UniTask<GameObject> GetAsync(string path, Transform parent, bool worldPositionStays)
    {
        // 尝试从对象池获取可用实例
        if (TryGetFromPool(path, out var instance))
        {
            SetupInstance(instance, parent, worldPositionStays);
            return instance;
        }

        // 加载预制体
        var prefab = await LoadPrefabAsync(path);
        
        // 创建新实例
        instance = Object.Instantiate(prefab);
        _instanceToPath[instance] = path;
        
        SetupInstance(instance, parent, worldPositionStays);
        return instance;
    }

    public void ReleaseInstance(GameObject instance)
    {
        if (instance == null) return;
        
        if (!_instanceToPath.TryGetValue(instance, out var path))
        {
            Object.Destroy(instance);
            return;
        }

        // 回收实例
        instance.SetActive(false);
        instance.transform.SetParent(null);
        
        if (!_instancePools.TryGetValue(path, out var pool))
        {
            pool = new Stack<GameObject>(4);
            _instancePools[path] = pool;
        }
        
        pool.Push(instance);
    }

    public void Release(string path)
    {
        if (_assetHandles.TryGetValue(path, out var handle))
        {
            handle.Release();
            _assetHandles.Remove(path);
        }
        
        _prefabCache.Remove(path);
        
        if (_instancePools.TryGetValue(path, out var pool))
        {
            while (pool.Count > 0)
            {
                var instance = pool.Pop();
                if (instance != null) Object.Destroy(instance);
            }
            _instancePools.Remove(path);
        }
    }

    public void Clear()
    {
        foreach (var handle in _assetHandles.Values)
        {
            handle.Release();
        }
        
        foreach (var pool in _instancePools.Values)
        {
            while (pool.Count > 0)
            {
                var instance = pool.Pop();
                if (instance != null) Object.Destroy(instance);
            }
        }
        
        _prefabCache.Clear();
        _assetHandles.Clear();
        _instancePools.Clear();
        _instanceToPath.Clear();
    }

    #region Private Methods
    private bool TryGetFromPool(string path, out GameObject instance)
    {
        if (_instancePools.TryGetValue(path, out var pool) && pool.Count > 0)
        {
            instance = pool.Pop();
            return true;
        }
        
        instance = null;
        return false;
    }

    private async UniTask<GameObject> LoadPrefabAsync(string path)
    {
        if (_prefabCache.TryGetValue(path, out var prefab))
        {
            return prefab;
        }

        var handle = YooAssets.LoadAssetAsync<GameObject>(path);
        await handle.Task;
        
        if (handle.Status != EOperationStatus.Succeed)
        {
            throw new Exception($"Failed to load prefab: {path}");
        }
        
        prefab = handle.AssetObject as GameObject;
        _prefabCache[path] = prefab;
        _assetHandles[path] = handle;
        
        return prefab;
    }

    private void SetupInstance(GameObject instance, Transform parent, bool worldPositionStays)
    {
        instance.SetActive(true);
        
        if (parent != null)
        {
            instance.transform.SetParent(parent, worldPositionStays);
        }
    }
    #endregion
}