using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;


public abstract class SingletonMod<T>  where T : class, IMod, new()
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                }
                return instance;
            }
        }
        bool initialized = false;
        public abstract void RegisterMessageHandler();
        public abstract void UnregisterMessageHandler();
        public bool Initialized => initialized;

        public SingletonMod()
        {
            if (instance != null)
            {
                throw new InvalidOperationException("Cannot create instances of a singleton class.");
            }
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

        //注册事件 virtual method
        public virtual void RegisterEvent()
        {
           
        }
        //取消注册事件 virtual method
        public virtual void UnregisterEvent()
        {
           
        }
        
        
    
    
        public void SendMessage<T>(T message) where T : IMessage
        {
            int protoId = ProtoManager.Instance.GetProtoIdByType(typeof(T));
            NetClientManager.Instance.SendMessageAsync(message, protoId);
        }
        public virtual void Initialize()
        {
            // Default implementation, can be overridden in derived classes
            ModManager.Instance.RegisterMod(Instance);
            RegisterMessageHandler();
            RegisterEvent();
            initialized = true;
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
            Debug.Log(Instance.ToString()+" OnApplicationFocus"+hasFocus);
        }

        public virtual void OnApplicationPause(bool pauseStatus)
        {
            // Default implementation, can be overridden in derived classes
            Debug.Log(Instance.ToString()+"OnApplicationPause"+pauseStatus);
        }

        public virtual void OnApplicationQuit()
        {
            // Default implementation, can be overridden in derived classes
            Debug.Log(Instance.ToString()+" OnApplicationQuit");
        }

        public virtual void AllModInitialize()
        {
            // Default implementation, can be overridden in derived classes
            Debug.Log(Instance.ToString()+" AllModInitialize");
        }
        public virtual void Dispose()
        {
            // Default implementation, can be overridden in derived classes
            Debug.Log(Instance.ToString()+" Destroy");
            UnregisterMessageHandler();
            UnregisterEvent();
        }
        
        
    }
