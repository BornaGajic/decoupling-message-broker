using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework;

internal class InMemoryServiceBus : ServiceBus
{
    public InMemoryServiceBus(
        IOptions<MessageBrokerSettings> messageBrokerSettings,
        IServiceProvider serviceProvider,
        IServiceProviderIsService serviceProviderIsService
    ) : base(messageBrokerSettings, serviceProvider, serviceProviderIsService)
    {
    }

    protected override IBusControl Setup()
    {
        ConsumerConvention.Register(new CustomConsumerConvention());

        var bus = Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.Host(_baseUri);

            cfg.PrefetchCount = 32;
            cfg.UseConcurrencyLimit(32);

            SetupEndpoints(cfg);
        });

        return bus;
    }
}