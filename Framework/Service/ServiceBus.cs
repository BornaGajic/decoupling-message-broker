using Framework.Cancellation;
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

        public virtual Task CancelAsync(Guid messageId, CancellationToken cancellationToken)
            => PublishAsync(new MessageToCancel { MessageIdToCancel = messageId }, cancellationToken);

        public virtual async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage
        {
            await _bus.Publish(message, cancellationToken);
        }

        public virtual Task SendAsync<T>(string destination, T message, CancellationToken cancellationToken = default)
            where T : class, IMessage
        {
            return SendAsync(new Uri(HostAdress, destination), message, cancellationToken);
        }

        public virtual async Task SendAsync<T>(Uri address, T message, CancellationToken cancellationToken = default)
            where T : class, IMessage
        {
            var sendEndpoint = await _bus.GetSendEndpoint(address);
            await sendEndpoint.Send(message, cancellationToken);
        }

        public virtual void Start()
        {
            if (_bus is not null)
                return;

            var retryRabbitMqPolicy = Policy
                .Handle<RabbitMqConnectionException>()
                .WaitAndRetry(
                    3,
                    attempt => TimeSpan.FromSeconds(10),
                    (ex, next, retry, ctx) => Console.WriteLine($"[#{retry}] Could not connect, retrying...")
                );

            _bus = retryRabbitMqPolicy.Execute(() =>
            {
                var bus = Setup(CancellationToken.None);

                try
                {
                    bus.Start(TimeSpan.FromSeconds(15)); // If connection string is OK bump this number. Though localhost RabbitMQ should connect in seconds.
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Failed to connect - timeout. Check your RabbitMQ connection string (FQDN / host, port #, creds...)");
                    return null;
                }

                return bus;
            }) ?? throw new Exception("Service bus failed to initialize.");
        }

        public virtual void Stop() => _bus.Stop();

        internal void ConfigureEndpoints(IEnumerable<Action<IBusConfigurator>> configurators)
        {
            foreach (var configurator in configurators)
            {
                configurator?.Invoke(_busConfigurator);
            }

            var duplicateNames = _busConfigurator.EndpointMap
                .GroupBy(setting => setting.Name)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                throw new Exception($"Duplicate message broker endpoint name(s): {string.Join(", ", duplicateNames)}. Each ReceiveEndpoint name must be unique within a process.");
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
        protected abstract IBusControl Setup(CancellationToken cancellationToken = default);
    }
}