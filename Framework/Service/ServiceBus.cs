using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Framework
{
    /// <summary>
    /// Some method implementations are omitted to keep things clean.
    /// </summary>
    internal abstract class ServiceBus : IServiceBus
    {
        private readonly BusConfigurator _busConfigurator = new();
        private readonly IServiceProviderIsService _serviceProviderIsService;
        private IBusControl _bus;

        public ServiceBus(IServiceProviderIsService serviceProviderIsService)
        {
            _serviceProviderIsService = serviceProviderIsService;
        }

        protected internal IReadOnlyCollection<EndpointSettings> Endpoints => _busConfigurator.EndpointMap;
        protected abstract Uri HostAdress { get; }

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
                var bus = Setup(CancellationToken.None);
                bus.Start();
                return bus;
            });

            if (_bus is null)
                throw new Exception("Service bus failed to initialize.");
        }

        public virtual void Stop() => _bus.Stop();

        internal void ConfigureEndpoints(IEnumerable<Action<IBusConfigurator>> configurators)
        {
            foreach (var configurator in configurators)
            {
                configurator?.Invoke(_busConfigurator);
            }

            foreach (var setting in _busConfigurator.EndpointMap)
            {
                var invalidType = setting.HandlerTypes.FirstOrDefault(handler =>
                    !handler.IsAssignableTo(typeof(IMessageHandler))
                    || !_serviceProviderIsService.IsService(handler)
                );

                if (invalidType is not null)
                {
                    throw new Exception($"Type '{invalidType.FullName}' is not assignable to {nameof(IMessageHandler)} or is not registered with IServiceCollection.");
                }
            }
        }

        /// <summary>
        /// Setup and create a Bus Control instance
        /// </summary>
        protected abstract IBusControl Setup(CancellationToken token = default);
    }
}