using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Framework;

internal class InMemoryServiceBus : ServiceBus
{
    public InMemoryServiceBus(
        IOptions<MessageBrokerSettings> messageBrokerSettings,
        IServiceProvider serviceProvider
    ) : base(messageBrokerSettings, serviceProvider)
    {
    }

    protected override IBusControl Setup()
    {
        ConsumerConvention.Register<CustomConsumerConvention>();

        var bus = Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.Host(_baseUri);

            SetupEndpoints(cfg);
        });

        return bus;
    }
}