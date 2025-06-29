using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[ManagerAttribute]
public partial class ProtoManager:SingletonManager<ProtoManager>, IGeneric
{
    private   Dictionary<int, Type> _protoIdToType;
    private   Dictionary<Type, int> _typeToProtoId;



    private void RegisterAllProtos()
    {
        var protoIds = typeof(ProtosMsgID).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(int));

        foreach (var protoIdField in protoIds)
        {
            int protoId = (int)protoIdField.GetValue(null);
            
            string messageTypeName = protoIdField.Name;
            Type messageType = Type.GetType(messageTypeName); 

            if (messageType != null)
            {
                RegisterProto(messageType, protoId);
            }
        }
    }
    private void RegisterProto(Type messageType, int protoId)
    {
        _protoIdToType[protoId] = messageType;
        _typeToProtoId[messageType] = protoId;
    }
     public override void Initialize()
     {
         base.Initialize();
         _protoIdToType = new Dictionary<int, Type>();
         _typeToProtoId = new Dictionary<Type, int>();
         RegisterAllProtos();
     }

     public override void Update(float time)
    {
        base.Update(time);
    }

     public override void Dispose()
     {
         base.Dispose();
     }

     public  void RegisterProto<T>(int protoId)
    {
        _protoIdToType[protoId] = typeof(T);
        _typeToProtoId[typeof(T)] = protoId;
    }

    public  Type GetTypeByProtoId(int protoId)
    {
        _protoIdToType.TryGetValue(protoId, out Type type);
        return type;
    }

    public  int GetProtoIdByType(Type type)
    {
        _typeToProtoId.TryGetValue(type, out int protoId);
        return protoId;
    }


}
