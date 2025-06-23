using System;
using System.Net;
using System.Threading.Tasks;

namespace Common.Network
{
    public interface INetworkService
    {
        Task StartAsync(IPEndPoint listenEndPoint, Func<Session, byte[], Task> onMessage);
        Task ConnectAsync(string host, int port, Action<Session> onConnected);
    }
}
