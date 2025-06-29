using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UpdateResourceState : IGameState
{
    private readonly IGameStateMachine _stateMachine;
    private float _downloadProgress;
    private bool _isDownloading;
    private long _totalBytes;
    private long _downloadedBytes;
    
    public GameStateId StateId => GameStateId.UpdateResource;

    public UpdateResourceState(IGameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public async UniTask OnEnterAsync(StateContext context)
    {
        Debug.Log("=== 开始更新资源 ===");
        
        try
        {
            // 从Context获取更新信息
            _totalBytes = context.GetData<long>("UpdateSize", 0);
            Debug.Log($"需要下载的资源大小: {_totalBytes / (1024f * 1024f):F2} MB");
            
            // 显示更新UI
            await ShowUpdateUIAsync();
            
            _downloadProgress = 0f;
            _downloadedBytes = 0;
            _isDownloading = true;
            
            // 开始下载资源
            await DownloadResourcesAsync(context);
            
            Debug.Log("=== 资源更新完成 ===");
            
            // 保存更新完成信息
            context.SetData("UpdateCompleted", true);
            context.SetData("UpdateTime", DateTime.Now);
            
            // 进入预加载状态
            await _stateMachine.ChangeStateAsync(GameStateId.PreloadResource);
        }
        catch (Exception e)
        {
            Debug.LogError($"资源更新失败: {e}");
            
            // 显示错误提示
            await ShowUpdateErrorAsync(e.Message);
            
            // 更新失败，可以选择重试或跳过
            bool shouldRetry = await ShowRetryDialogAsync();
            if (shouldRetry)
            {
                // 重新进入更新状态
                await _stateMachine.ChangeStateAsync(GameStateId.UpdateResource);
            }
            else
            {
                // 跳过更新，进入游戏
                await _stateMachine.ChangeStateAsync(GameStateId.PreloadResource);
            }
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
        
        if (_isDownloading)
        {
            // 更新UI显示下载进度
            UpdateDownloadUI();
        }
    }

    public async UniTask OnExitAsync(StateContext context)
    {
        Debug.Log("退出更新资源状态");
        _isDownloading = false;
        
        // 隐藏更新UI
        await HideUpdateUIAsync();
        
        await UniTask.CompletedTask;
    }

    private async UniTask ShowUpdateUIAsync()
    {
        Debug.Log("显示更新UI");
        // TODO: 显示资源更新UI
        // 例如：UIManager.Instance.ShowPanel("UpdatePanel");
        await UniTask.Delay(100);
    }

    private async UniTask HideUpdateUIAsync()
    {
        Debug.Log("隐藏更新UI");
        // TODO: 隐藏资源更新UI
        await UniTask.Delay(100);
    }

    private async UniTask DownloadResourcesAsync(StateContext context)
    {
        Debug.Log("开始下载资源...");
        
        // TODO: 实际的资源下载逻辑
        
        // 获取需要下载的资源列表
        var downloadList = await GetDownloadListAsync();
        
        // 下载每个资源
        foreach (var resourceInfo in downloadList)
        {
            await DownloadSingleResourceAsync(resourceInfo);
            
            if (!_isDownloading)
            {
                // 下载被中断
                throw new Exception("下载被用户取消");
            }
        }
        
        // 验证下载的资源
        await VerifyDownloadedResourcesAsync();
        
        Debug.Log("资源下载完成");
    }

    private async UniTask<List<ResourceInfo>> GetDownloadListAsync()
    {
        Debug.Log("获取下载列表...");
        
        // TODO: 从服务器获取需要下载的资源列表
        await UniTask.Delay(500);
        
        // 模拟返回资源列表
        return new List<ResourceInfo>
        {
            new ResourceInfo { Name = "config.bundle", Size = 1024 * 1024 },
            new ResourceInfo { Name = "ui.bundle", Size = 5 * 1024 * 1024 },
            new ResourceInfo { Name = "audio.bundle", Size = 10 * 1024 * 1024 },
            new ResourceInfo { Name = "prefabs.bundle", Size = 20 * 1024 * 1024 }
        };
    }

    private async UniTask DownloadSingleResourceAsync(ResourceInfo resourceInfo)
    {
        Debug.Log($"下载资源: {resourceInfo.Name}");
        
        // 模拟下载过程
        long resourceBytes = 0;
        while (resourceBytes < resourceInfo.Size)
        {
            // 模拟下载速度（每次下载1MB）
            long downloadChunk = Math.Min(1024 * 1024, resourceInfo.Size - resourceBytes);
            resourceBytes += downloadChunk;
            _downloadedBytes += downloadChunk;
            
            // 更新进度
            _downloadProgress = (float)_downloadedBytes / _totalBytes;
            _downloadProgress = Mathf.Clamp01(_downloadProgress);
            
            Debug.Log($"下载进度: {_downloadProgress:P} ({_downloadedBytes / (1024f * 1024f):F2}/{_totalBytes / (1024f * 1024f):F2} MB)");
            
            await UniTask.Delay(100); // 模拟下载延迟
        }
        
        Debug.Log($"资源 {resourceInfo.Name} 下载完成");
    }

    private async UniTask VerifyDownloadedResourcesAsync()
    {
        Debug.Log("验证下载的资源...");
        
        // TODO: 验证资源完整性（MD5校验等）
        await UniTask.Delay(500);
        
        Debug.Log("资源验证完成");
    }

    private void UpdateDownloadUI()
    {
        // TODO: 更新下载进度UI
        // 例如：
        // UIManager.Instance.UpdateDownloadProgress(_downloadProgress);
        // UIManager.Instance.UpdateDownloadSpeed(GetDownloadSpeed());
    }

    private async UniTask ShowUpdateErrorAsync(string errorMessage)
    {
        Debug.LogError($"更新错误: {errorMessage}");
        // TODO: 显示错误提示UI
        await UniTask.Delay(1000);
    }

    private async UniTask<bool> ShowRetryDialogAsync()
    {
        Debug.Log("显示重试对话框");
        // TODO: 显示重试确认UI
        await UniTask.Delay(500);
        
        // 模拟用户选择
        return UnityEngine.Random.Range(0f, 1f) < 0.5f;
    }

    private class ResourceInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
    }
}
