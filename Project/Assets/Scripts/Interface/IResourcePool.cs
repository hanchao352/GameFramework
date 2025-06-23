public interface IResourcePool
{
    /// <summary>
    /// 清空池中所有资源
    /// </summary>
    void Clear();
    
    /// <summary>
    /// 释放指定路径的资源
    /// </summary>
    void Release(string path);
    
    /// <summary>
    /// 获取池中资源数量
    /// </summary>
    int ResourceCount { get; }
}