using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

[ManagerAttribute]
public class TimerManager : SingletonManager<TimerManager>, IGeneric
{
    private class TimerData
    {
        public Action Callback;
        public Action<object[]> CallbackWithParams;
        public object[] Args;
        public long TargetTime;        // 目标时间(ticks)
        public long IntervalTicks;     // 间隔时间(ticks)
        public bool Repeat;
        public bool IsBackgroundTimer; // 标记是否为后台计时器

        public void Reset()
        {
            Callback = null;
            CallbackWithParams = null;
            Args = null;
            TargetTime = 0;
            IntervalTicks = 0;
            Repeat = false;
            IsBackgroundTimer = false;
        }
    }

    private readonly Dictionary<int, TimerData> timers = new Dictionary<int, TimerData>();
    private readonly List<int> timersToRemove = new List<int>();
    private readonly object lockObject = new object();
    private int nextId = 1;
    private readonly Stopwatch stopwatch = new Stopwatch();
    private const long TicksPerMillisecond = TimeSpan.TicksPerMillisecond;
    
    // 后台时间管理
    private long backgroundStartTime;  // 进入后台时的时间戳(ticks)
    private bool isInBackground;       // 是否在后台
    
    // 对象池相关
    private readonly Stack<TimerData> timerDataPool = new Stack<TimerData>();
    private const int InitialPoolSize = 20;
    private const int MaxPoolSize = 100;

    // 性能统计
    private int totalTimersCreated;
    private int totalTimersReused;

    // 调试控制
#if UNITY_EDITOR || DEBUG
    [NonSerialized] public bool EnableDebugLog = false;
#endif

    public int ActiveTimersCount => timers.Count;
    public int TotalTimersCreated => totalTimersCreated;
    public int TotalTimersReused => totalTimersReused;
    public float ReuseRatio => totalTimersCreated > 0 ? (float)totalTimersReused / totalTimersCreated : 0;

    public override void Initialize()
    {
        stopwatch.Start();
        
        // 预初始化对象池
        for (int i = 0; i < InitialPoolSize; i++)
        {
            timerDataPool.Push(new TimerData());
            totalTimersCreated++;
        }
        
        // 注册应用暂停/恢复事件
        Application.focusChanged += OnApplicationFocusChanged;
    }

    private void OnApplicationFocusChanged(bool hasFocus)
    {
        if (hasFocus)
        {
            // 应用回到前台
            OnApplicationResume();
        }
        else
        {
            // 应用进入后台
            OnApplicationPause();
        }
    }

    private void OnApplicationPause()
    {
        if (isInBackground) return;
        
        isInBackground = true;
        backgroundStartTime = stopwatch.ElapsedTicks;
        
        #if UNITY_EDITOR || DEBUG
        if (EnableDebugLog) Debug.Log("应用进入后台");
        #endif
        
        // 标记所有活动定时器为后台计时器
        lock (lockObject)
        {
            foreach (var timer in timers.Values)
            {
                timer.IsBackgroundTimer = true;
            }
        }
    }

    private void OnApplicationResume()
    {
        if (!isInBackground) return;
        
        isInBackground = false;
        long backgroundDuration = stopwatch.ElapsedTicks - backgroundStartTime;
        
        #if UNITY_EDITOR || DEBUG
        if (EnableDebugLog) 
            Debug.Log($"应用回到前台，后台停留时间: {backgroundDuration / TicksPerMillisecond}ms");
        #endif
        
        // 检查并处理过期定时器
        CheckExpiredTimersOnResume();
    }

    private void CheckExpiredTimersOnResume()
    {
        long currentTicks = stopwatch.ElapsedTicks;
        timersToRemove.Clear();
        
        lock (lockObject)
        {
            foreach (var entry in timers)
            {
                int id = entry.Key;
                TimerData data = entry.Value;
                
                // 只处理后台计时器
                if (!data.IsBackgroundTimer) continue;
                
                // 如果定时器已过期
                if (currentTicks >= data.TargetTime)
                {
                    try
                    {
                        // 立即执行回调
                        data.Callback?.Invoke();
                        data.CallbackWithParams?.Invoke(data.Args);
                        
                        #if UNITY_EDITOR || DEBUG
                        if (EnableDebugLog) 
                            Debug.Log($"后台定时器到期立即执行: ID {id}");
                        #endif
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"后台定时器回调错误: {e}");
                    }
                    
                    if (data.Repeat)
                    {
                        // 计算新的目标时间
                        data.TargetTime = currentTicks + data.IntervalTicks;
                        #if UNITY_EDITOR || DEBUG
                        if (EnableDebugLog) 
                            Debug.Log($"重复定时器重置: ID {id}, 新目标时间: {data.TargetTime}");
                        #endif
                    }
                    else
                    {
                        timersToRemove.Add(id);
                        ReturnTimerDataToPool(data);
                    }
                }
                else
                {
                    // 定时器未过期，继续等待
                    data.IsBackgroundTimer = false; // 重置标记
                    #if UNITY_EDITOR || DEBUG
                    if (EnableDebugLog) 
                    {
                        long remaining = (data.TargetTime - currentTicks) / TicksPerMillisecond;
                        Debug.Log($"后台定时器未到期: ID {id}, 剩余时间: {remaining}ms");
                    }
                    #endif
                }
            }
            
            // 移除已完成的单次定时器
            foreach (int id in timersToRemove)
            {
                if (timers.Remove(id))
                {
                    #if UNITY_EDITOR || DEBUG
                    if (EnableDebugLog) Debug.Log($"移除后台定时器: ID {id}");
                    #endif
                }
            }
        }
    }

    private TimerData GetTimerDataFromPool()
    {
        TimerData data;
        lock (lockObject)
        {
            if (timerDataPool.Count > 0)
            {
                data = timerDataPool.Pop();
                totalTimersReused++;
                return data;
            }
        }
        
        data = new TimerData();
        Interlocked.Increment(ref totalTimersCreated);
        return data;
    }

    private void ReturnTimerDataToPool(TimerData data)
    {
        if (data == null) return;
        
        data.Reset();
        
        lock (lockObject)
        {
            if (timerDataPool.Count < MaxPoolSize)
            {
                timerDataPool.Push(data);
            }
        }
    }

    public override void Update(float deltaTime)
    {
        if (timers.Count == 0) return;

        long currentTicks = stopwatch.ElapsedTicks;
        timersToRemove.Clear();

        lock (lockObject)
        {
            foreach (var entry in timers)
            {
                int id = entry.Key;
                TimerData data = entry.Value;
                
                // 跳过后台计时器（已在切回前台时处理）
                if (data.IsBackgroundTimer) continue;

                if (currentTicks >= data.TargetTime)
                {
                    try
                    {
                        data.Callback?.Invoke();
                        data.CallbackWithParams?.Invoke(data.Args);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"定时器回调错误: {e}");
                        timersToRemove.Add(id);
                        continue;
                    }

                    if (data.Repeat)
                    {
                        data.TargetTime = currentTicks + data.IntervalTicks;
                    }
                    else
                    {
                        timersToRemove.Add(id);
                        ReturnTimerDataToPool(data);
                    }
                }
            }

            foreach (int id in timersToRemove)
            {
                if (timers.Remove(id))
                {
                    #if UNITY_EDITOR || DEBUG
                    if (EnableDebugLog) Debug.Log($"移除定时器: ID {id}");
                    #endif
                }
            }
        }
    }

    private int AddTimer(TimerData data)
    {
        lock (lockObject)
        {
            int id = nextId++;
            timers[id] = data;
            
            // 如果当前在后台，标记为后台计时器
            if (isInBackground)
            {
                data.IsBackgroundTimer = true;
            }
            
            return id;
        }
    }

    public int SetTimeout(Action callback, float delayInMilliseconds)
    {
        var data = GetTimerDataFromPool();
        data.Callback = callback;
        data.TargetTime = stopwatch.ElapsedTicks + (long)(delayInMilliseconds * TicksPerMillisecond);
        data.Repeat = false;
        return AddTimer(data);
    }

    public int SetTimeout(Action<object[]> callback, float delayInMilliseconds, params object[] args)
    {
        var data = GetTimerDataFromPool();
        data.CallbackWithParams = callback;
        data.Args = args;
        data.TargetTime = stopwatch.ElapsedTicks + (long)(delayInMilliseconds * TicksPerMillisecond);
        data.Repeat = false;
        return AddTimer(data);
    }

    public int SetInterval(Action callback, float intervalInMilliseconds)
    {
        var data = GetTimerDataFromPool();
        data.Callback = callback;
        data.IntervalTicks = (long)(intervalInMilliseconds * TicksPerMillisecond);
        data.TargetTime = stopwatch.ElapsedTicks + data.IntervalTicks;
        data.Repeat = true;
        
        // 注册时立即执行一次回调
        try
        {
            callback?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"定时器初始回调执行错误: {e}");
        }
        
        return AddTimer(data);
    }

    public int SetInterval(Action<object[]> callback, float intervalInMilliseconds, params object[] args)
    {
        var data = GetTimerDataFromPool();
        data.CallbackWithParams = callback;
        data.Args = args;
        data.IntervalTicks = (long)(intervalInMilliseconds * TicksPerMillisecond);
        data.TargetTime = stopwatch.ElapsedTicks + data.IntervalTicks;
        data.Repeat = true;
        
        // 注册时立即执行一次回调
        try
        {
            callback?.Invoke(args);
        }
        catch (Exception e)
        {
            Debug.LogError($"定时器初始回调执行错误: {e}");
        }
        
        return AddTimer(data);
    }

    public int SetIntervalAfterDelay(Action callback, float delayInMilliseconds, float intervalInMilliseconds)
    {
        var data = GetTimerDataFromPool();
        data.Callback = callback;
        data.IntervalTicks = (long)(intervalInMilliseconds * TicksPerMillisecond);
        data.TargetTime = stopwatch.ElapsedTicks + (long)(delayInMilliseconds * TicksPerMillisecond);
        data.Repeat = true;
        return AddTimer(data);
    }

    public int SetIntervalAfterDelay(Action<object[]> callback, float delayInMilliseconds, float intervalInMilliseconds, params object[] args)
    {
        var data = GetTimerDataFromPool();
        data.CallbackWithParams = callback;
        data.Args = args;
        data.IntervalTicks = (long)(intervalInMilliseconds * TicksPerMillisecond);
        data.TargetTime = stopwatch.ElapsedTicks + (long)(delayInMilliseconds * TicksPerMillisecond);
        data.Repeat = true;
        return AddTimer(data);
    }

    public bool IsTimerActive(int id)
    {
        lock (lockObject)
        {
            return timers.ContainsKey(id);
        }
    }

    public int GetRemainingTime(int id)
    {
        lock (lockObject)
        {
            if (timers.TryGetValue(id, out var data))
            {
                long remaining = data.TargetTime - stopwatch.ElapsedTicks;
                return (int)(remaining / TicksPerMillisecond);
            }
            return -1;
        }
    }

    public void RemoveTimer(int id)
    {
        lock (lockObject)
        {
            if (timers.TryGetValue(id, out TimerData data))
            {
                ReturnTimerDataToPool(data);
                timers.Remove(id);
            }
        }
    }

    public override void Dispose()
    {
        // 取消事件注册
        Application.focusChanged -= OnApplicationFocusChanged;
        
        lock (lockObject)
        {
            foreach (var timer in timers.Values)
            {
                ReturnTimerDataToPool(timer);
            }
            timers.Clear();
            stopwatch.Stop();
            
            // 清空对象池
            timerDataPool.Clear();
            totalTimersCreated = 0;
            totalTimersReused = 0;
        }
    }
}