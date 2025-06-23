using SimpleJSON;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
public class Main : MonoBehaviour
{
  
    public static Main Instance;
    // 加载脚本实例时调用 Awake
    private List<IGeneric>  managers;
    private List<IMod> mods;
    public void Awake()
    {
        DontDestroyOnLoad(this);
        var managerTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => typeof(IGeneric).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

         managers = new List<IGeneric>();

        foreach (var type in managerTypes)
        {
            // 获取静态属性 "Instance"
            var instanceProperty = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (instanceProperty != null)
            {
                // 通过 "Instance" 属性获取单例实例
                var managerInstance = instanceProperty.GetValue(null, null) as IGeneric;
                if (managerInstance != null)
                {
                    managers.Add(managerInstance);
                }
            }
        }
        for(int i = 0; i < managers.Count; i++)
        {
            managers[i].Initialize();
        }
        
        var modTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => typeof(IMod).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

         mods = new List<IMod>();

        foreach (var mod in modTypes)
        {
            // 获取静态属性 "Instance"
            var instanceProperty = mod.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (instanceProperty != null)
            {
                // 通过 "Instance" 属性获取单例实例
                var managerInstance = instanceProperty.GetValue(null, null) as IMod;
                if (managerInstance != null)
                {
                    mods.Add(managerInstance);
                }
            }
        }
        
        for(int i = 0; i < mods.Count; i++)
        {
            mods[i].Initialize();
        }
        for(int i = 0; i < managers.Count; i++)
        {
            managers[i].AllManagerInitialize();
        }
        for(int i = 0; i < mods.Count; i++)
        {
            mods[i].AllModInitialize();
        }
        NetClientManager.Instance.ConnectAsync();
        Instance = this;
        
    }
    public void DontDestroyOnChangeScene(GameObject target)
    {
        DontDestroyOnLoad(target);
    }
   







   
    public void OnEnable()
    {

    }

   
    public void Start()
    {
    
        UIManager.Instance.ShowView(UIName.UILogin);

    }
    
    public void Update()
    {
        
        float time = Time.deltaTime;
        foreach (var manager in managers)
        {
            manager.Update(time);
        }

       
    }
    
    public void OnDisable()
    {

    }
    
    public void OnDestroy()
    {
        foreach (var manager in managers)
        {
            manager.Dispose();
        }
       
    }


    private void OnApplicationFocus(bool hasFocus)
    {
        foreach (var manager in managers)
        {
            manager.OnApplicationFocus(hasFocus);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        foreach (var manager in managers)
        {
            manager.OnApplicationPause(pauseStatus);
        }
    }

    private void OnApplicationQuit()
    {
        foreach (var manager in managers)
        {
            manager.OnApplicationQuit();
        }
    }
    
}
