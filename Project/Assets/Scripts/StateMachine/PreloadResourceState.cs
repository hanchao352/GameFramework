using System;
using System.Collections.Generic;
using cfg.Bean;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PreloadResourceState : IGameState
{
    private readonly IGameStateMachine _stateMachine;
    private float _loadProgress;
    private int _totalResources;
    private int _loadedResources;
    
    public GameStateId StateId => GameStateId.PreloadResource;

    public PreloadResourceState(IGameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public async UniTask OnEnterAsync(StateContext context)
    {
        Debug.Log("=== 开始预加载资源 ===");
        
        try
        {
            await TableManager.Instance.LoadConfiguration();
            // 显示加载UI
            await ShowLoadingUIAsync();
            
            _loadProgress = 0f;
            _loadedResources = 0;
            
            // 获取所有需要预加载的资源
            var resourcesToLoad = GetPreloadResourceList();
            _totalResources = resourcesToLoad.Count;
            
            // 预加载必要的游戏资源
            await PreloadEssentialResourcesAsync(resourcesToLoad);
            
            // 初始化游戏数据
            await InitializeGameDataAsync(context);
            
            Debug.Log("=== 预加载完成 ===");
            
            // 保存预加载信息
            context.SetData("PreloadCompleted", true);
            context.SetData("PreloadTime", DateTime.Now);
            
            // 等待一小段时间，确保UI显示完整
            await UniTask.Delay(500);
            
            // 进入游戏状态
            await _stateMachine.ChangeStateAsync(GameStateId.InGame);
        }
        catch (Exception e)
        {
            Debug.LogError($"预加载资源失败: {e}");
            
            // 显示错误提示
            await ShowLoadErrorAsync(e.Message);
            
            // 预加载失败，退出游戏
            await _stateMachine.ChangeStateAsync(GameStateId.Exit);
        }
    }

    public void OnUpdate(float deltaTime, StateContext context)
    {
        // 更新所有Manager
        if (context.Managers != null)
        {
            foreach (var manager in context.Managers)
            {
                try
                {
                    manager.Update(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Manager Update失败 - {manager.GetType().Name}: {e.Message}");
                }
            }
        }
        
        // 更新所有Mod
        if (context.Mods != null)
        {
            foreach (var mod in context.Mods)
            {
                try
                {
                    mod.Update(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Mod Update失败 - {mod.GetType().Name}: {e.Message}");
                }
            }
        }
        
        // 更新加载进度UI
        UpdateLoadingUI();
    }

    public async UniTask OnExitAsync(StateContext context)
    {
        Debug.Log("退出预加载资源状态");
        
        // 隐藏加载UI
        await HideLoadingUIAsync();
        
        await UniTask.CompletedTask;
    }

    private List<string> GetPreloadResourceList()
    {
        // 返回需要预加载的资源列表
        return new List<string>
        {
            // UI资源
            "UI/MainMenu",
            
            // 音频资源
            "Audio/BGM/MainTheme",
           
            
            // 配置文件
            "Config/GameConfig",
            
            // 核心预制体
            "Prefabs/Player",
            // 材质和贴图
            "Materials/Common",
            "Textures/Icons"
        };
    }

    private async UniTask PreloadEssentialResourcesAsync(List<string> resourcesToLoad)
    {
        Debug.Log($"开始预加载 {resourcesToLoad.Count} 个资源");
        
        // 分类加载资源
        await LoadUIResourcesAsync(resourcesToLoad);
        await LoadAudioResourcesAsync(resourcesToLoad);
        await LoadConfigResourcesAsync(resourcesToLoad);
        await LoadPrefabResourcesAsync(resourcesToLoad);
        await LoadMaterialResourcesAsync(resourcesToLoad);
    }

    private async UniTask LoadUIResourcesAsync(List<string> resources)
    {
        Debug.Log("预加载UI资源...");
        
        var uiResources = resources.FindAll(r => r.StartsWith("UI/"));
        foreach (var resource in uiResources)
        {
            await LoadSingleResourceAsync(resource);
        }
    }

    private async UniTask LoadAudioResourcesAsync(List<string> resources)
    {
        Debug.Log("预加载音频资源...");
        
        var audioResources = resources.FindAll(r => r.StartsWith("Audio/"));
        foreach (var resource in audioResources)
        {
            await LoadSingleResourceAsync(resource);
        }
    }

    private async UniTask LoadConfigResourcesAsync(List<string> resources)
    {
        Debug.Log("预加载配置文件...");
        
        var configResources = resources.FindAll(r => r.StartsWith("Config/"));
        foreach (var resource in configResources)
        {
            await LoadSingleResourceAsync(resource);
        }
    }

    private async UniTask LoadPrefabResourcesAsync(List<string> resources)
    {
        Debug.Log("预加载预制体资源...");
        
        var prefabResources = resources.FindAll(r => r.StartsWith("Prefabs/"));
        foreach (var resource in prefabResources)
        {
            await LoadSingleResourceAsync(resource);
        }
    }

    private async UniTask LoadMaterialResourcesAsync(List<string> resources)
    {
        Debug.Log("预加载材质资源...");
        
        var materialResources = resources.FindAll(r => r.StartsWith("Materials/") || r.StartsWith("Textures/"));
        foreach (var resource in materialResources)
        {
            await LoadSingleResourceAsync(resource);
        }
    }

    private async UniTask LoadSingleResourceAsync(string resourcePath)
    {
        try
        {
            Debug.Log($"加载资源: {resourcePath}");
            
            // TODO: 使用YooAsset或其他资源管理系统加载资源
            // var handle = YooAssets.LoadAssetAsync<UnityEngine.Object>(resourcePath);
            // await handle.ToUniTask();
            
            // 模拟加载延迟
            await UniTask.Delay(50);
            
            _loadedResources++;
            _loadProgress = (float)_loadedResources / _totalResources;
            
            Debug.Log($"资源加载进度: {_loadProgress:P} ({_loadedResources}/{_totalResources})");
        }
        catch (Exception e)
        {
            Debug.LogError($"加载资源失败 {resourcePath}: {e}");
            throw;
        }
    }

    private async UniTask InitializeGameDataAsync(StateContext context)
    {
        Debug.Log("初始化游戏数据...");
        
        // 加载游戏配置
        await LoadGameConfigAsync();
        
        // 将游戏数据保存到Context
        context.SetData("GameInitialized", true);
        
        Debug.Log("游戏数据初始化完成");
    }

    private async UniTask LoadGameConfigAsync()
    {
        Debug.Log("加载游戏配置...");
        // TODO: 加载游戏配置文件
        await UniTask.Delay(100);
    }

    

    private async UniTask ShowLoadingUIAsync()
    {
        Debug.Log("显示加载UI");
        // TODO: 显示加载界面
        await UniTask.Delay(100);
    }

    private async UniTask HideLoadingUIAsync()
    {
        Debug.Log("隐藏加载UI");
        // TODO: 隐藏加载界面
        await UniTask.Delay(100);
    }

    private void UpdateLoadingUI()
    {
        // TODO: 更新加载进度UI
        // 例如：UIManager.Instance.UpdateLoadingProgress(_loadProgress);
    }

    private async UniTask ShowLoadErrorAsync(string errorMessage)
    {
        Debug.LogError($"加载错误: {errorMessage}");
        // TODO: 显示加载错误提示
        await UniTask.Delay(2000);
    }
}
