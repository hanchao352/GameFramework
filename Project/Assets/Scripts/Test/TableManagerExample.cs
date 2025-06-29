using UnityEngine;
using cfg;

/// <summary>
/// CfgManager使用示例
/// 演示如何在游戏中使用配置管理器
/// </summary>
public class TableManagerExample : MonoBehaviour
{
    [SerializeField] private int _testRewardId = 1001;

    void Awake()
    {
        TableManager.Instance.Initialize();
    }
    
    private void Start()
    {
        // 订阅配置加载事件
        TableManager.Instance.OnConfigLoaded += OnConfigLoaded;
        TableManager.Instance.OnConfigLoadFailed += OnConfigLoadFailed;

        // 如果配置已经加载完成，直接执行测试
        if (TableManager.Instance.IsConfigLoaded)
        {
            TestConfigAccess();
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (TableManager.Instance != null)
        {
            TableManager.Instance.OnConfigLoaded -= OnConfigLoaded;
            TableManager.Instance.OnConfigLoadFailed -= OnConfigLoadFailed;
        }
    }
    
    /// <summary>
    /// 配置加载完成回调
    /// </summary>
    private void OnConfigLoaded()
    {
        Debug.Log("配置加载完成，开始测试配置访问");
        TestConfigAccess();
    }
    
    /// <summary>
    /// 配置加载失败回调
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    private void OnConfigLoadFailed(string errorMessage)
    {
        Debug.LogError($"配置加载失败: {errorMessage}");
    }
    
    /// <summary>
    /// 测试配置访问
    /// </summary>
    private void TestConfigAccess()
    {
        // 1. 测试获取单个奖励配置
        TestGetSingleReward();
        
        // 2. 测试获取所有奖励配置
        TestGetAllRewards();
        
        // 3. 测试检查奖励是否存在
        TestCheckRewardExists();
    }
    
    /// <summary>
    /// 测试获取单个奖励配置
    /// </summary>
    private void TestGetSingleReward()
    {
        var reward = TableManager.Instance.ConfigTables.TbEndReward.Get(_testRewardId);
        if (reward != null)
        {
            Debug.Log($"获取奖励配置成功:");
            Debug.Log($"  ID: {reward.Id}");
            Debug.Log($"  名称: {reward.Name}");
            Debug.Log($"  描述: {reward.Desc}");
            Debug.Log($"  数量: {reward.Count}");
        }
        else
        {
            Debug.LogWarning($"未找到ID为 {_testRewardId} 的奖励配置");
        }
    }
    
    /// <summary>
    /// 测试获取所有奖励配置
    /// </summary>
    private void TestGetAllRewards()
    {
        var allRewards = TableManager.Instance.ConfigTables.TbEndReward.DataList;
        Debug.Log($"总共有 {allRewards.Count} 个奖励配置:");
        
        foreach (var reward in allRewards)
        {
            Debug.Log($"  [{reward.Id}] {reward.Name} - {reward.Desc} x{reward.Count}");
        }
    }
    
    /// <summary>
    /// 测试检查奖励是否存在
    /// </summary>
    private void TestCheckRewardExists()
    {
        // 测试存在的ID
        bool exists1 = TableManager.Instance.ConfigTables.TbEndReward.Get(1001) != null;
        Debug.Log($"奖励ID 1001 是否存在: {exists1}");
        
        // 测试不存在的ID
        bool exists2 = TableManager.Instance.ConfigTables.TbEndReward.DataMap.TryGetValue(9999, out _);
        Debug.Log($"奖励ID 9999 是否存在: {exists2}");
    }
    
  
    
} 