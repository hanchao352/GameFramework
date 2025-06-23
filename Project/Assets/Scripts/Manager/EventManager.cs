using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : SingletonManager<EventManager>, IGeneric
{
    // 无参数事件字典
    private Dictionary<EventID, List<Action>> _noParamEvents;
    
    // 带参数事件字典
    private Dictionary<EventID, List<IEventHandlerWrapper>> _paramEvents;
    
    // 延迟事件队列（基于时间）
    private List<DelayedEvent> _timeDelayedEvents;
    
    // 延迟事件队列（基于帧数）
    private List<FrameDelayedEvent> _frameDelayedEvents;
    
    // 可重用的列表缓存（优化GC）
    private List<Action> _actionListCache = new List<Action>();
    private List<IEventHandlerWrapper> _wrapperListCache = new List<IEventHandlerWrapper>();
    
    // 嵌套触发计数器
    private int _triggerDepth = 0;
    
    // 嵌套触发时的临时列表池
    private Stack<List<Action>> _actionListPool = new Stack<List<Action>>();
    private Stack<List<IEventHandlerWrapper>> _wrapperListPool = new Stack<List<IEventHandlerWrapper>>();

    // 延迟事件结构（基于时间）
    private struct DelayedEvent
    {
        public EventID EventID;
        public EventParams Params;
        public bool IsParamEvent;
        public float TriggerTime;
    }

    // 延迟事件结构（基于帧数）
    private struct FrameDelayedEvent
    {
        public EventID EventID;
        public EventParams Params;
        public bool IsParamEvent;
        public int FramesLeft;
    }

    // 事件处理器包装器接口
    public interface IEventHandlerWrapper
    {
        void Invoke(EventParams eventParams);
    }

    // 具体事件处理器包装器
    private class EventHandlerWrapper<T> : IEventHandlerWrapper where T : EventParams
    {
        public EventID EventID { get; }
        public Action<T> Handler { get; }
        
        public EventHandlerWrapper(EventID eventId, Action<T> handler)
        {
            EventID = eventId;
            Handler = handler;
        }

        public void Invoke(EventParams eventParams)
        {
            if (eventParams is T typedParams)
            {
                Handler?.Invoke(typedParams);
            }
            else
            {
                Debug.LogError($"Event {EventID} received wrong parameter type. " +
                               $"Expected: {typeof(T).Name}, " +
                               $"got: {eventParams?.GetType().Name ?? "null"}");
            }
        }
    }

    // 初始化
    public override void Initialize()
    {
        base.Initialize();
        
        _noParamEvents = new Dictionary<EventID, List<Action>>();
        _paramEvents = new Dictionary<EventID, List<IEventHandlerWrapper>>();
        _timeDelayedEvents = new List<DelayedEvent>();
        _frameDelayedEvents = new List<FrameDelayedEvent>();
        
        // 预填充对象池（5个列表）
        for (int i = 0; i < 5; i++)
        {
            _actionListPool.Push(new List<Action>(16));
            _wrapperListPool.Push(new List<IEventHandlerWrapper>(16));
        }
    }

    // ======================== 事件注册 ========================
    
    public void Register(EventID eventId, Action handler)
    {
        if (!_noParamEvents.ContainsKey(eventId))
            _noParamEvents[eventId] = new List<Action>();

        if (!_noParamEvents[eventId].Contains(handler))
            _noParamEvents[eventId].Add(handler);
    }

    public void Register<T>(EventID eventId, Action<T> handler) where T : EventParams
    {
        if (!_paramEvents.ContainsKey(eventId))
            _paramEvents[eventId] = new List<IEventHandlerWrapper>();

        var wrapper = new EventHandlerWrapper<T>(eventId, handler);
        _paramEvents[eventId].Add(wrapper);
    }

    // ======================== 事件注销 ========================
    
    public void Unregister(EventID eventId, Action handler)
    {
        if (_noParamEvents.TryGetValue(eventId, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0) _noParamEvents.Remove(eventId);
        }
    }

    public void Unregister<T>(EventID eventId, Action<T> handler) where T : EventParams
    {
        if (!_paramEvents.TryGetValue(eventId, out var wrappers)) return;
        
        for (int i = wrappers.Count - 1; i >= 0; i--)
        {
            if (wrappers[i] is EventHandlerWrapper<T> typedWrapper && 
                typedWrapper.Handler == handler)
            {
                wrappers.RemoveAt(i);
                break;
            }
        }
        
        if (wrappers.Count == 0) _paramEvents.Remove(eventId);
    }

    // ======================== 事件触发（支持任意深度嵌套） ========================
    
    public void Trigger(EventID eventId)
    {
        if (!_noParamEvents.TryGetValue(eventId, out var handlers) || handlers.Count == 0)
            return;
        
        _triggerDepth++;
        
        try
        {
            List<Action> listToUse = GetListForCurrentDepth(handlers);
            
            foreach (var handler in listToUse)
            {
                try
                {
                    handler?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Event {eventId} handler exception: {e}");
                }
            }
            
            ReturnListAfterUse(listToUse);
        }
        finally
        {
            _triggerDepth--;
        }
    }

    public void Trigger<T>(EventID eventId, T eventParams) where T : EventParams
    {
        if (!_paramEvents.TryGetValue(eventId, out var wrappers) || wrappers.Count == 0)
        {
            EventParamsFactory.Instance.Recycle(eventParams);
            return;
        }
        
        _triggerDepth++;
        
        try
        {
            List<IEventHandlerWrapper> listToUse = GetWrapperListForCurrentDepth(wrappers);
            
            foreach (var wrapper in listToUse)
            {
                try
                {
                    wrapper.Invoke(eventParams);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Event {eventId} handler exception: {e}");
                }
            }
            
            ReturnWrapperListAfterUse(listToUse);
            
            EventParamsFactory.Instance.Recycle(eventParams);
        }
        finally
        {
            _triggerDepth--;
        }
    }
    
    // ======================== 嵌套支持核心方法 ========================
    
    private List<Action> GetListForCurrentDepth(List<Action> handlers)
    {
        if (_triggerDepth == 1)
        {
            _actionListCache.Clear();
            _actionListCache.AddRange(handlers);
            return _actionListCache;
        }
        
        var list = GetActionListFromPool();
        list.AddRange(handlers);
        return list;
    }
    
    private void ReturnListAfterUse(List<Action> list)
    {
        if (_triggerDepth > 1)
        {
            list.Clear();
            ReturnActionListToPool(list);
        }
    }
    
    private List<IEventHandlerWrapper> GetWrapperListForCurrentDepth(List<IEventHandlerWrapper> wrappers)
    {
        if (_triggerDepth == 1)
        {
            _wrapperListCache.Clear();
            _wrapperListCache.AddRange(wrappers);
            return _wrapperListCache;
        }
        
        var list = GetWrapperListFromPool();
        list.AddRange(wrappers);
        return list;
    }
    
    private void ReturnWrapperListAfterUse(List<IEventHandlerWrapper> list)
    {
        if (_triggerDepth > 1)
        {
            list.Clear();
            ReturnWrapperListToPool(list);
        }
    }
    
    // ======================== 列表池管理 ========================
    
    private List<Action> GetActionListFromPool()
    {
        return _actionListPool.Count > 0 ? _actionListPool.Pop() : new List<Action>(16);
    }
    
    private void ReturnActionListToPool(List<Action> list)
    {
        list.Clear();
        _actionListPool.Push(list);
    }
    
    private List<IEventHandlerWrapper> GetWrapperListFromPool()
    {
        return _wrapperListPool.Count > 0 ? _wrapperListPool.Pop() : new List<IEventHandlerWrapper>(16);
    }
    
    private void ReturnWrapperListToPool(List<IEventHandlerWrapper> list)
    {
        list.Clear();
        _wrapperListPool.Push(list);
    }

    // ======================== 延迟触发 ========================
    
    public void TriggerDelayed(EventID eventId, float delaySeconds)
    {
        _timeDelayedEvents.Add(new DelayedEvent
        {
            EventID = eventId,
            Params = null,
            IsParamEvent = false,
            TriggerTime = Time.time + delaySeconds
        });
    }

    public void TriggerDelayed<T>(EventID eventId, T eventParams, float delaySeconds) where T : EventParams
    {
        _timeDelayedEvents.Add(new DelayedEvent
        {
            EventID = eventId,
            Params = eventParams,
            IsParamEvent = true,
            TriggerTime = Time.time + delaySeconds
        });
    }

    public void TriggerDelayedFrames(EventID eventId, int frames)
    {
        _frameDelayedEvents.Add(new FrameDelayedEvent
        {
            EventID = eventId,
            Params = null,
            IsParamEvent = false,
            FramesLeft = frames
        });
    }

    public void TriggerDelayedFrames<T>(EventID eventId, T eventParams, int frames) where T : EventParams
    {
        _frameDelayedEvents.Add(new FrameDelayedEvent
        {
            EventID = eventId,
            Params = eventParams,
            IsParamEvent = true,
            FramesLeft = frames
        });
    }

    // ======================== 更新处理 ========================
    
    public override void Update(float time)
    {
        ProcessTimeDelayedEvents();
        ProcessFrameDelayedEvents();
    }

    private void ProcessTimeDelayedEvents()
    {
        for (int i = _timeDelayedEvents.Count - 1; i >= 0; i--)
        {
            var evt = _timeDelayedEvents[i];
            if (Time.time >= evt.TriggerTime)
            {
                var eventToTrigger = evt;
                _timeDelayedEvents.RemoveAt(i);
                
                if (eventToTrigger.IsParamEvent)
                {
                    Trigger(eventToTrigger.EventID, eventToTrigger.Params);
                }
                else
                {
                    Trigger(eventToTrigger.EventID);
                }
            }
        }
    }

    private void ProcessFrameDelayedEvents()
    {
        for (int i = _frameDelayedEvents.Count - 1; i >= 0; i--)
        {
            var evt = _frameDelayedEvents[i];
            evt.FramesLeft--;
            
            if (evt.FramesLeft <= 0)
            {
                var eventToTrigger = evt;
                _frameDelayedEvents.RemoveAt(i);
                
                if (eventToTrigger.IsParamEvent)
                {
                    Trigger(eventToTrigger.EventID, eventToTrigger.Params);
                }
                else
                {
                    Trigger(eventToTrigger.EventID);
                }
            }
            else
            {
                _frameDelayedEvents[i] = evt;
            }
        }
    }

    // ======================== 清理资源 ========================
    
    public override void Dispose()
    {
        base.Dispose();
        
        _noParamEvents?.Clear();
        _paramEvents?.Clear();
        
        // 回收延迟事件参数
        RecycleDelayedEventParams(_timeDelayedEvents);
        RecycleDelayedEventParams(_frameDelayedEvents);
        
        _timeDelayedEvents?.Clear();
        _frameDelayedEvents?.Clear();
        
        // 清理缓存和对象池
        _actionListCache?.Clear();
        _wrapperListCache?.Clear();
        
        _actionListPool.Clear();
        _wrapperListPool.Clear();
    }
    
    private void RecycleDelayedEventParams(List<DelayedEvent> events)
    {
        if (events == null) return;
        
        foreach (var evt in events) 
        {
            if (evt.IsParamEvent && evt.Params != null) 
                EventParamsFactory.Instance.Recycle(evt.Params);
        }
    }
    
    private void RecycleDelayedEventParams(List<FrameDelayedEvent> events)
    {
        if (events == null) return;
        
        foreach (var evt in events) 
        {
            if (evt.IsParamEvent && evt.Params != null) 
                EventParamsFactory.Instance.Recycle(evt.Params);
        }
    }
}