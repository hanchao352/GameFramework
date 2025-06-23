using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Common.Network;
using Common.Utils;
using Data;
using Data.Repositories;

namespace GatewayServer
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // 构建 Host，并加入 ConsoleLogger
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<BufferManager>(_ => new BufferManager(1024 * 1024, 8192));

                    // 接口注入
                    services.AddSingleton<INetworkService, NetworkService>();
                    services.AddSingleton<MessageDispatcher>();

                    services.AddSingleton<MongoContext>(_ => new MongoContext("mongodb://localhost:27017"));
                    services.AddSingleton<IUserRepository, UserRepository>();

                    services.Configure<GatewayConfig>(ctx.Configuration.GetSection("Gateway"));
                    services.AddHostedService<GatewayService>();
                })
                .Build();

            // 启动 Host
            await host.StartAsync();

            // 取得 Logger 和 Config，打印启动成功
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var cfg    = host.Services.GetRequiredService<IOptions<GatewayConfig>>().Value;
            logger.LogInformation("🚀 GatewayServer 已启动，监听端口 {Port}", cfg.Port);

            // 阻塞直到退出
            await host.WaitForShutdownAsync();
        }
    }
}