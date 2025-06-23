# GameFramework


## Luban 配置模块

### 1. 基本使用

```csharp
// 获取配置管理器实例
var tableManager = TableManager.Instance;

// 检查配置是否已加载
if (tableManager.IsConfigLoaded)
{
    // 获取奖励配置
    var reward = tableManager.GetReward(1001);
    if (reward != null)
    {
        Debug.Log($"奖励: {reward.Name}, 数量: {reward.Count}");
    }
}
```

### 2. 事件订阅

```csharp
private void Start()
{
    // 订阅配置加载事件
    TableManager.Instance.OnConfigLoaded += OnConfigLoaded;
    TableManager.Instance.OnConfigLoadFailed += OnConfigLoadFailed;
}

private void OnConfigLoaded()
{
    Debug.Log("配置加载完成");
    // 在这里执行需要配置数据的初始化逻辑
}

private void OnConfigLoadFailed(string errorMessage)
{
    Debug.LogError($"配置加载失败: {errorMessage}");
}

private void OnDestroy()
{
    // 取消订阅
    if (TableManager.Instance != null)
    {
        TableManager.Instance.OnConfigLoaded -= OnConfigLoaded;
        TableManager.Instance.OnConfigLoadFailed -= OnConfigLoadFailed;
    }
}
```


### 3. 代码设置

```csharp
// 设置配置目录
TableManager.Instance.SetConfigDirectory("CustomPath/output");

// 启用/禁用调试日志
TableManager.Instance.EnableDebugLog(false);

// 手动加载配置
TableManager.Instance.LoadConfiguration();
```

### 4. 示例代码

参考 `TableManagerExample.cs` 文件，其中包含了完整的使用示例和测试代码。

### 5. 常见问题

1. **配置加载失败**
   - 检查配置文件路径是否正确
   - 确认 JSON 文件格式是否有效
   - 查看 Console 中的错误日志

2. **获取配置返回 null**
   - 确认配置已加载（`IsConfigLoaded` 为 true）
   - 检查配置 ID 是否存在
   - 确认配置表是否正确生成

3. **事件不触发**
   - 确认事件订阅在配置加载前完成
   - 检查是否正确取消订阅避免内存泄漏 