using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Common.Network;
using CenterServer.Services;

namespace CenterServer
{
    public class CenterHostedService : IHostedService
    {
        private readonly INetworkService    _network;
        private readonly MessageDispatcher  _dispatcher;
        private readonly RoutingService     _routing;
        private readonly MergeService       _merge;
        private readonly ILogger<CenterHostedService> _logger;

        public CenterHostedService(
            INetworkService network,
            MessageDispatcher dispatcher,
            RoutingService routing,
            MergeService merge,
            ILogger<CenterHostedService> logger)
        {
            _network    = network;
            _dispatcher = dispatcher;
            _routing    = routing;
            _merge      = merge;
            _logger     = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CenterService 开始初始化…");

            // TODO: 在这里注册路由、合服等回调
            _logger.LogInformation("CenterService 初始化完成。");

            // 开始监听
            const int port = 7000;
            _logger.LogInformation("CenterService 开始监听端口 {Port}", port);
            await _network.StartAsync(
                new IPEndPoint(IPAddress.Any, port),
                (session, packet) =>
                {
                    _logger.LogDebug("[Center] 收到数据包，长度 {Len}", packet.Length);
                    return _dispatcher.Dispatch(session, packet);
                });
            _logger.LogInformation("CenterService 已开始监听。");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CenterService 正在停止…");
            return Task.CompletedTask;
        }
    }
}