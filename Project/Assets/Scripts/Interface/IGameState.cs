using Cysharp.Threading.Tasks;

public interface IGameState
{
    /// <summary>
    /// 状态ID
    /// </summary>
    GameStateId StateId { get; }
        
    /// <summary>
    /// 进入状态
    /// </summary>
    UniTask OnEnterAsync(StateContext context);
        
    /// <summary>
    /// 更新状态
    /// </summary>
    void OnUpdate(float deltaTime, StateContext context);
        
    /// <summary>
    /// 退出状态
    /// </summary>
    UniTask OnExitAsync(StateContext context);
}