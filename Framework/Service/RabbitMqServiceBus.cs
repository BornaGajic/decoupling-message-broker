using Framework.Common;
using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework;

// useful links:
// AutoDelete and endpoint configuration: https://groups.google.com/g/masstransit-discuss/c/AlPB3s2QXfM
// QueueExpiration: https://stackoverflow.com/questions/66760347/consequences-of-setting-queueexpiration-in-masstransit
// Queue name per service type: https://stackoverflow.com/questions/69446842/multiple-consumers-with-the-same-name-in-different-projects-subscribed-to-the-sa
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

    protected override IBusControl Setup(CancellationToken cancellationToken = default)
    {
        ConsumerConvention.Register<CustomConsumerConvention>();

        return Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.UseMessageRetry(rc =>
            {
                rc.Handle<RabbitMqConnectionException>();
                rc.Interval(5, 5000);
            });
            cfg.Host(HostAdress);

            foreach (var setting in Endpoints)
            {
                cfg.ReceiveEndpoint(setting.Name, e =>
                {
                    e.UseMessageRetry(rc =>
                    {
                        rc.Handle<RabbitMqConnectionException>();
                        rc.Interval(5, 5000);
                    });
                    e.UseConcurrencyLimit(setting.Concurrency);
                    e.PrefetchCount = setting.Concurrency;
                    e.PurgeOnStartup = true; // Remove messages on startup
                    e.AutoDelete = true; // Auto delete the queue on bus stop
                    foreach (var consumer in setting.HandlerTypes)
                    {
                        // One consumer can implement multriple IMessageHandler<> interfaces
                        foreach (var messageHandler in consumer.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>)))
                        {
                            var messageType = messageHandler.GetGenericArguments()[0];
                            e.Consumer(
                                typeof(FaultConsumer<>).MakeGenericType(messageType),
                                consumerType => ActivatorUtilities.CreateInstance(_serviceProvider, consumerType)
                            );
                        }

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