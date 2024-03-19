using Framework;
using Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DecouplingMessageBroker
{
    internal class Program
    {
        private static async Task Main()
        {
            var container = SetupContainer(svc =>
            {
                svc.RegisterMessageBroker(SetupConfiguration());
            });

            var bus = container.GetRequiredService<IServiceBus>();
            bus.Start();

            Console.WriteLine("Bus started.");
            Console.WriteLine("Press ENTER to send a message to other console.");

            Console.ReadKey();

            var message = new MyMessage
            {
                Id = Guid.NewGuid(),
                Text = "Hello, World!"
            };

            await bus.PublishAsync(message);

            Console.WriteLine($"Message sent: {message}");
            Console.WriteLine("Press ENTER to close the console.");

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
}