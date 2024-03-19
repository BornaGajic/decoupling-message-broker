using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Framework;

internal class RabbitMqServiceBus : ServiceBus
{
    public RabbitMqServiceBus(
        IOptions<MessageBrokerSettings> messageBrokerSettings,
        IServiceProvider serviceProvider
    ) : base(messageBrokerSettings, serviceProvider)
    {
    }

    protected override IBusControl Setup()
    {
        ConsumerConvention.Register<CustomConsumerConvention>();

        var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(_baseUri);
            cfg.PurgeOnStartup = true;

            SetupEndpoints(cfg);
        });

        return bus;
    }
}