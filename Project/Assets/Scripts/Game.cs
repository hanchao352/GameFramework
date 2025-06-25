using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using YooAsset;

public class Game : MonoBehaviour
{
    // 在编辑器中设置这些参数
    public string packageName = "DefaultPackage"; // 资源包名称
    public string assetPath = "Assets/Res/UI/window";  // 预制体资源路径

    private ResourcePackage _package; // YooAsset资源包


    IEnumerator Start()
    {
        DontDestroyOnLoad(this);
        // 1. 初始化YooAsset
        yield return InitializeYooAsset();
        
        // 2. 加载起始场景
        yield return ChangeScene();
        
        // 3. 示例：加载并实例化预制体
        yield return LoadAndInstantiatePrefab(assetPath);

        yield return LoadSprite();
    }

    private IEnumerator LoadSprite()
    {
        string location = "Assets/Res/Atlas/test.spriteatlas";
        AssetHandle handle = _package.LoadAssetAsync<SpriteAtlas>(location);
        yield return handle;
        
        // 检查加载结果
        if (handle.Status == EOperationStatus.Succeed)
        {
            Debug.Log($"图集加载成功:");
            var spriteAtals = handle.AssetObject as SpriteAtlas;
            Sprite sprite = spriteAtals.GetSprite("主角 拆分 拷贝");
            var img = GameObject.Find("Canvas/window/Image").GetComponent<Image>();
            img.sprite = sprite;
            img.SetNativeSize();
        }
        else
        {
            Debug.LogError($"图集加载失败, 错误: {handle.LastError}");
        }
    
    }
    
    #region YooAsset初始化
    /// <summary>
    /// 初始化YooAsset资源系统
    /// </summary>
    private IEnumerator InitializeYooAsset()
    {
        // 初始化资源系统
        YooAssets.Initialize();
        _package = YooAssets.CreatePackage(packageName);
        // 设置该资源包为默认的资源包
        YooAssets.SetDefaultPackage(_package);
        // 编辑器模拟模式
        var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);    
        var packageRoot = buildResult.PackageRootDirectory;
        var editorFileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
        var initParameters = new EditorSimulateModeParameters();
        initParameters.EditorFileSystemParameters = editorFileSystemParams;
        var initOperation = _package.InitializeAsync(initParameters);
        yield return initOperation;
    
        if(initOperation.Status == EOperationStatus.Succeed)
            Debug.Log("资源包初始化成功！");
        else 
            Debug.LogError($"资源包初始化失败：{initOperation.Error}");
        
        // 请求最新的资源版本信息
        var requestOperation = _package.RequestPackageVersionAsync();
        Debug.Log("正在请求最新的资源版本...");
        yield return requestOperation;

        string packageVersion = "";
        if (requestOperation.Status == EOperationStatus.Succeed)
        {
            packageVersion = requestOperation.PackageVersion;
            Debug.Log($"获取最新资源版本成功: {packageVersion}");
        }
        else
        {
            Debug.LogError("获取资源版本失败：" + requestOperation.Error);
        }

        // 更新资源清单
        var updateOperation = _package.UpdatePackageManifestAsync(packageVersion);
        yield return updateOperation;

        if (updateOperation.Status == EOperationStatus.Succeed)
        {
            Debug.Log("资源清单更新成功");
        }
        else
        {
            Debug.LogError("资源清单更新失败：" + updateOperation.Error);
        }

    }
    #endregion

    #region 场景管理
    /// <summary>
    /// 切换场景
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public IEnumerator ChangeScene()
    {
        Debug.Log($"开始加载场景");
        string location = "Assets/Res/Scene/TestLoadScene";
        var sceneMode = LoadSceneMode.Single;
        var physicsMode = LocalPhysicsMode.None;
        bool suspendLoad = false;
        SceneHandle handle = _package.LoadSceneAsync(location, sceneMode, physicsMode, suspendLoad);
        yield return handle;
        // 检查加载结果
        if (handle.Status == EOperationStatus.Succeed)
        {
            Debug.Log($"场景加载成功:");
            handle.Release();
        }
        else
        {
            Debug.LogError($"场景加载失败, 错误: {handle.LastError}");
        }
    }
    #endregion

    #region 预制体加载
    /// <summary>
    /// 加载并实例化预制体
    /// </summary>
    /// <param name="assetPath">资源路径</param>
    public IEnumerator LoadAndInstantiatePrefab(string assetPath)
    {
        Debug.Log($"开始加载预制体: {assetPath}");
        
        // 创建资源加载任务
        var assetHandle = _package.LoadAssetAsync<GameObject>(assetPath);
        
        // 等待预制体加载完成
        yield return assetHandle;
        
        if (assetHandle.Status == EOperationStatus.Succeed)
        {
            var root = GameObject.Find("Canvas").transform;
            // 实例化游戏对象
            GameObject prefab = assetHandle.AssetObject as GameObject;
            if (prefab != null)
            {
                var go = Instantiate(prefab,root);
                go.name = "window";
                Debug.Log($"预制体实例化成功: {assetPath}");
            }
        }
        else
        {
            Debug.LogError($"预制体加载失败: {assetPath}, {assetHandle.LastError}");
        }
        
        // 释放资源句柄（实例化后不再需要）
        assetHandle.Release();
    }
    #endregion

    // 清理资源
    private void OnDestroy()
    {
        if (_package != null)
        {
            _package.UnloadAllAssetsAsync();
            _package = null;
        }
    }
}