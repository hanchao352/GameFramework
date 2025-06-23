using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class FrameManager : SingletonManager<FrameManager>, IGeneric
{
    private class FrameData
    {
        public Action Callback;
        public Action<object[]> CallbackWithParams;
        public object[] Args;
        public int RemainingFrames;    // 剩余帧数
        public int IntervalFrames;     // 间隔帧数（重复定时器）
        public bool Repeat;
        public bool IsPaused;          // 用户手动暂停状态

        public void Reset()
        {
            Callback = null;
            CallbackWithParams = null;
            Args = null;
            RemainingFrames = 0;
            IntervalFrames = 0;
            Repeat = false;
            IsPaused = false;
        }
    }

    private readonly Dictionary<int, FrameData> frameTimers = new Dictionary<int, FrameData>();
    private readonly List<int> timersToRemove = new List<int>();
    private readonly object lockObject = new object();
    private int nextId = 1;
    private int currentFrame;
    
    // 后台状态管理
    private bool isInBackground;
    
    // 对象池相关
    private readonly Stack<FrameData> frameDataPool = new Stack<FrameData>();
    private const int InitialPoolSize = 20;
    private const int MaxPoolSize = 100;

    // 调试控制
#if UNITY_EDITOR || DEBUG
    [NonSerialized] public bool EnableDebugLog = false;
#endif

    public int ActiveTimersCount => frameTimers.Count;
    public int CurrentFrame => currentFrame;

    public override void Initialize()
    {
        currentFrame = 0;
        isInBackground = false;
        
        // 预初始化对象池
        for (int i = 0; i < InitialPoolSize; i++)
        {
            frameDataPool.Push(new FrameData());
        }
        
        // 注册应用焦点变化事件
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
        
        #if UNITY_EDITOR || DEBUG
        if (EnableDebugLog) Debug.Log("应用进入后台，帧定时器自动暂停");
        #endif
    }

    private void OnApplicationResume()
    {
        if (!isInBackground) return;
        
        isInBackground = false;
        
        #if UNITY_EDITOR || DEBUG
        if (EnableDebugLog) Debug.Log("应用回到前台，帧定时器恢复运行");
        #endif
    }

    public override void Update(float deltaTime)
    {
        // 后台时跳过所有处理
        if (isInBackground) return;
        
        currentFrame++;
        
        if (frameTimers.Count == 0) return;
        
        timersToRemove.Clear();

        lock (lockObject)
        {
            foreach (var entry in frameTimers)
            {
                int id = entry.Key;
                FrameData data = entry.Value;
                
                // 跳过暂停的定时器
                if (data.IsPaused) continue;
                
                // 减少剩余帧数
                data.RemainingFrames--;
                
                if (data.RemainingFrames <= 0)
                {
                    try
                    {
                        data.Callback?.Invoke();
                        data.CallbackWithParams?.Invoke(data.Args);
                        
                        #if UNITY_EDITOR || DEBUG
                        if (EnableDebugLog) 
                            Debug.Log($"帧定时器触发: ID {id}");
                        #endif
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"帧回调错误: {e}");
                        timersToRemove.Add(id);
                        continue;
                    }

                    if (data.Repeat)
                    {
                        // 重置剩余帧数
                        data.RemainingFrames = data.IntervalFrames;
                    }
                    else
                    {
                        timersToRemove.Add(id);
                    }
                }
            }

            foreach (int id in timersToRemove)
            {
                if (frameTimers.TryGetValue(id, out FrameData data))
                {
                    ReturnFrameDataToPool(data);
                    frameTimers.Remove(id);
                    #if UNITY_EDITOR || DEBUG
                    if (EnableDebugLog) Debug.Log($"移除帧定时器: ID {id}");
                    #endif
                }
            }
        }
    }

    private FrameData GetFrameDataFromPool()
    {
        lock (lockObject)
        {
            if (frameDataPool.Count > 0)
            {
                return frameDataPool.Pop();
            }
        }
        return new FrameData();
    }

    private void ReturnFrameDataToPool(FrameData data)
    {
        if (data == null) return;
        
        data.Reset();
        
        lock (lockObject)
        {
            if (frameDataPool.Count < MaxPoolSize)
            {
                frameDataPool.Push(data);
            }
        }
    }

    private int AddFrameTimer(FrameData data)
    {
        lock (lockObject)
        {
            int id = nextId++;
            frameTimers[id] = data;
            return id;
        }
    }

    #region Public API
    
    /// <summary>
    /// 在指定帧数后执行一次回调
    /// </summary>
    public int SetFrameTimeout(Action callback, int delayFrames)
    {
        var data = GetFrameDataFromPool();
        data.Callback = callback;
        data.RemainingFrames = delayFrames;
        data.Repeat = false;
        return AddFrameTimer(data);
    }

    /// <summary>
    /// 在指定帧数后执行一次回调（带参数）
    /// </summary>
    public int SetFrameTimeout(Action<object[]> callback, int delayFrames, params object[] args)
    {
        var data = GetFrameDataFromPool();
        data.CallbackWithParams = callback;
        data.Args = args;
        data.RemainingFrames = delayFrames;
        data.Repeat = false;
        return AddFrameTimer(data);
    }

    /// <summary>
    /// 每隔指定帧数重复执行回调（立即执行第一次）
    /// </summary>
    public int SetFrameInterval(Action callback, int intervalFrames)
    {
        var data = GetFrameDataFromPool();
        data.Callback = callback;
        data.IntervalFrames = intervalFrames;
        data.RemainingFrames = intervalFrames;
        data.Repeat = true;
        
        // 立即执行一次回调
        try
        {
            callback?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"帧间隔初始回调错误: {e}");
        }
        
        return AddFrameTimer(data);
    }

    /// <summary>
    /// 每隔指定帧数重复执行回调（带参数，立即执行第一次）
    /// </summary>
    public int SetFrameInterval(Action<object[]> callback, int intervalFrames, params object[] args)
    {
        var data = GetFrameDataFromPool();
        data.CallbackWithParams = callback;
        data.Args = args;
        data.IntervalFrames = intervalFrames;
        data.RemainingFrames = intervalFrames;
        data.Repeat = true;
        
        // 立即执行一次回调
        try
        {
            callback?.Invoke(args);
        }
        catch (Exception e)
        {
            Debug.LogError($"帧间隔初始回调错误: {e}");
        }
        
        return AddFrameTimer(data);
    }

    /// <summary>
    /// 延迟指定帧数后开始，每隔指定帧数重复执行
    /// </summary>
    public int SetFrameIntervalAfterDelay(Action callback, int delayFrames, int intervalFrames)
    {
        var data = GetFrameDataFromPool();
        data.Callback = callback;
        data.IntervalFrames = intervalFrames;
        data.RemainingFrames = delayFrames;
        data.Repeat = true;
        return AddFrameTimer(data);
    }

    /// <summary>
    /// 延迟指定帧数后开始，每隔指定帧数重复执行（带参数）
    /// </summary>
    public int SetFrameIntervalAfterDelay(Action<object[]> callback, int delayFrames, int intervalFrames, params object[] args)
    {
        var data = GetFrameDataFromPool();
        data.CallbackWithParams = callback;
        data.Args = args;
        data.IntervalFrames = intervalFrames;
        data.RemainingFrames = delayFrames;
        data.Repeat = true;
        return AddFrameTimer(data);
    }

    /// <summary>
    /// 暂停指定帧定时器
    /// </summary>
    public void PauseFrameTimer(int id)
    {
        lock (lockObject)
        {
            if (frameTimers.TryGetValue(id, out FrameData data))
            {
                data.IsPaused = true;
                #if UNITY_EDITOR || DEBUG
                if (EnableDebugLog) 
                    Debug.Log($"暂停帧定时器: ID {id}, 剩余帧数: {data.RemainingFrames}");
                #endif
            }
        }
    }

    /// <summary>
    /// 恢复指定帧定时器
    /// </summary>
    public void ResumeFrameTimer(int id)
    {
        lock (lockObject)
        {
            if (frameTimers.TryGetValue(id, out FrameData data))
            {
                data.IsPaused = false;
                #if UNITY_EDITOR || DEBUG
                if (EnableDebugLog) 
                    Debug.Log($"恢复帧定时器: ID {id}, 剩余帧数: {data.RemainingFrames}");
                #endif
            }
        }
    }

    /// <summary>
    /// 检查定时器是否激活
    /// </summary>
    public bool IsFrameTimerActive(int id)
    {
        lock (lockObject)
        {
            return frameTimers.ContainsKey(id);
        }
    }

    /// <summary>
    /// 获取定时器剩余帧数
    /// </summary>
    public int GetRemainingFrames(int id)
    {
        lock (lockObject)
        {
            if (frameTimers.TryGetValue(id, out var data))
            {
                return Mathf.Max(0, data.RemainingFrames);
            }
            return -1;
        }
    }

    /// <summary>
    /// 移除帧定时器
    /// </summary>
    public void RemoveFrameTimer(int id)
    {
        lock (lockObject)
        {
            if (frameTimers.TryGetValue(id, out FrameData data))
            {
                ReturnFrameDataToPool(data);
                frameTimers.Remove(id);
                #if UNITY_EDITOR || DEBUG
                if (EnableDebugLog) Debug.Log($"移除帧定时器: ID {id}");
                #endif
            }
        }
    }

    #endregion

    public override void Dispose()
    {
        // 取消事件注册
        Application.focusChanged -= OnApplicationFocusChanged;
        
        lock (lockObject)
        {
            foreach (var timer in frameTimers.Values)
            {
                ReturnFrameDataToPool(timer);
            }
            frameTimers.Clear();
            frameDataPool.Clear();
        }
    }
}