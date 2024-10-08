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

    protected override IBusControl Setup()
    {
        ConsumerConvention.Register<CustomConsumerConvention>();

        return Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.Host(HostAdress);

            foreach (var (endpointName, handlers) in base.Endpoints())
            {
                cfg.ReceiveEndpoint(endpointName, e =>
                {
                    foreach (var consumer in handlers)
                    {
                        e.Consumer(
                            typeof(ScopedMessageHandler<>).MakeGenericType(consumer),
                        consumerType => ActivatorUtilities.CreateInstance(_serviceProvider, consumerType, [_serviceProvider.CreateScope()])
                        );
                    }
                });
            }
        });
    }
}