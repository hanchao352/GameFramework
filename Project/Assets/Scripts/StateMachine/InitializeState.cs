using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InitializeState : IGameState
{
    private readonly IGameStateMachine _stateMachine;
    
    public GameStateId StateId => GameStateId.Initialize;

    public InitializeState(IGameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public async UniTask OnEnterAsync(StateContext context)
    {
        Debug.Log("=== 开始初始化游戏 ===");
        
        try
        {
            
            // 1. 自动发现并初始化所有Manager
            var managers = await InitializeAllManagersAsync();
            
            // 2. 自动发现并初始化所有Mod
            var mods = await InitializeAllModsAsync();
            
            // 3. 调用所有Manager的AllManagerInitialize
            await CallAllManagerInitializeAsync(managers);
            
            // 4. 调用所有Mod的AllModInitialize
            await CallAllModInitializeAsync(mods);
            
            // 5. 将Manager和Mod保存到Context中
            context.Managers = managers;
            context.Mods = mods;
            
            // 也可以保存其他数据到Context
            context.SetData("InitializeTime", DateTime.Now);
            context.SetData("GameVersion", Application.version);
            await _stateMachine.ChangeStateAsync(GameStateId.PreloadResource);
            Debug.Log("=== 游戏初始化完成 ===");
        }
        catch (Exception e)
        {
            Debug.LogError($"游戏初始化失败: {e}");
            Debug.LogError($"错误堆栈: {e.StackTrace}");
            throw;
        }
    }

    public void OnUpdate(float deltaTime, StateContext context)
    {
        // 更新所有Manager
        if (context.Managers != null)
        {
            foreach (var manager in context.Managers)
            {
                try
                {
                    manager.Update(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Manager Update失败 - {manager.GetType().Name}: {e.Message}");
                }
            }
        }
        
        // 更新所有Mod
        if (context.Mods != null)
        {
            foreach (var mod in context.Mods)
            {
                try
                {
                    mod.Update(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Mod Update失败 - {mod.GetType().Name}: {e.Message}");
                }
            }
        }
    }

    public async UniTask OnExitAsync(StateContext context)
    {
        Debug.Log("退出初始化状态");
        await UniTask.CompletedTask;
    }

    

    /// <summary>
    /// 自动发现并初始化所有Manager
    /// </summary>
    private async UniTask<List<IGeneric>> InitializeAllManagersAsync()
    {
        Debug.Log("开始自动发现并初始化所有Manager...");
        
        var managers = new List<IGeneric>();
        
        try
        {
            // 通过反射查找所有实现IGeneric接口的类型
            var managerTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IGeneric).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var type in managerTypes)
            {
                try
                {
                    // 获取静态属性 "Instance"
                    var instanceProperty = type.GetProperty("Instance", 
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    
                    if (instanceProperty != null)
                    {
                        // 通过 "Instance" 属性获取单例实例
                        var managerInstance = instanceProperty.GetValue(null, null) as IGeneric;
                        if (managerInstance != null)
                        {
                            managers.Add(managerInstance);
                            Debug.Log($"发现Manager: {type.Name}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"获取Manager实例失败 - {type.Name}: {e.Message}");
                }
            }
            
            Debug.Log($"共发现 {managers.Count} 个Manager");
            
            // 初始化所有Manager
            for (int i = 0; i < managers.Count; i++)
            {
                try
                {
                    Debug.Log($"初始化Manager [{i + 1}/{managers.Count}]: {managers[i].GetType().Name}");
                    managers[i].Initialize();
                    await UniTask.Yield();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Manager初始化失败 - {managers[i].GetType().Name}: {e}");
                }
            }
            
            Debug.Log("所有Manager初始化完成");
        }
        catch (Exception e)
        {
            Debug.LogError($"Manager发现和初始化过程失败: {e}");
        }
        
        return managers;
    }

    /// <summary>
    /// 自动发现并初始化所有Mod
    /// </summary>
    private async UniTask<List<IMod>> InitializeAllModsAsync()
    {
        Debug.Log("开始自动发现并初始化所有Mod...");
        
        var mods = new List<IMod>();
        
        try
        {
            // 通过反射查找所有实现IMod接口的类型
            var modTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IMod).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var mod in modTypes)
            {
                try
                {
                    // 获取静态属性 "Instance"
                    var instanceProperty = mod.GetProperty("Instance", 
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    
                    if (instanceProperty != null)
                    {
                        // 通过 "Instance" 属性获取单例实例
                        var modInstance = instanceProperty.GetValue(null, null) as IMod;
                        if (modInstance != null)
                        {
                            mods.Add(modInstance);
                            Debug.Log($"发现Mod: {mod.Name}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"获取Mod实例失败 - {mod.Name}: {e.Message}");
                }
            }
            
            Debug.Log($"共发现 {mods.Count} 个Mod");
            
            // 初始化所有Mod
            for (int i = 0; i < mods.Count; i++)
            {
                try
                {
                    Debug.Log($"初始化Mod [{i + 1}/{mods.Count}]: {mods[i].GetType().Name}");
                    mods[i].Initialize();
                    await UniTask.Yield();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Mod初始化失败 - {mods[i].GetType().Name}: {e}");
                }
            }
            
            Debug.Log("所有Mod初始化完成");
        }
        catch (Exception e)
        {
            Debug.LogError($"Mod发现和初始化过程失败: {e}");
        }
        
        return mods;
    }

    /// <summary>
    /// 调用所有Manager的AllManagerInitialize方法
    /// </summary>
    private async UniTask CallAllManagerInitializeAsync(List<IGeneric> managers)
    {
        Debug.Log("调用所有Manager的AllManagerInitialize...");
        
        if (managers == null || managers.Count == 0)
        {
            Debug.LogWarning("没有Manager需要调用AllManagerInitialize");
            return;
        }
        
        for (int i = 0; i < managers.Count; i++)
        {
            try
            {
                Debug.Log($"调用AllManagerInitialize [{i + 1}/{managers.Count}]: {managers[i].GetType().Name}");
                managers[i].AllManagerInitialize();
                await UniTask.Yield();
            }
            catch (Exception e)
            {
                Debug.LogError($"AllManagerInitialize调用失败 - {managers[i].GetType().Name}: {e}");
            }
        }
        
        Debug.Log("所有Manager的AllManagerInitialize调用完成");
    }

    /// <summary>
    /// 调用所有Mod的AllModInitialize方法
    /// </summary>
    private async UniTask CallAllModInitializeAsync(List<IMod> mods)
    {
        Debug.Log("调用所有Mod的AllModInitialize...");
        
        if (mods == null || mods.Count == 0)
        {
            Debug.LogWarning("没有Mod需要调用AllModInitialize");
            return;
        }
        
        for (int i = 0; i < mods.Count; i++)
        {
            try
            {
                Debug.Log($"调用AllModInitialize [{i + 1}/{mods.Count}]: {mods[i].GetType().Name}");
                mods[i].AllModInitialize();
                await UniTask.Yield();
            }
            catch (Exception e)
            {
                Debug.LogError($"AllModInitialize调用失败 - {mods[i].GetType().Name}: {e}");
            }
        }
        
        Debug.Log("所有Mod的AllModInitialize调用完成");
    }
}
