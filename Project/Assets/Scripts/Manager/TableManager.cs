using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using cfg;

public class TableManager : SingletonManager<TableManager>, IGeneric
{
   
      #region 配置相关字段
    
    /// <summary>
    /// 配置表实例
    /// </summary>
    public Tables ConfigTables { get; private set; }
    
    /// <summary>
    /// 配置文件目录路径
    /// </summary>
    [SerializeField]
    private string _configDirectory = "../DataTables/output";
    
    
    /// <summary>
    /// 是否启用调试日志
    /// </summary>
    [SerializeField]
    private bool _enableDebugLog = true;
    
    /// <summary>
    /// 配置是否已加载
    /// </summary>
    public bool IsConfigLoaded { get; private set; }
    
    /// <summary>
    /// 配置加载完成事件
    /// </summary>
    public event Action OnConfigLoaded;
    
    /// <summary>
    /// 配置加载失败事件
    /// </summary>
    public event Action<string> OnConfigLoadFailed;

    #endregion

    public override void Initialize()
    {
        LoadConfiguration();
    }
    
    #region 配置加载
    
    /// <summary>
    /// 加载配置数据
    /// </summary>
    /// <returns>是否加载成功</returns>
    public bool LoadConfiguration()
    {
        try
        {
            LogDebug("开始加载配置数据...");
            
            // 检查配置目录是否存在
            string fullPath = GetConfigDirectoryPath();
            if (!Directory.Exists(fullPath))
            {
                LogError($"配置目录不存在: {fullPath}");
                OnConfigLoadFailed?.Invoke($"配置目录不存在: {fullPath}");
                return false;
            }
            
            // 创建配置表实例
            ConfigTables = new Tables(LoadConfigFile);
            
            IsConfigLoaded = true;
            LogDebug("配置数据加载完成");
            
            // 触发加载完成事件
            OnConfigLoaded?.Invoke();
            
            return true;
        }
        catch (Exception ex)
        {
            LogError($"配置加载失败: {ex.Message}\n{ex.StackTrace}");
            OnConfigLoadFailed?.Invoke(ex.Message);
            IsConfigLoaded = false;
            return false;
        }
    }
    
    /// <summary>
    /// 重新加载配置数据
    /// </summary>
    /// <returns>是否重载成功</returns>
    public bool ReloadConfiguration()
    {
        LogDebug("重新加载配置数据...");
        IsConfigLoaded = false;
        ConfigTables = null;
        return LoadConfiguration();
    }
    
    /// <summary>
    /// 配置文件加载器
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>JSON节点</returns>
    private JSONNode LoadConfigFile(string fileName)
    {
        try
        {
            string filePath = Path.Combine(GetConfigDirectoryPath(), $"{fileName}.json");
            
            if (!File.Exists(filePath))
            {
                LogError($"配置文件不存在: {filePath}");
                throw new FileNotFoundException($"配置文件不存在: {filePath}");
            }
            
            string jsonContent = File.ReadAllText(filePath);
            JSONNode jsonNode = JSON.Parse(jsonContent);
            
            if (jsonNode == null)
            {
                LogError($"配置文件解析失败: {filePath}");
                throw new InvalidDataException($"配置文件解析失败: {filePath}");
            }
            
            LogDebug($"成功加载配置文件: {fileName}.json");
            return jsonNode;
        }
        catch (Exception ex)
        {
            LogError($"加载配置文件 {fileName} 失败: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// 获取配置目录的完整路径
    /// </summary>
    /// <returns>配置目录路径</returns>
    private string GetConfigDirectoryPath()
    {
        // 支持相对路径和绝对路径
        if (Path.IsPathRooted(_configDirectory))
        {
            return _configDirectory;
        }
        else
        {
            return Path.Combine(Application.dataPath, "..", _configDirectory);
        }
    }
    
    #endregion
    
    #region 配置访问接口
    
    /// <summary>
    /// 获取奖励配置表
    /// </summary>
    public cfg.demo.TbReward GetRewardTable()
    {
        if (!IsConfigLoaded)
        {
            LogError("配置未加载，无法获取奖励配置表");
            return null;
        }
        return ConfigTables.TbReward;
    }
    
    /// <summary>
    /// 根据ID获取奖励配置
    /// </summary>
    /// <param name="id">奖励ID</param>
    /// <returns>奖励配置，如果不存在返回null</returns>
    public cfg.demo.Reward GetReward(int id)
    {
        var rewardTable = GetRewardTable();
        if (rewardTable == null)
        {
            return null;
        }

        if(rewardTable.DataMap.TryGetValue(id, out var reward))
        {
            return reward;
        }

        return null;
     
    }
    
    /// <summary>
    /// 检查奖励配置是否存在
    /// </summary>
    /// <param name="id">奖励ID</param>
    /// <returns>是否存在</returns>
    public bool HasReward(int id)
    {
        return GetReward(id) != null;
    }
    
    /// <summary>
    /// 获取所有奖励配置
    /// </summary>
    /// <returns>奖励配置列表</returns>
    public IReadOnlyList<cfg.demo.Reward> GetAllRewards()
    {
        var rewardTable = GetRewardTable();
        if (rewardTable == null)
        {
            return new List<cfg.demo.Reward>();
        }
        
        return rewardTable.DataList;
    }
    
    #endregion
    
    #region 工具方法
    
    /// <summary>
    /// 设置配置目录路径
    /// </summary>
    /// <param name="directory">目录路径</param>
    public void SetConfigDirectory(string directory)
    {
        _configDirectory = directory;
        LogDebug($"配置目录已设置为: {directory}");
    }
    
    /// <summary>
    /// 启用或禁用调试日志
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void EnableDebugLog(bool enable)
    {
        _enableDebugLog = enable;
    }
    
    /// <summary>
    /// 输出调试日志
    /// </summary>
    /// <param name="message">日志信息</param>
    private void LogDebug(string message)
    {
        if (_enableDebugLog)
        {
            Debug.Log($"[CfgManager] {message}");
        }
    }
    
    /// <summary>
    /// 输出错误日志
    /// </summary>
    /// <param name="message">错误信息</param>
    private void LogError(string message)
    {
        Debug.LogError($"[CfgManager] {message}");
    }
    
    #endregion
    
   


   


}
