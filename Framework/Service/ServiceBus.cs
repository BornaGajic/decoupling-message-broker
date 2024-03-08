using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace Framework
{
    public abstract class ServiceBus : IServiceBus
    {
        protected readonly Uri _baseUri;
        private readonly IServiceProvider _serviceProvider;
        private IBusControl _bus;

        public ServiceBus(IOptions<MessageBrokerSettings> messageBrokerSettings, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _baseUri = messageBrokerSettings.Value.Transport == MessageBrokerTransport.InMemory ? null : new Uri(messageBrokerSettings.Value.ConnectionString);
        }

        public virtual async Task PublishAsync<T>(T message)
            where T : class, IMessage
        {
            await _bus.Publish(message);
        }

        public virtual Task SendAsync<T>(string destination, T message)
            where T : class, IMessage
        {
            return SendAsync(new Uri(_baseUri, destination), message);
        }

        public virtual async Task SendAsync<T>(Uri address, T message)
            where T : class, IMessage
        {
            var sendEndpoint = await _bus.GetSendEndpoint(address);
            await sendEndpoint.Send(message);
        }

        public virtual void Start()
        {
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

        public virtual async Task StartAsync()
        {
            var retryRabbitMqPolicy = Policy
                .Handle<RabbitMqConnectionException>()
                .WaitAndRetryAsync(
                    3,
                    attempt => TimeSpan.FromSeconds(10)
                );

            _bus = await retryRabbitMqPolicy.ExecuteAsync(async () =>
            {
                var bus = Setup();
                var busHandle = await bus.StartAsync();
                await busHandle.Ready;
                return bus;
            });

            if (_bus is null)
                throw new Exception("Service bus failed to initialize.");
        }

        public virtual void Stop() => _bus.Stop();

        public Task StopAsync() => _bus.StopAsync();

        /// <summary>
        /// Setup and create a Bus Control instance
        /// </summary>
        protected abstract IBusControl Setup();

        /// <summary>
        /// Register all <see cref="IMessageHandler{TMessage}"/> found in all assemblies.
        /// </summary>
        protected virtual void SetupEndpoints(IBusFactoryConfigurator cfg)
        {
            var allMessageBrokerTypes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(t => t.GetTypes())
                .Where(t =>
                    t.IsAssignableTo(typeof(IMessage))
                    || t.IsAssignableTo(typeof(IMessageHandler))
                )
                .ToList();

            var messageTypes = allMessageBrokerTypes
                .Where(t => !t.IsInterface && t.IsAssignableTo(typeof(IMessage)))
                .Select(t => typeof(IMessageHandler<>).MakeGenericType(t))
                .ToList();

            var consumerTypes = allMessageBrokerTypes
                .Where(t => messageTypes.Any(messageType => t.IsAssignableTo(messageType)))
                .ToList();

            SetupEndpoints(cfg, new Dictionary<string, IEnumerable<Type>>()
            {
                ["tag-app-default"] = consumerTypes
            });
        }

        protected virtual void SetupEndpoints(IBusFactoryConfigurator cfg, IDictionary<string, IEnumerable<Type>> endpointHandlers)
        {
            if (endpointHandlers.Count > 0)
            {
                foreach (var (endpointName, handlers) in endpointHandlers)
                {
                    if (handlers.Any(handler => !handler.IsAssignableTo(typeof(IMessageHandler))))
                    {
                        throw new Exception($"Type is not assignable to {nameof(IMessageHandler)}");
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
            };
        }
    }
}