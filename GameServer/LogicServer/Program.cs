using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Common.Network;
using Common.Utils;
using Data;
using Data.Repositories;
using LogicServer.Modules;

namespace LogicServer
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .ConfigureServices((ctx, services) =>
                {
                    // 工具 & 网络
                    services.AddSingleton<BufferManager>(_ => new BufferManager(1024 * 1024, 8192));
                    services.AddSingleton<INetworkService, NetworkService>();
                    services.AddSingleton<MessageDispatcher>();

                    // 数据访问
                    services.AddSingleton<MongoContext>(_ => new MongoContext("mongodb://localhost:27017"));
                    services.AddSingleton<IUserRepository, UserRepository>();

                    // 业务模块
                    services.AddSingleton<LoginModule>();
                    services.AddHostedService<LogicHostedService>();
                })
                .Build();

            await host.StartAsync();

            // 打印启动成功
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("🚀 LogicServer 已启动，监听端口 {Port}", 6000);

            await host.WaitForShutdownAsync();
        }
    }
}