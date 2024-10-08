using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework;

internal class RabbitMqServiceBus : ServiceBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<MessageBrokerSettings> _settings;

    public RabbitMqServiceBus(
        IOptions<MessageBrokerSettings> messageBrokerSettings,
        IServiceProvider serviceProvider,
        IServiceProviderIsService serviceProviderIsService
    ) : base(serviceProviderIsService)
    {
        _settings = messageBrokerSettings;
        _serviceProvider = serviceProvider;
    }

    protected override Uri HostAdress => new(_settings.Value.ConnectionString);

    protected override IBusControl Setup()
    {
        ConsumerConvention.Register<CustomConsumerConvention>();

        return Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(HostAdress);

            foreach (var (endpointName, handlers) in base.Endpoints())
            {
                cfg.ReceiveEndpoint(endpointName, e =>
                {
                    e.DiscardFaultedMessages();
                    e.DiscardSkippedMessages();
                    e.UseConcurrencyLimit(32);
                    e.PrefetchCount = 32;
                    e.PurgeOnStartup = true; // Remove messages on startup
                    e.AutoDelete = true; // Auto delete the queue on bus stop

                    foreach (var consumer in handlers)
                    {
                        e.Consumer(
                        typeof(ScopedMessageHandler<>).MakeGenericType(consumer),
                            consumerType => ActivatorUtilities.CreateInstance(_serviceProvider, consumerType)
                        );
                    }
                });
            }
        });
    }
}