using Framework.Common;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Framework;

internal class InMemoryServiceBus : ServiceBus
{
    private readonly IServiceProvider _serviceProvider;

    public InMemoryServiceBus(
        IServiceProvider serviceProvider,
        IServiceProviderIsService serviceProviderIsService
    ) : base(serviceProviderIsService)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Uri HostAdress => null;

    protected override IBusControl Setup(CancellationToken token = default)
    {
        ConsumerConvention.Register<CustomConsumerConvention>();

        return Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.Host(HostAdress);

            foreach (var setting in Endpoints)
            {
                cfg.ReceiveEndpoint(setting.Name, e =>
                {
                    e.UseConcurrencyLimit(setting.Concurrency);
                    e.PrefetchCount = setting.Concurrency;

                    foreach (var consumer in setting.HandlerTypes)
                    {
                        // One consumer can implement multiple IMessageHandler<> interfaces
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