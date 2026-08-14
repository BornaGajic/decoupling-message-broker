using Framework;
using Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Worker;

internal class Program
{
    private static void Main()
    {
        Console.Title = "Worker";

        Console.WriteLine("Bus is starting...");

        var container = SetupContainer(svc =>
        {
            svc.RegisterMessageBrokerEndpoint(cfg =>
            {
                cfg.ReceiveEndpoint("worker", 2, ep =>
                {
                    ep.AddHandler<MyMessageHandler>();
                });
            });

            svc.RegisterMessageBroker(SetupConfiguration());

            svc.AddSingleton<MyMessageHandler>();
        });

        var bus = container.GetRequiredService<IServiceBus>();
        bus.Start();

        Console.WriteLine("Bus started.");

        Console.ReadKey();

        bus.Stop();

        Console.WriteLine("Bus stopped.");
    }

    private static IConfiguration SetupConfiguration() => new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false).Build();

    private static IServiceProvider SetupContainer(Action<IServiceCollection> callback)
    {
        var services = new ServiceCollection();
        callback?.Invoke(services);
        return services.BuildServiceProvider();
    }
}