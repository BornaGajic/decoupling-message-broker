using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Framework;

public class RabbitMqServiceBus : ServiceBus
{
    public RabbitMqServiceBus(
        IOptions<MessageBrokerSettings> messageBrokerSettings,
        IServiceProvider serviceProvider
    ) : base(messageBrokerSettings, serviceProvider)
    {
    }

    protected override IBusControl Setup()
    {
        ConsumerConvention.Register(new CustomConsumerConvention());

        var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(_baseUri);

            cfg.PrefetchCount = 32;
            cfg.UseConcurrencyLimit(32);
            cfg.PurgeOnStartup = true;

            SetupEndpoints(cfg);
        });

        return bus;
    }
}