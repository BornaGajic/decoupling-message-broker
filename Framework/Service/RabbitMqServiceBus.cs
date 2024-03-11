using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework;

internal class RabbitMqServiceBus : ServiceBus
{
    public RabbitMqServiceBus(
        IOptions<MessageBrokerSettings> messageBrokerSettings,
        IServiceProvider serviceProvider,
        IServiceProviderIsService serviceProviderIsService
    ) : base(messageBrokerSettings, serviceProvider, serviceProviderIsService)
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