using Cysharp.Threading.Tasks;

public interface IGameStateMachine
{
    /// <summary>
    /// 当前状态
    /// </summary>
    IGameState CurrentState { get; }
        
    /// <summary>
    /// 当前状态ID
    /// </summary>
    GameStateId CurrentStateId { get; }
        
    /// <summary>
    /// 状态上下文
    /// </summary>
    StateContext Context { get; }
        
    /// <summary>
    /// 注册状态
    /// </summary>
    void RegisterState(IGameState state);
        
    /// <summary>
    /// 切换状态
    /// </summary>
    UniTask ChangeStateAsync(GameStateId stateId);
        
    /// <summary>
    /// 更新状态机
    /// </summary>
    void Update(float deltaTime);
}