using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ExitState : IGameState
{
    public GameStateId StateId => GameStateId.Exit;

    public async UniTask OnEnterAsync(StateContext context)
    {
        Debug.Log("=== 进入退出状态 ===");
        
        try
        {
            
            
            // 获取游戏统计数据
            ShowGameStatistics(context);
            
            // 保存游戏数据
            await SaveGameDataAsync(context);
            
            // 上传统计数据（如果需要）
            await UploadAnalyticsAsync(context);
            
            // 清理所有Manager
            await CleanupManagersAsync(context);
            
            // 清理所有Mod
            await CleanupModsAsync(context);
            
            // 清理资源
            await CleanupResourcesAsync();
            
            // 清理Context
            CleanupContext(context);
            
            Debug.Log("=== 退出流程完成 ===");
            
            // 等待一小段时间确保所有清理完成
            await UniTask.Delay(1000);
            
            // 退出游戏
            QuitGame();
        }
        catch (Exception e)
        {
            Debug.LogError($"退出流程出错: {e}");
            // 即使出错也要退出游戏
            QuitGame();
        }
    }

    public void OnUpdate(float deltaTime, StateContext context)
    {
        // 退出状态不需要更新逻辑
        // 但仍然要更新Manager和Mod，让它们有机会完成最后的工作
        
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
        // 退出状态通常不会再切换到其他状态 ,但是吧 md加上
        Debug.Log("退出ExitState（这通常不应该发生）");
        await UniTask.CompletedTask;
    }

    private async UniTask ShowExitUIAsync()
    {
        Debug.Log("显示退出UI");
        
        await UniTask.Delay(100);
    }

    private void ShowGameStatistics(StateContext context)
    {
        Debug.Log("=== 游戏统计 ===");
        
        // 获取并显示游戏统计数据
        var initTime = context.GetData<DateTime>("InitializeTime");
        var gameStartTime = context.GetData<DateTime>("GameStartTime");
        var playTime = context.GetData<float>("LastPlayTime");
        
        if (initTime != default)
        {
            Debug.Log($"游戏初始化时间: {initTime}");
        }
        
        if (gameStartTime != default)
        {
            Debug.Log($"游戏开始时间: {gameStartTime}");
            var totalTime = DateTime.Now - initTime;
            Debug.Log($"总运行时间: {totalTime.TotalSeconds:F2} 秒");
        }
        
        if (playTime > 0)
        {
            Debug.Log($"游戏时长: {playTime:F2} 秒");
        }
        
        // 显示其他统计信息
        Debug.Log($"使用的Manager数量: {context.Managers?.Count ?? 0}");
        Debug.Log($"使用的Mod数量: {context.Mods?.Count ?? 0}");
    }

    private async UniTask SaveGameDataAsync(StateContext context)
    {
        Debug.Log("保存游戏数据...");
        
        try
        {
            // TODO: 保存玩家进度
            await SavePlayerProgressAsync();
            
            // TODO: 保存游戏设置
            await SaveGameSettingsAsync();
            
            // TODO: 保存统计数据
            await SaveStatisticsAsync(context);
            
            Debug.Log("游戏数据保存完成");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存游戏数据失败: {e}");
            // 继续退出流程，不中断
        }
    }

    private async UniTask SavePlayerProgressAsync()
    {
        Debug.Log("保存玩家进度...");
        // TODO: 实现玩家进度保存
        await UniTask.Delay(100);
    }

    private async UniTask SaveGameSettingsAsync()
    {
        Debug.Log("保存游戏设置...");
        // TODO: 保存音量、画质等设置
        await UniTask.Delay(100);
    }

    private async UniTask SaveStatisticsAsync(StateContext context)
    {
        Debug.Log("保存统计数据...");
        // TODO: 保存游戏统计数据
        await UniTask.Delay(100);
    }

    private async UniTask UploadAnalyticsAsync(StateContext context)
    {
        Debug.Log("上传分析数据...");
        
        try
        {
            // TODO: 上传游戏分析数据到服务器
            // 游戏时长、关卡进度、错误日志等
            
            await UniTask.Delay(200);
            Debug.Log("分析数据上传完成");
        }
        catch (Exception e)
        {
            Debug.LogError($"上传分析数据失败: {e}");
            // 不中断退出流程
        }
    }

    private async UniTask CleanupManagersAsync(StateContext context)
    {
        Debug.Log("清理所有Manager...");
        
        if (context.Managers != null)
        {
            // 反向遍历，后初始化的先清理
            for (int i = context.Managers.Count - 1; i >= 0; i--)
            {
                try
                {
                    var manager = context.Managers[i];
                    Debug.Log($"清理Manager: {manager.GetType().Name}");
                    
                    // TODO: 如果Manager有Cleanup方法，调用它
                    // if (manager is ICleanup cleanup)
                    // {
                    //     await cleanup.CleanupAsync();
                    // }
                    
                    await UniTask.Yield();
                }
                catch (Exception e)
                {
                    Debug.LogError($"清理Manager失败: {e}");
                }
            }
        }
        
        Debug.Log("所有Manager清理完成");
    }

    private async UniTask CleanupModsAsync(StateContext context)
    {
        Debug.Log("清理所有Mod...");
        
        if (context.Mods != null)
        {
            // 反向遍历，后初始化的先清理
            for (int i = context.Mods.Count - 1; i >= 0; i--)
            {
                try
                {
                    var mod = context.Mods[i];
                    Debug.Log($"清理Mod: {mod.GetType().Name}");
                    
                    // TODO: 如果Mod有Cleanup方法，调用它
                    // if (mod is ICleanup cleanup)
                    // {
                    //     await cleanup.CleanupAsync();
                    // }
                    
                    await UniTask.Yield();
                }
                catch (Exception e)
                {
                    Debug.LogError($"清理Mod失败: {e}");
                }
            }
        }
        
        Debug.Log("所有Mod清理完成");
    }

    private async UniTask CleanupResourcesAsync()
    {
        Debug.Log("清理游戏资源...");
        
        try
        {
            // 停止所有音频
            AudioListener.pause = true;
            AudioListener.volume = 0f;
            
            // TODO: 释放YooAsset资源
            // YooAssets.UnloadUnusedAssets();
            
            // 卸载所有场景（除了当前场景）
            
            
            // 强制垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            // 卸载未使用的资源
            await Resources.UnloadUnusedAssets();
            
            Debug.Log("游戏资源清理完成");
        }
        catch (Exception e)
        {
            Debug.LogError($"清理资源失败: {e}");
        }
    }

    private void CleanupContext(StateContext context)
    {
        Debug.Log("清理状态上下文...");
        
        try
        {
            // 清空Context中的所有数据
            context.Clear();
            Debug.Log("状态上下文清理完成");
        }
        catch (Exception e)
        {
            Debug.LogError($"清理状态上下文失败: {e}");
        }
    }

    private void QuitGame()
    {
        Debug.Log("退出游戏");
        
#if UNITY_EDITOR
        Debug.Log("在编辑器中停止播放");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("退出应用程序");
        Application.Quit();
#endif
    }
}
