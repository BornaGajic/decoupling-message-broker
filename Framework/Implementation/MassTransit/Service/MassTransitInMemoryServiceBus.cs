using Framework.Common;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.MassTransit;

internal class MassTransitInMemoryServiceBus : MassTransitServiceBus
{
    private readonly IServiceProvider _serviceProvider;

    public MassTransitInMemoryServiceBus(
        IServiceProvider serviceProvider,
        IServiceProviderIsService serviceProviderIsService
    ) : base(serviceProviderIsService)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Uri HostAdress => null;

    protected override IBusControl Setup(int concurrencyLimit = 1, CancellationToken token = default)
    {
        ConsumerConvention.Register<CustomConsumerConvention>();

        return Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.UseConcurrencyLimit(concurrencyLimit);
            cfg.Host(HostAdress);
            cfg.Publish<IMessage>(topology => topology.Exclude = true);

            foreach (var setting in Endpoints)
            {
                cfg.ReceiveEndpoint(setting.Name, e =>
                {
                    e.UseConcurrencyLimit(setting.Concurrency);
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