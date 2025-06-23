using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

public abstract class UIBase : IViewGeneric
{
    public ViewInfo viewInfo;
    public object[] Args;
    public GameObject Root { get; set; }
    public List<ComponentBase> childComponents { get; set; }
    public int CurrentOrder { get; set; } = -1;
    public bool Isvisible => isvisible;
    
    // 添加取消令牌源
    private CancellationTokenSource _cts;
    private bool isvisible = false;
    
    public async UniTask<GameObject> CreateRoot()
    {
        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;
        
        try
        {
            // 使用可取消的异步操作
            GameObject root = await CreateRootAsync(cancellationToken);
            Root = root; 
            return root;
        }
        catch (OperationCanceledException)
        {
            // 清理已创建的部分资源
            if (Root != null)
            {
                Object.Destroy(Root);
                Root = null;
            }
            throw; // 重新抛出异常
        }
        finally
        {
            // 清理取消令牌
            _cts?.Dispose();
            _cts = null;
        }
    }

    // 分离实际的异步创建逻辑
    private async UniTask<GameObject> CreateRootAsync(CancellationToken cancellationToken)
    {
        // 模拟耗时操作，使用可取消的Delay
        await UniTask.Delay(1000, cancellationToken: cancellationToken);
        
        // 创建UI根对象
        return new GameObject($"{GetType().Name}_Root");
    }

    // 添加取消加载的方法
    public void CancelLoading()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        
        // 清理可能已创建的部分资源
        if (Root != null)
        {
            Object.Destroy(Root);
            Root = null;
        }
    }

    public virtual void Initialize()
    {
        childComponents = new List<ComponentBase>();
    }

    public virtual void OnShow(params object[] args)
    {
        isvisible = true;
        // 确保激活Root
        if (Root != null && !Root.activeSelf)
        {
            Root.SetActive(true);
        }
    }

    public virtual void UpdateView(params object[] args)
    {
        Args = args;
        for (int i = 0; i < childComponents.Count; i++)
        {
            childComponents[i].UpdateView(); 
        }
    }

    public virtual void Update(float time)
    {
        for (int i = 0; i < childComponents.Count; i++)
        {
            childComponents[i].Update(time);
        }
    }

    public virtual void OnApplicationFocus(bool hasFocus) { }
    public virtual void OnApplicationPause(bool pauseStatus) { }
    public virtual void OnApplicationQuit() { }

    public virtual void OnHide()
    {
        isvisible = false;
        if (Root != null)
        {
            Root.SetActive(false);
        }
        for (int i = 0; i < childComponents.Count; i++)
        {
            childComponents[i].Visible = false;
        }
    }

    public virtual void OnEnterMutex()
    {
        for (int i = 0; i < childComponents.Count; i++)
        {
            childComponents[i].OnEnterMutex();
        }
    }

    public virtual void OnExitMutex()
    {
        for (int i = 0; i < childComponents.Count; i++)
        {
            childComponents[i].OnExitMutex();
        }
    }

    public virtual void Dispose()
    {
        // 取消任何进行中的加载
        CancelLoading();
        
        for (int i = 0; i < childComponents.Count; i++)
        {
            childComponents[i].Dispose();
        }
        
        if (Root != null)
        {
            Object.Destroy(Root);
            Root = null;
        }
    }
    
    public virtual T MakeComponent<T>(GameObject comroot) where T : ComponentBase
    {
        T com = comroot.MakeComponent<T>();
        if (childComponents.Contains(com) == false)
        {
            childComponents.Add(com);
        }
        return com;
    }
    
    public void SetParent(Transform parent)
    {
        if (Root != null)
        {
            Root.transform.SetParent(parent, false);
        }
    }
    
    public void SetActive(bool active)
    {
        if (Root != null)
        {
            Root.SetActive(active);
        }
    }
    
    public bool IsActive()
    {
        return Root != null && Root.activeSelf;
    }
}