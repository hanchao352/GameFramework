using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

/// <inheritdoc />
public class ResManager : SingletonManager<ResManager>, IGeneric
{
    
    public GameObjectPool GameObjects { get; private set; }
    public AssetPool<Sprite> Sprites { get; private set; }
    public AtlasPool Atlases { get; private set; }
    public AssetPool<AudioClip> Audios { get; private set; }
    public AssetPool<Material> Materials{ get; private set; }
    public AssetPool<TextAsset> TextAssets { get; private set; }
    private readonly Dictionary<Type, IResourcePool> _poolRegistry = 
        new Dictionary<Type, IResourcePool>(8);
    public override void Initialize()
    {
        base.Initialize();
        GameObjects = new GameObjectPool();
        Sprites = new AssetPool<Sprite>();
        Atlases = new AtlasPool();
        Audios = new AssetPool<AudioClip>();
        Materials = new AssetPool<Material>();
        TextAssets = new AssetPool<TextAsset>();
    }
    private void RegisterPool(IResourcePool pool)
    {
        var type = pool.GetType();
        if (!_poolRegistry.ContainsKey(type))
        {
            _poolRegistry.Add(type, pool);
        }
    }
    /// <summary>
    /// 预加载资源
    /// </summary>
    public async UniTask Preload<T>(string path) where T : Object
    {
        if (typeof(T) == typeof(GameObject))
        {
            await GameObjects.GetAsync(path);
        }
        else
        {
            var pool = GetAssetPool<T>();
            if (pool != null) await pool.GetAsync(path);
        }
    }
    // Sprite 获取
    public UniTask<Sprite> GetSpriteAsync(string path) => Sprites.GetAsync(path);
    
    // AudioClip 获取
    public UniTask<AudioClip> GetAudioAsync(string path) => Audios.GetAsync(path);
    
    // Material 获取
    public UniTask<Material> GetMaterialAsync(string path) => Materials.GetAsync(path);
    
    // TextAsset 获取
    public UniTask<TextAsset> GetTextAssetAsync(string path) => TextAssets.GetAsync(path);
    public async UniTask<Sprite> GetSpriteFromAtlasAsync(string atlasPath, string spriteName)
    {
        return await Atlases.GetSpriteAsync(atlasPath, spriteName);
    }
    
    public async UniTask<GameObject> GetGameObjectAsync(string path, 
        Transform parent, bool worldPositionStays = false)
    {
        var instance = await GameObjects.GetAsync(path);
        if (parent != null)
        {
            instance.transform.SetParent(parent, worldPositionStays);
        }
        return instance;
    }
    private IAssetPool<T> GetAssetPool<T>() where T : Object
    {
        if (typeof(T) == typeof(Sprite)) return Sprites as IAssetPool<T>;
        if (typeof(T) == typeof(SpriteAtlas)) return Atlases as IAssetPool<T>;
        if (typeof(T) == typeof(AudioClip)) return Audios as IAssetPool<T>;
        if (typeof(T) == typeof(Material)) return Materials as IAssetPool<T>;
        if (typeof(T) == typeof(TextAsset)) return TextAssets as IAssetPool<T>;
        return null;
    }
    public void ClearAllPools()
    {
        foreach (var pool in _poolRegistry.Values)
        {
            pool.Clear();
        }
    }
    public T GetPool<T>() where T : class, IResourcePool
    {
        return _poolRegistry.TryGetValue(typeof(T), out var pool) ? pool as T : null;
    }
    
    public override void Update(float time)
    {
        base.Update(time);
    }
    
    public override void OnApplicationFocus(bool hasFocus)
    {
        base.OnApplicationFocus(hasFocus);
    }
    
    public override void OnApplicationPause(bool pauseStatus)
    {
        base.OnApplicationPause(pauseStatus);
    }
    
    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }
    
    public override void Dispose()
    {
        base.Dispose();
        ClearAllPools();
    }
    
    
}
