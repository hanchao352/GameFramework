using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using cfg;
using Luban;

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
#if UNITY_EDITOR
    private string _configDirectory = Application.dataPath + "/Res/Config";
#elif UNITY_ANDROID
    private string _configDirectory = "DataTables";
#elif UNITY_IOS
    private string _configDirectory = "DataTables";
#elif UNITY_STANDALONE_WIN
    private string _configDirectory = "DataTables";
#endif
    
    
    /// <summary>
    /// 是否启用调试日志
    /// </summary>
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
    
    
    
    /// <summary>
    /// 配置文件扩展名过滤
    /// </summary>
    private string[] _configFileExtensions = { ".json", ".txt" };
    


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
            LogDebug($"开始加载配置数据... (平台: {Application.platform})");
            
            // 检查配置目录是否存在
            string fullPath = _configDirectory;
            LogDebug($"使用配置路径: {fullPath}");
            
            if (!Directory.Exists(fullPath))
            {
                LogError($"配置目录不存在: {fullPath} (平台: {Application.platform})");
                OnConfigLoadFailed?.Invoke($"配置目录不存在: {fullPath}");
                return false;
            }
            
            // 创建配置表实例
            ConfigTables = new Tables(LoadConfigFileByBuf);
            
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
    /// 配置文件加载器
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>JSON节点</returns>
    private JSONNode LoadConfigFile(string fileName)
    {
        try
        {
            string filePath = Path.Combine(_configDirectory, $"{fileName}.json");
            
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
    
    private ByteBuf LoadConfigFileByBuf(string fileName)
    {
        try
        {
            string filePath = Path.Combine(_configDirectory, $"{fileName}.bytes");
            
            if (!File.Exists(filePath))
            {
                LogError($"配置文件不存在: {filePath}");
                throw new FileNotFoundException($"配置文件不存在: {filePath}");
            }
            
            
            
            
            byte[] bytes = File.ReadAllBytes(filePath);
            
            if (bytes == null)
            {
                LogError($"配置文件解析失败: {filePath}");
                throw new InvalidDataException($"配置文件解析失败: {filePath}");
            }
            
            LogDebug($"成功加载配置文件: {fileName}.bytes");
            return new ByteBuf(bytes);
        }
        catch (Exception ex)
        {
            LogError($"加载配置文件 {fileName} 失败: {ex.Message}");
            throw;
        }
    }
    
    #endregion
    
    
    
    #region 工具方法
    
    /// <summary>
    /// 设置配置目录路径（默认平台）
    /// </summary>
    /// <param name="directory">目录路径</param>
    public void SetConfigDirectory(string directory)
    {
        _configDirectory = directory;
        LogDebug($"默认配置目录已设置为: {directory}");
    }
    
    
    
    /// <summary>
    /// 设置支持的配置文件扩展名
    /// </summary>
    /// <param name="extensions">扩展名数组</param>
    public void SetConfigFileExtensions(string[] extensions)
    {
        _configFileExtensions = extensions;
        LogDebug($"配置文件扩展名已设置为: {string.Join(", ", extensions)}");
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
            Debug.Log($"[TableManager] {message}");
        }
    }
    
    /// <summary>
    /// 输出错误日志
    /// </summary>
    /// <param name="message">错误信息</param>
    private void LogError(string message)
    {
        Debug.LogError($"[TableManager] {message}");
    }
    
    #endregion
    
   


   


}
