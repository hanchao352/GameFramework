using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStateMachine : IGameStateMachine
{
    private readonly Dictionary<GameStateId, IGameState> _states = new();
    private readonly StateContext _context = new();
    private IGameState _currentState;
    private bool _isTransitioning;
    private GameStateId _pendingStateId = GameStateId.None;

    public IGameState CurrentState => _currentState;
    public GameStateId CurrentStateId => _currentState?.StateId ?? GameStateId.None;
    public StateContext Context => _context;

    public void RegisterState(IGameState state)
    {
        if (state == null)
        {
            Debug.LogError("Cannot register null state!");
            return;
        }

        if (_states.ContainsKey(state.StateId))
        {
            Debug.LogWarning($"State {state.StateId} already registered!");
            return;
        }

        _states[state.StateId] = state;
        Debug.Log($"Registered state: {state.StateId}");
    }

    public async UniTask ChangeStateAsync(GameStateId stateId)
    {
        Debug.Log($"[StateMachine] 请求切换到状态: {stateId}");
        
        // 如果当前正在转换中，设置待切换状态
        if (_isTransitioning)
        {
            Debug.Log($"[StateMachine] 状态转换正在进行中，将 {stateId} 设置为待切换状态");
            _pendingStateId = stateId;
            return;
        }

        if (!_states.TryGetValue(stateId, out var newState))
        {
            Debug.LogError($"[StateMachine] 状态 {stateId} 未找到!");
            return;
        }

        if (_currentState != null && _currentState.StateId == stateId)
        {
            Debug.LogWarning($"[StateMachine] 已经在状态 {stateId} 中!");
            return;
        }

        await PerformStateTransitionAsync(newState);
    }

    private async UniTask PerformStateTransitionAsync(IGameState newState)
    {
        _isTransitioning = true;
        var previousState = _currentState;
        
        Debug.Log($"[StateMachine] 开始状态转换: {previousState?.StateId ?? GameStateId.None} -> {newState.StateId}");

        try
        {
            // 退出当前状态
            if (previousState != null)
            {
                Debug.Log($"[StateMachine] 退出状态: {previousState.StateId}");
                await previousState.OnExitAsync(_context);
                Debug.Log($"[StateMachine] 成功退出状态: {previousState.StateId}");
            }

            // 切换到新状态
            _currentState = newState;
            Debug.Log($"[StateMachine] 进入状态: {_currentState.StateId}");
            await _currentState.OnEnterAsync(_context);
            Debug.Log($"[StateMachine] 成功进入状态: {_currentState.StateId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[StateMachine] 状态转换失败 ({newState.StateId}): {e}");
            Debug.LogError($"[StateMachine] 错误堆栈: {e.StackTrace}");
            throw;
        }
        finally
        {
            _isTransitioning = false;
            Debug.Log($"[StateMachine] 状态转换完成，当前状态: {CurrentStateId}");
            
            // 检查是否有待切换的状态
            if (_pendingStateId != GameStateId.None)
            {
                var pendingState = _pendingStateId;
                _pendingStateId = GameStateId.None;
                Debug.Log($"[StateMachine] 执行待切换状态: {pendingState}");
                await ChangeStateAsync(pendingState);
            }
        }
    }

    public void Update(float deltaTime)
    {
        if (!_isTransitioning && _currentState != null)
        {
            _currentState.OnUpdate(deltaTime, _context);
        }
    }
}
