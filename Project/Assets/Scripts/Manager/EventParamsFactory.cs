using System;
using System.Collections.Generic;

public class EventParamsFactory : SingletonManager<EventParamsFactory>, IGeneric
{
    // 对象池：按参数类型存储
    private Dictionary<Type, Stack<EventParams>> _paramsPool = 
        new Dictionary<Type, Stack<EventParams>>();

    public override void Initialize()
    {
        base.Initialize();
        _paramsPool = new Dictionary<Type, Stack<EventParams>>();
    }

    // 获取参数实例（从对象池）
    public T Get<T>() where T : EventParams, new()
    {
        Type type = typeof(T);
        
        // 尝试从对象池获取
        if (_paramsPool.TryGetValue(type, out var pool) && pool.Count > 0)
        {
            return (T)pool.Pop();
        }
        
        // 创建新实例
        return new T();
    }

    // 回收参数实例
    public void Recycle(EventParams eventParams)
    {
        if (eventParams == null) return;
        
        Type type = eventParams.GetType();
        
        // 重置参数状态
        eventParams.Reset();
        
        // 初始化对象池
        if (!_paramsPool.ContainsKey(type))
        {
            _paramsPool[type] = new Stack<EventParams>();
        }
        
        // 回收到对象池
        _paramsPool[type].Push(eventParams);
    }

    // 清理特定类型的对象池
    public void ClearPool<T>() where T : EventParams
    {
        Type type = typeof(T);
        if (_paramsPool.ContainsKey(type))
        {
            _paramsPool[type].Clear();
        }
    }

    // 清理所有对象池
    public void ClearAllPools()
    {
        foreach (var pool in _paramsPool.Values)
        {
            pool.Clear();
        }
        _paramsPool.Clear();
    }

    public override void Dispose()
    {
        base.Dispose();
        ClearAllPools();
    }
}