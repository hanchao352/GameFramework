using Cysharp.Threading.Tasks;
using UnityEngine;



/// <summary>
/// 游戏主入口 - 使用状态机管理游戏流程
/// </summary>
public class Main : MonoBehaviour
{
    private IGameStateMachine _gameStateMachine;
    private bool _isInitialized;

    private void Awake()
    {
        // 确保Main对象在场景切换时不被销毁
        DontDestroyOnLoad(gameObject);
        
        // 设置应用程序基本配置
        ConfigureApplication();
        
        // 初始化基础日志系统
        InitializeLogger();
    }

    private async void Start()
    {
        Debug.Log("=== 游戏启动 ===");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Device: {SystemInfo.deviceModel}");
        
        // 初始化游戏状态机
        await InitializeGameStateMachineAsync();
        
        // 启动游戏流程 - 从检查更新开始
        await StartGameFlowAsync();
    }

    private void Update()
    {
        if (_isInitialized && _gameStateMachine != null)
        {
            _gameStateMachine.Update(Time.deltaTime);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // 可以通过Context通知所有Manager和Mod
        if (_gameStateMachine?.Context != null)
        {
            _gameStateMachine.Context.SetData("IsPaused", pauseStatus);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        
        // 可以通过Context通知所有Manager和Mod
        if (_gameStateMachine?.Context != null)
        {
            _gameStateMachine.Context.SetData("HasFocus", hasFocus);
        }
    }

    private void OnDestroy()
    {
        Debug.Log("Main对象被销毁");
        // 清理Context
        _gameStateMachine?.Context?.Clear();
    }

    private void ConfigureApplication()
    {
        // 设置目标帧率
        Application.targetFrameRate = 60;
        
        // 防止屏幕休眠
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        
        // 设置后台运行
        Application.runInBackground = true;
        
        // 设置屏幕方向（根据游戏类型调整）
#if UNITY_ANDROID || UNITY_IOS
        Screen.orientation = ScreenOrientation.Portrait;
#endif
    }

    private void InitializeLogger()
    {
        // 设置日志级别
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.unityLogger.logEnabled = true;
        Debug.unityLogger.filterLogType = LogType.Log;
#else
        Debug.unityLogger.logEnabled = true;
        Debug.unityLogger.filterLogType = LogType.Warning;
#endif
    }

    private async UniTask InitializeGameStateMachineAsync()
    {
        Debug.Log("初始化游戏状态机...");
        
        // 创建状态机实例
        _gameStateMachine = new GameStateMachine();
        
        // 注册所有游戏状态
        RegisterGameStates();
        
        _isInitialized = true;
        
        await UniTask.CompletedTask;
    }

    private void RegisterGameStates()
    {
        // 按照新的顺序注册所有游戏状态
        _gameStateMachine.RegisterState(new CheckUpdateState(_gameStateMachine));
        _gameStateMachine.RegisterState(new UpdateResourceState(_gameStateMachine));
        _gameStateMachine.RegisterState(new InitializeState(_gameStateMachine));
        _gameStateMachine.RegisterState(new PreloadResourceState(_gameStateMachine));
        _gameStateMachine.RegisterState(new InGameState(_gameStateMachine));
        _gameStateMachine.RegisterState(new ExitState());
        
        Debug.Log("所有游戏状态注册完成");
    }

    private async UniTask StartGameFlowAsync()
    {
        try
        {
            // 从检查更新状态开始
            await _gameStateMachine.ChangeStateAsync(GameStateId.CheckUpdate);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"游戏启动失败: {e}");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>
    /// 获取游戏状态机（供其他系统使用）
    /// </summary>
    public IGameStateMachine GetStateMachine()
    {
        return _gameStateMachine;
    }

    /// <summary>
    /// 获取状态上下文（供其他系统使用）
    /// </summary>
    public StateContext GetStateContext()
    {
        return _gameStateMachine?.Context;
    }
}
