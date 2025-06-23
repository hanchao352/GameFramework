using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Common.Network;
using Google.Protobuf;
using Game;  // 你的 proto 生成类型命名空间

namespace GatewayServer
{
    public class GatewayConfig
    {
        public int Port { get; set; } = 5000;
    }

    public class GatewayService : IHostedService
    {
        private readonly INetworkService            _network;
        private readonly MessageDispatcher          _dispatcher;
        private readonly GatewayConfig              _config;
        private readonly ILogger<GatewayService>    _logger;

        public GatewayService(
            INetworkService network,
            MessageDispatcher dispatcher,
            IOptions<GatewayConfig> cfg,
            ILogger<GatewayService> logger)
        {
            _network    = network;
            _dispatcher = dispatcher;
            _config     = cfg.Value;
            _logger     = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚀 GatewayService starting on port {Port}...", _config.Port);

            // 1. 注册类型 ↔ ID （只做一次）
            MessageTypeRegistry.Register<LoginReq>(1001);

            // 2. 只传回调，不要再带 msgId 或 Parser 了
            _dispatcher.RegisterHandler<LoginReq>(async (session, req) =>
            {
                _logger.LogInformation("📨 Received LoginReq: account={Account}", req.Account);
                await ForwardToLogic(session, req.ToByteArray(), 1001);
            });

            // 启动网络监听
            _logger.LogInformation("🔌 GatewayService listening on {Endpoint}", $"0.0.0.0:{_config.Port}");
            await _network.StartAsync(
                new IPEndPoint(IPAddress.Any, _config.Port),
                (session, packet) =>
                {
                    _logger.LogDebug("[Gateway] Received packet length={Len}", packet.Length);
                    return _dispatcher.Dispatch(session, packet);
                });

            _logger.LogInformation("✅ GatewayService is up and running.");
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🏁 GatewayService stopping...");
            return Task.CompletedTask;
        }

        private Task ForwardToLogic(Session session, byte[] body, int msgId)
        {
            _logger.LogDebug("⏩ Forwarding msgId={MsgId} to logic server", msgId);
            // TODO: 根据路由策略选择逻辑服并转发
            return Task.CompletedTask;
        }
    }
}
