using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using cfg;
using Cysharp.Threading.Tasks;
using Luban;
using Debug = UnityEngine.Debug;

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
    private string _configDirectory = Application.persistentDataPat + "/Res/Config";
#elif UNITY_IOS
    private string _configDirectory = Application.persistentDataPat + "/Res/Config";
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

    #endregion

    public override void Initialize()
    {
      
    }


    #region 配置加载

    /// <summary>
    /// 加载配置数据
    /// </summary>
    /// <returns>是否加载成功</returns>
    public async UniTask<bool> LoadConfiguration()
    {
        try
        {
            Debug.Log($"开始加载配置数据... (平台: {Application.platform})");
            // 检查配置目录是否存在
            string fullPath = _configDirectory;
            Debug.Log($"使用配置路径: {fullPath}");

            if (!Directory.Exists(fullPath))
            {
                Debug.LogError($"配置目录不存在: {fullPath} (平台: {Application.platform})");
                OnConfigLoadFailed?.Invoke($"配置目录不存在: {fullPath}");
                return false;
            }

           
            //同步创建配表示例
            //ConfigTables = new Tables(LoadConfigFileByBuf);
            //Debug.Log(a.ElapsedMilliseconds);
            
            // 异步创建配置表实例
            ConfigTables = new Tables();
            await ConfigTables.InitializeAsync(LoadConfigFileByBufAsync);
            IsConfigLoaded = true;
            Debug.Log("配置数据加载完成");


            // 触发加载完成事件
            OnConfigLoaded?.Invoke();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"配置加载失败: {ex.Message}\n{ex.StackTrace}");
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
                Debug.LogError($"配置文件不存在: {filePath}");
                throw new FileNotFoundException($"配置文件不存在: {filePath}");
            }

            string jsonContent = File.ReadAllText(filePath);

            JSONNode jsonNode = JSON.Parse(jsonContent);

            if (jsonNode == null)
            {
                Debug.LogError($"配置文件解析失败: {filePath}");
                throw new InvalidDataException($"配置文件解析失败: {filePath}");
            }

            Debug.Log($"成功加载配置文件: {fileName}.json");
            return jsonNode;
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载配置文件 {fileName} 失败: {ex.Message}");
            throw;
        }
    }

    private async UniTask<JSONNode> LoadConfigFileAsync(string fileName)
    {
        try
        {
            string filePath = Path.Combine(_configDirectory, $"{fileName}.json");

            if (!File.Exists(filePath))
            {
                Debug.LogError($"配置文件不存在: {filePath}");
                throw new FileNotFoundException($"配置文件不存在: {filePath}");
            }

            string jsonContent = await File.ReadAllTextAsync(filePath);

            JSONNode jsonNode = JSON.Parse(jsonContent);

            if (jsonNode == null)
            {
                Debug.LogError($"配置文件解析失败: {filePath}");
                throw new InvalidDataException($"配置文件解析失败: {filePath}");
            }

            Debug.Log($"成功加载配置文件: {fileName}.json");
            return jsonNode;
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载配置文件 {fileName} 失败: {ex.Message}");
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
                Debug.LogError($"配置文件不存在: {filePath}");
                throw new FileNotFoundException($"配置文件不存在: {filePath}");
            }

            byte[] bytes = File.ReadAllBytes(filePath);
            if (bytes == null)
            {
                Debug.LogError($"配置文件解析失败: {filePath}");
                throw new InvalidDataException($"配置文件解析失败: {filePath}");
            }

            Debug.Log($"成功加载配置文件: {fileName}.bytes");
            return new ByteBuf(bytes);
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载配置文件 {fileName} 失败: {ex.Message}");
            throw;
        }
    }


    private async UniTask<ByteBuf> LoadConfigFileByBufAsync(string fileName)
    {
        try
        {
            string filePath = Path.Combine(_configDirectory, $"{fileName}.bytes");

            if (!File.Exists(filePath))
            {
                Debug.LogError($"配置文件不存在: {filePath}");
                throw new FileNotFoundException($"配置文件不存在: {filePath}");
            }

            byte[] bytes = await File.ReadAllBytesAsync(filePath);
            if (bytes == null)
            {
                Debug.LogError($"配置文件解析失败: {filePath}");
                throw new InvalidDataException($"配置文件解析失败: {filePath}");
            }

            Debug.Log($"成功加载配置文件: {fileName}.bytes");
            return new ByteBuf(bytes);
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载配置文件 {fileName} 失败: {ex.Message}");
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
        Debug.Log($"默认配置目录已设置为: {directory}");
    }

    /// <summary>
    /// 启用或禁用调试日志
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void EnableDebugLog(bool enable)
    {
        _enableDebugLog = enable;
    }

    #endregion
}