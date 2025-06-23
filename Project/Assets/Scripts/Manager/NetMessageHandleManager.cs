using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Google.Protobuf;

public class NetMessageHandleManager:SingletonManager<NetMessageHandleManager>,IGeneric
{
   
    private readonly Dictionary<int, Func<IMessage, UniTask>> _callbacks = new Dictionary<int, Func<IMessage, UniTask>>();
    private  Dictionary<Type, Dictionary<int, Func<IMessage, UniTask>>> _messageIdToCallback;
    public override void Initialize()
    {
        base.Initialize();
        _messageIdToCallback = new Dictionary<Type, Dictionary<int, Func<IMessage, UniTask>>>();
    }

    public override void Update(float time)
    {
        base.Update(time);
    }

    public override void OnApplicationFocus(bool hasFocus)
    {
        base.OnApplicationFocus(hasFocus);
    }

    public override void OnApplicationPause(bool pauseStatus)
    {
        base.OnApplicationPause(pauseStatus);
    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }

    public override void Dispose()
    {
        base.Dispose();
        _messageIdToCallback.Clear();
    }


    public  void RegisterCallback<T>(int protoId, Func<T, UniTask> callback) where T : IMessage<T>
    {
        Type messageType = typeof(T);
        if (!_messageIdToCallback.ContainsKey(messageType))
        {
            _messageIdToCallback[messageType] = new Dictionary<int, Func<IMessage, UniTask>>();
        }
        _messageIdToCallback[messageType][protoId] = (message) =>  callback((T)message);
    }
    public  void UnregisterCallback<T>(int protoId) where T : IMessage<T>
    {
        Type messageType = typeof(T);
        if (_messageIdToCallback.ContainsKey(messageType))
        {
            _messageIdToCallback[messageType].Remove(protoId);
        }
    }
    public  async Task<bool> InvokeCallback(int protoId, IMessage message)
    {

        Type messageType = message.GetType();
        if (_messageIdToCallback.TryGetValue(messageType, out Dictionary<int, Func<IMessage, UniTask>> callbackDict))
        {
            if (callbackDict.TryGetValue(protoId, out Func<IMessage, UniTask> callback))
            {
                await callback(message);
                return true;
            }
        }
        return false;
    }
    internal bool TryGetCallback(int protoId, out Func<IMessage, UniTask> callback)
    {
        return _callbacks.TryGetValue(protoId, out callback);
    }
}
