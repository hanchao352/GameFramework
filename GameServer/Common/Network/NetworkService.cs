using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Common.Utils;

namespace Common.Network
{
    public class NetworkService : INetworkService
    {
        private readonly BufferManager _bufferManager;

        public NetworkService(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
        }

        public async Task StartAsync(IPEndPoint ep, Func<Session, byte[], Task> onMessage)
        {
            var listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(ep);
            listener.Listen(1024);
            Console.WriteLine($"Listening on {ep}…");
            while (true)
            {
                var client = await listener.AcceptAsync();
                var session = new Session(client);
                Console.WriteLine($"[NetworkService] 新客户端连接：{client.RemoteEndPoint}");
                StartReceive(session, onMessage);
            }
        }

        private void StartReceive(Session session, Func<Session, byte[], Task> onMessage)
        {
            var args = new SocketAsyncEventArgs();
            _bufferManager.SetBuffer(args);
            args.AcceptSocket = session.Socket;
            args.Completed += (s, e) => ProcessReceive(e, session, onMessage);
            session.Socket.ReceiveAsync(args);
        }

        private async void ProcessReceive(SocketAsyncEventArgs e, Session session, Func<Session, byte[], Task> onMessage)
        {
            if (e.BytesTransferred <= 0) return;
            session.ReceiveOffset += e.BytesTransferred;
            var buffer = session.ReceiveBuffer;
            int offset = 0;
            while (session.ReceiveOffset - offset >= 8)
            {
                int pkgLen = BitConverter.ToInt32(buffer, offset);
                if (session.ReceiveOffset - offset < pkgLen) break;
                int msgId = BitConverter.ToInt32(buffer, offset + 4);
                var body = new byte[pkgLen - 8];
                Array.Copy(buffer, offset + 8, body, 0, body.Length);
                await onMessage(session, CreatePacket(msgId, body));
                offset += pkgLen;
            }
            Array.Copy(buffer, offset, buffer, 0, session.ReceiveOffset - offset);
            session.ReceiveOffset -= offset;
            session.Socket.ReceiveAsync(e);
        }

        public byte[] CreatePacket(int msgId, byte[] body)
        {
            byte[] pkg = PacketHelper.CreatePacket(msgId, body);
            return pkg;
        }

        public Task ConnectAsync(string host, int port, Action<Session> onConnected)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(host, port, ar => {
                socket.EndConnect(ar);
                var session = new Session(socket);
                onConnected(session);
                StartReceive(session, async (_,__)=>{});
            }, null);
            return Task.CompletedTask;
        }
    }
}
