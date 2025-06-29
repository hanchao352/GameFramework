using System.Collections.Generic;

public class StateContext
{
    private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        
    /// <summary>
    /// 所有Manager列表
    /// </summary>
    public List<IGeneric> Managers { get; set; }
        
    /// <summary>
    /// 所有Mod列表
    /// </summary>
    public List<IMod> Mods { get; set; }
        
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData<T>(string key, T value)
    {
        _data[key] = value;
    }
        
    /// <summary>
    /// 获取数据
    /// </summary>
    public T GetData<T>(string key, T defaultValue = default)
    {
        if (_data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
        
    /// <summary>
    /// 是否包含指定键
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _data.ContainsKey(key);
    }
        
    /// <summary>
    /// 移除数据
    /// </summary>
    public bool RemoveData(string key)
    {
        return _data.Remove(key);
    }
        
    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        _data.Clear();
        Managers = null;
        Mods = null;
    }
}