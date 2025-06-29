using Cysharp.Threading.Tasks;
using UnityEngine;



/// <summary>
/// 游戏主入口 - 使用状态机管理游戏流程
/// </summary>
public class Main : MonoBehaviour
{
    private IGameStateMachine _gameStateMachine;
    private InitializeState _initializeState;
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
        
        // 启动游戏流程 - 所有的Manager和Mod初始化都在InitializeState中自动进行
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
        Debug.Log($"应用程序暂停状态: {pauseStatus}");
        
        if (pauseStatus)
        {
            // 游戏暂停时的处理
            // TODO: 保存游戏状态
            // TODO: 暂停音频
            // TODO: 断开网络连接（如果需要）
        }
        else
        {
            // 游戏恢复时的处理
            // TODO: 恢复音频
            // TODO: 重新连接网络（如果需要）
            // TODO: 刷新游戏状态
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"应用程序焦点状态: {hasFocus}");
        
        if (!hasFocus)
        {
            // 失去焦点时的处理
            // TODO: 可能需要暂停某些操作
        }
    }

    private void OnDestroy()
    {
        Debug.Log("Main对象被销毁");
        // TODO: 清理资源
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
        
        // TODO: 初始化自定义日志系统
        // Logger.Initialize();
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
        // 创建并保存InitializeState的引用
        _initializeState = new InitializeState(_gameStateMachine);
        
        // 注册所有游戏状态
        _gameStateMachine.RegisterState(_initializeState);
        _gameStateMachine.RegisterState(new CheckUpdateState(_gameStateMachine));
        _gameStateMachine.RegisterState(new UpdateResourceState(_gameStateMachine));
        _gameStateMachine.RegisterState(new PreloadResourceState(_gameStateMachine));
        _gameStateMachine.RegisterState(new InGameState(_gameStateMachine));
        _gameStateMachine.RegisterState(new ExitState());
        
        Debug.Log("所有游戏状态注册完成");
    }

    private async UniTask StartGameFlowAsync()
    {
        try
        {
            // 从初始化状态开始 - 所有Manager和Mod的自动发现和初始化都在这里进行
            await _gameStateMachine.ChangeStateAsync(GameStateId.Initialize);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"游戏启动失败: {e}");
            
            // 显示错误对话框
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("启动错误", $"游戏启动失败:\n{e.Message}", "确定");
#else
            // TODO: 显示游戏内错误UI
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
    /// 请求切换游戏状态（供其他系统调用）
    /// </summary>
    public async UniTask RequestChangeStateAsync(GameStateId targetState)
    {
        if (_gameStateMachine != null && _isInitialized)
        {
            await _gameStateMachine.ChangeStateAsync(targetState);
        }
        else
        {
            Debug.LogWarning("游戏状态机尚未初始化，无法切换状态");
        }
    }
}
