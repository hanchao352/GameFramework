﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;

public class SingletonManager<T>  where T : class, IGeneric, new()
{
    private static T instance;
    private static object lockObject = new object();
    public static T Instance
    {
        get
        {
            lock (lockObject)
            {
                if (instance == null)
                {
                    instance = new T();
                }
                return instance;
            }
        }
    }

    protected SingletonManager()
    {
        if (instance != null)
        {
            throw new InvalidOperationException("Cannot create instances of a singleton class.");
        }
    }

    // Implement IManager interface with virtual methods so they can be overridden
    public virtual void Initialize()
    {
        // Default implementation, can be overridden in derived classes
        Debug.Log(Instance.ToString()+" Initialize");
      
    }

    public virtual void Update(float time)
    {
        // Default implementation, can be overridden in derived classes
        //Debug.Log(Instance.ToString()+" Update"+time);
    }

    public virtual void OnApplicationFocus(bool hasFocus)
    {
        // Default implementation, can be overridden in derived classes
        //Debug.Log(Instance.ToString()+" OnApplicationFocus"+hasFocus);
    }

    public virtual void OnApplicationPause(bool pauseStatus)
    {
        // Default implementation, can be overridden in derived classes
        //Debug.Log(Instance.ToString()+"OnApplicationPause"+pauseStatus);
    }

    public virtual void OnApplicationQuit()
    {
        // Default implementation, can be overridden in derived classes
        //Debug.Log(Instance.ToString()+" OnApplicationQuit");
    }

    public virtual void Dispose()
    {
        // Default implementation, can be overridden in derived classes
        //Debug.Log(Instance.ToString()+" Destroy");
        UnregisterMessageHandler();
    }

    protected void RegisterCallback<T>(Func<T, UniTask> callback) where T : IMessage<T>
    {
        int protoId = ProtoManager.Instance.GetProtoIdByType(typeof(T));
        NetMessageHandleManager.Instance.RegisterCallback(protoId, callback);
    }

    protected void UnregisterCallback<T>() where T : IMessage<T>
    {
        int protoId = ProtoManager.Instance.GetProtoIdByType(typeof(T));
        NetMessageHandleManager.Instance.UnregisterCallback<T>(protoId);
    }
    public virtual void AllManagerInitialize()
    {
        // Default implementation, can be overridden in derived classes
        RegisterMessageHandler();
        Debug.Log(Instance.ToString()+" AllManagerInitialize");
    }
    

    public virtual void RegisterMessageHandler()
    {
        
    }
    public virtual void UnregisterMessageHandler()
    {
        
    }

}