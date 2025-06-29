using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CheckUpdateState : IGameState
{
    private readonly IGameStateMachine _stateMachine;
    private bool _hasUpdate;
    
    public GameStateId StateId => GameStateId.CheckUpdate;

    public CheckUpdateState(IGameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public async UniTask OnEnterAsync(StateContext context)
    {
        Debug.Log("=== 开始检查更新 ===");
        
        try
        {
            // 显示检查更新UI
            await ShowCheckUpdateUIAsync();
            
            // 检查版本
            _hasUpdate = await CheckVersionAsync();
            
            // 确保在主线程
            await UniTask.SwitchToMainThread();
            
            if (_hasUpdate)
            {
                Debug.Log("发现新版本，需要更新");
                
                // 可以在这里弹出更新提示
                bool shouldUpdate = await ShowUpdateConfirmDialogAsync();
                
                if (shouldUpdate)
                {
                    // 保存更新信息到Context
                    context.SetData("NeedUpdate", true);
                    context.SetData("UpdateSize", GetUpdateSize());
                    
                    await _stateMachine.ChangeStateAsync(GameStateId.UpdateResource);
                }
                else
                {
                    // 用户选择跳过更新
                    Debug.Log("用户选择跳过更新");
                    await _stateMachine.ChangeStateAsync(GameStateId.PreloadResource);
                }
            }
            else
            {
                Debug.Log("当前已是最新版本");
                await _stateMachine.ChangeStateAsync(GameStateId.PreloadResource);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"检查更新失败: {e}");
            // 检查更新失败，继续进入游戏
            await _stateMachine.ChangeStateAsync(GameStateId.PreloadResource);
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
    }

    public async UniTask OnExitAsync(StateContext context)
    {
        Debug.Log("退出检查更新状态");
        
        // 隐藏检查更新UI
        await HideCheckUpdateUIAsync();
        
        await UniTask.CompletedTask;
    }

    private async UniTask ShowCheckUpdateUIAsync()
    {
        Debug.Log("显示检查更新UI");
        // TODO: 显示检查更新的UI界面
        await UniTask.Delay(100);
    }

    private async UniTask HideCheckUpdateUIAsync()
    {
        Debug.Log("隐藏检查更新UI");
        // TODO: 隐藏检查更新的UI界面
        await UniTask.Delay(100);
    }

    private async UniTask<bool> CheckVersionAsync()
    {
        Debug.Log("正在检查版本信息...");
        
        try
        {
            // TODO: 实际的版本检查逻辑
            // 1. 获取本地版本号
            string localVersion = Application.version;
            Debug.Log($"本地版本: {localVersion}");
            
            // 2. 请求服务器版本号
            string remoteVersion = await GetRemoteVersionAsync();
            Debug.Log($"远程版本: {remoteVersion}");
            
            // 3. 比较版本
            bool needUpdate = CompareVersion(localVersion, remoteVersion);
            
            return needUpdate;
        }
        catch (Exception e)
        {
            Debug.LogError($"版本检查失败: {e}");
            return false;
        }
    }

    private async UniTask<string> GetRemoteVersionAsync()
    {
        // TODO: 从服务器获取版本信息
        // 这里模拟网络请求
        await UniTask.Delay(1000);
        
        // 模拟返回一个版本号
        return "1.0.1";
    }

    private bool CompareVersion(string localVersion, string remoteVersion)
    {
        try
        {
            var local = new Version(localVersion);
            var remote = new Version(remoteVersion);
            return remote > local;
        }
        catch
        {
            // 版本号格式错误，默认不需要更新
            return false;
        }
    }

    private async UniTask<bool> ShowUpdateConfirmDialogAsync()
    {
        Debug.Log("显示更新确认对话框");
        
        // TODO: 显示更新确认UI
        // 这里模拟用户选择
        await UniTask.Delay(500);
        
        // 模拟有80%的概率选择更新
        return UnityEngine.Random.Range(0f, 1f) < 0.8f;
    }

    private long GetUpdateSize()
    {
        // TODO: 获取实际的更新包大小
        // 这里返回模拟值（100MB）
        return 100 * 1024 * 1024;
    }
}
