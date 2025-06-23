using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Common.Network;
using Game;
using LogicServer.Modules;

namespace LogicServer
{
    public class LogicHostedService : IHostedService
    {
        private readonly INetworkService    _network;
        private readonly MessageDispatcher  _dispatcher;
        private readonly LoginModule        _login;
        private readonly ILogger<LogicHostedService> _logger;

        public LogicHostedService(
            INetworkService network,
            MessageDispatcher dispatcher,
            LoginModule login,
            ILogger<LogicHostedService> logger)
        {
            _network    = network;
            _dispatcher = dispatcher;
            _login      = login;
            _logger     = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("LogicService 开始初始化…");

            // ─── 一定要先注册所有消息类型 ↔ ID ───
            MessageTypeRegistry.Register<LoginReq>(1001);

            // 然后再初始化模块（它会调用 RegisterHandler<LoginReq>）
            _login.Init();
            _logger.LogInformation("LoginModule 初始化完成。");

            // 启动监听……
            const int port = 6000;
            _logger.LogInformation("LogicService 开始监听端口 {Port}", port);
            await _network.StartAsync(
                new IPEndPoint(IPAddress.Any, port),
                (session, packet) =>
                {
                    _logger.LogDebug("[Logic] 收到数据包，长度 {Len}", packet.Length);
                    return _dispatcher.Dispatch(session, packet);
                });
            _logger.LogInformation("LogicService 已开始监听。");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("LogicService 正在停止…");
            return Task.CompletedTask;
        }
    }
}