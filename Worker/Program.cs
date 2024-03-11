using Framework;
using Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Worker
{
    internal class Program
    {
        public static IServiceProvider SetupContainer(Action<IServiceCollection> callback)
        {
            var services = new ServiceCollection();
            callback?.Invoke(services);
            return services.BuildServiceProvider();
        }

        private static async Task Main(string[] args)
        {
            var container = SetupContainer(svc =>
            {
                svc.RegisterMessageBroker(SetupConfiguration(), cfg =>
                {
                    cfg.ReceiveEndpoint("worker", ep =>
                    {
                        ep.AddHandler<MyMessageHandler>();
                    });
                });
                svc.AddSingleton<MyMessageHandler>();
            });

            var bus = container.GetRequiredService<IServiceBus>();
            await bus.StartAsync();

            Console.WriteLine("Bus started.");

            Console.ReadKey();

            await bus.StopAsync();

            Console.WriteLine("Bus stopped.");
        }

        private static IConfiguration SetupConfiguration() => new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false).Build();
    }
}