using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// 游戏中状态
/// </summary>
public class InGameState : IGameState
{
    private readonly IGameStateMachine _stateMachine;
    private bool _isGameRunning;
    private bool _isPaused;
    private float _gameStartTime;
    
    public GameStateId StateId => GameStateId.InGame;

    public InGameState(IGameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public async UniTask OnEnterAsync(StateContext context)
    {
        Debug.Log("=== 进入游戏状态 ===");
        
        try
        {
            // 从Context获取初始化信息
            var initTime = context.GetData<DateTime>("InitializeTime");
            var preloadTime = context.GetData<DateTime>("PreloadTime");
            Debug.Log($"游戏初始化时间: {initTime}");
            Debug.Log($"预加载完成时间: {preloadTime}");
            
            _isGameRunning = true;
            _isPaused = false;
            _gameStartTime = Time.time;
            
            // 加载游戏场景
            await LoadGameSceneAsync();
            
            // 初始化游戏逻辑
            await InitializeGameLogicAsync(context);
            
            Debug.Log("游戏开始运行");
            UIManager.Instance.ShowView(UIName.UIMain);
            context.SetData("GameStartTime", DateTime.Now);
        }
        catch (Exception e)
        {
            Debug.LogError($"进入游戏状态失败: {e}");
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
        
        if (_isGameRunning && !_isPaused)
        {
            // 更新游戏逻辑
            UpdateGameLogic(deltaTime, context);
            
            // 检查游戏输入
            CheckGameInput(context);
        }
        
        // 检查系统输入（即使暂停也要检查）
        CheckSystemInput(context);
    }

    public async UniTask OnExitAsync(StateContext context)
    {
        Debug.Log("退出游戏状态");
        _isGameRunning = false;
        
        try
        {
            // 保存游戏进度
            await SaveGameProgressAsync(context);
            
           
            
            // 隐藏游戏UI
          
            
            // 清理游戏资源
            await CleanupGameResourcesAsync();
            
            // 记录游戏时长
            float playTime = Time.time - _gameStartTime;
            context.SetData("LastPlayTime", playTime);
            Debug.Log($"本次游戏时长: {playTime:F2} 秒");
        }
        catch (Exception e)
        {
            Debug.LogError($"退出游戏状态时发生错误: {e}");
        }
    }

    private async UniTask LoadGameSceneAsync()
    {
        Debug.Log("加载游戏场景...");
        
        try
        {
            // TODO: 加载场景
            // var operation = SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
            // if (operation != null)
            // {
            //     while (!operation.isDone)
            //     {
            //         Debug.Log($"场景加载进度: {operation.progress:P}");
            //         await UniTask.Yield();
            //     }
            // }
            // else
            // {
            //     // 如果场景不存在，创建一个空场景
            //     Debug.LogWarning("GameScene 不存在，创建空场景");
            //    
            // }
            await UniTask.Delay(1000);
            Debug.Log("游戏场景加载完成");
        }
        catch (Exception e)
        {
            Debug.LogError($"加载游戏场景失败: {e}");
            throw;
        }
    }

    private async UniTask InitializeGameLogicAsync(StateContext context)
    {
        Debug.Log("初始化游戏逻辑");
        
       
        
        Debug.Log("游戏逻辑初始化完成");
    }

    private void UpdateGameLogic(float deltaTime, StateContext context)
    {
        
    }

    /// <summary>
    /// 检查游戏输入
    /// </summary>
    /// <param name="context"></param>
    private void CheckGameInput(StateContext context)
    {
        
    }

    private void CheckSystemInput(StateContext context)
    {
        // 检查暂停
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Pause))
        {
            TogglePause();
        }
        
        // 检查退出
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowExitConfirmDialog();
        }
        
        // 检查调试键（仅在开发模式）
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ShowDebugInfo(context);
        }
#endif
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        Debug.Log($"游戏暂停状态: {_isPaused}");
        
        // TODO: 显示/隐藏暂停菜单
    }

    private void ShowExitConfirmDialog()
    {
        Debug.Log("显示退出确认对话框");
        
        // TODO: 显示退出确认UI
        // 如果确认退出，则调用 ExitGame()
        
        // 这里直接退出作为示例
        ExitGame();
    }

    private void ExitGame()
    {
        Debug.Log("请求退出游戏");
        _stateMachine.ChangeStateAsync(GameStateId.Exit).Forget();
    }

    private void ShowDebugInfo(StateContext context)
    {
        Debug.Log("=== 调试信息 ===");
        Debug.Log($"游戏运行时间: {Time.time - _gameStartTime:F2} 秒");
        Debug.Log($"当前关卡: {context.GetData("CurrentLevel", 1)}");
        Debug.Log($"Manager数量: {context.Managers?.Count ?? 0}");
        Debug.Log($"Mod数量: {context.Mods?.Count ?? 0}");
    }

    private async UniTask SaveGameProgressAsync(StateContext context)
    {
        Debug.Log("保存游戏进度...");
        
        try
        {
            // TODO: 实现游戏进度保存
            // 保存玩家数据
            // 保存关卡进度
            // 保存游戏设置
            
            await UniTask.Delay(200);
            Debug.Log("游戏进度保存完成");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存游戏进度失败: {e}");
        }
    }

    private async UniTask CleanupGameResourcesAsync()
    {
        Debug.Log("清理游戏资源...");
        
        // 清理游戏对象
        CleanupGameObjects();
        
        // 卸载不需要的资源
        await UnloadUnusedResourcesAsync();
        
        // 重置时间缩放
        Time.timeScale = 1f;
        
        Debug.Log("游戏资源清理完成");
    }

    private void CleanupGameObjects()
    {
        // TODO: 清理游戏中的对象
        // 销毁玩家
        // 销毁敌人
        // 清理特效
    }

    private async UniTask UnloadUnusedResourcesAsync()
    {
        // 触发垃圾回收
        GC.Collect();
        
        // 卸载未使用的资源
        var operation = Resources.UnloadUnusedAssets();
        await operation;
    }
}
