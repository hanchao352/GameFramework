using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Google.Protobuf;
using System.Threading.Tasks;
using UnityEngine;


public static class ObjectCreator
{
    private static readonly ConcurrentDictionary<Type, Func<object>> CreatorCache = 
        new ConcurrentDictionary<Type, Func<object>>();

    // 泛型版本（类型安全且高效）
    public static T CreateInstance<T>() where T : new()
    {
        return GenericCreator<T>.Creator();
    }

    // 非泛型版本（兼容原始逻辑）
    public static object CreateInstance(Type type)
    {
        if (!CreatorCache.TryGetValue(type, out var creator))
        {
            var newExpr = Expression.New(type);
            var lambda = Expression.Lambda<Func<object>>(
                Expression.Convert(newExpr, typeof(object))
            );
            creator = lambda.Compile();
            CreatorCache[type] = creator;
        }
        return creator();
    }

    // 内部泛型缓存类（每个T独立缓存）
    private static class GenericCreator<T> where T : new()
    {
        public static readonly Func<T> Creator = BuildCreator();

        private static Func<T> BuildCreator()
        {
            // 不需要类型转换，直接返回T类型
            var newExpr = Expression.New(typeof(T));
            var lambda = Expression.Lambda<Func<T>>(newExpr);
            return lambda.Compile();
        }
    }
}

public static class UtilHelper
{
        
        
        public  static void  ToSend<T>(this T message) where T : IMessage
        {
            int protoId = ProtoManager.Instance.GetProtoIdByType(typeof(T));
             NetClientManager.Instance.SendMessageAsync(message, protoId);
        }
    
        
}

public static class UIUtilHleper
{
    public static T MakeComponent<T>(this GameObject gameObject) where T:ComponentBase
    {
        T component = (T)ObjectCreator.CreateInstance(typeof(T));
        component.Root = gameObject;
        component.Initialize();
        return component;
    }
    
}
