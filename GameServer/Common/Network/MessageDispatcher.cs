using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Google.Protobuf;

namespace Common.Network
{
    /// <summary>
    /// 简易的类型↔消息ID 映射注册中心（后面可以根据需要扩展）
    /// 在应用启动阶段，你需要像这样一一注册类型和 ID 映射：
    ///     MessageTypeRegistry.Register<LoginReq>(1001);
    ///     MessageTypeRegistry.Register<MoveReq>(1002);
    /// </summary>
    public static class MessageTypeRegistry
    {
        static readonly Dictionary<Type, int> _typeToId = new();
        public static void Register<T>(int msgId) where T : IMessage<T>
            => _typeToId[typeof(T)] = msgId;

        public static int GetMsgId<T>() where T : IMessage<T>
        {
            if (_typeToId.TryGetValue(typeof(T), out var id)) return id;
            throw new InvalidOperationException($"消息类型 {typeof(T)} 未注册 ID");
        }
    }

    public class MessageDispatcher
    {
        // msgId -> Parser
        readonly Dictionary<int, MessageParser>            _parsers  = new();
        // msgId -> list of handlers
        readonly Dictionary<int, List<Func<Session,IMessage,Task>>> _handlers = new();

        /// <summary>
        /// 注册一个 T 类型的回调
        /// 只需：_dispatcher.RegisterHandler<LoginReq>(OnLoginReq);
        /// 支持同类型多次调用，内部会累加
        /// </summary>
        public void RegisterHandler<T>(Func<Session, T, Task> handler)
            where T : IMessage<T>
        {
            // 反射拿到 T.Parser
            var prop = typeof(T).GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
            if (prop == null) throw new InvalidOperationException($"类型 {typeof(T)} 不含 Parser 属性");
            var parser = (MessageParser)prop.GetValue(null)!;

            // 反射或配置获取该 T 对应的消息ID
            int msgId = MessageTypeRegistry.GetMsgId<T>();

            // 保存 Parser（首次遇到时）
            if (!_parsers.ContainsKey(msgId))
                _parsers[msgId] = parser;

            // 包装成统一的 IMessage 回调
            Func<Session,IMessage,Task> wrapper = async (ses, msg) =>
                await handler(ses, (T)msg);

            // 累加到 handlers 列表
            if (!_handlers.TryGetValue(msgId, out var list))
            {
                list = new List<Func<Session,IMessage,Task>>();
                _handlers[msgId] = list;
            }
            list.Add(wrapper);
        }

        /// <summary>
        /// 从网络读到完整包后，调用此方法分发
        /// </summary>
        public async Task Dispatch(Session session, byte[] packet)
        {
            int msgId = BitConverter.ToInt32(packet, 4);
            // 取 Parser
            if (!_parsers.TryGetValue(msgId, out var parser)) return;

            // 解析消息
            var msg = parser.ParseFrom(packet, 8, packet.Length - 8);

            // 调用所有注册的回调
            if (_handlers.TryGetValue(msgId, out var list))
            {
                foreach (var h in list)
                    await h(session, msg);
            }
        }
    }
}
