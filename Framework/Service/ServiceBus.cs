using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace Framework
{
    /// <summary>
    /// Some method implementations are omitted to keep things clean.
    /// </summary>
    internal abstract class ServiceBus : IServiceBus
    {
        protected readonly Uri _baseUri;
        private readonly BusConfigurator _busConfigurator = new();
        private readonly IServiceProvider _serviceProvider;
        private IBusControl _bus;

        public ServiceBus(
            IOptions<MessageBrokerSettings> messageBrokerSettings,
            IServiceProvider serviceProvider
        )
        {
            _serviceProvider = serviceProvider;
            _baseUri = messageBrokerSettings.Value.Transport == MessageBrokerTransport.InMemory ? null : new Uri(messageBrokerSettings.Value.ConnectionString);
        }

        public virtual async Task PublishAsync<T>(T message)
            where T : class, IMessage
        {
            await _bus.Publish(message);
        }

        public virtual void Start()
        {
            if (_bus is not null)
                return;

            var retryRabbitMqPolicy = Policy
                .Handle<RabbitMqConnectionException>()
                .WaitAndRetry(
                    3,
                    attempt => TimeSpan.FromSeconds(10)
                );

            _bus = retryRabbitMqPolicy.Execute(() =>
            {
                var bus = Setup();
                bus.Start();
                return bus;
            });

            if (_bus is null)
                throw new Exception("Service bus failed to initialize.");
        }

        public virtual void Stop() => _bus.Stop();

        internal void ConfigureEndpoints(Action<IBusConfigurator> configurator) => configurator?.Invoke(_busConfigurator);

        /// <summary>
        /// Setup and create a Bus Control instance
        /// </summary>
        protected abstract IBusControl Setup();

        /// <summary>
        /// Register bus endpoints
        /// </summary>
        protected virtual void SetupEndpoints(IBusFactoryConfigurator cfg)
        {
            var serviceProviderIsService = _serviceProvider.GetRequiredService<IServiceProviderIsService>();

            foreach (var (endpointName, handlers) in _busConfigurator.EndpointMap)
            {
                var invalidType = handlers.FirstOrDefault(handler =>
                    !handler.IsAssignableTo(typeof(IMessageHandler))
                    || !serviceProviderIsService.IsService(handler)
                );

                if (invalidType is not null)
                {
                    throw new Exception($"Type '{invalidType.FullName}' is not assignable to {nameof(IMessageHandler)} or is not registered with IServiceCollection.");
                }

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
        }
    }
}