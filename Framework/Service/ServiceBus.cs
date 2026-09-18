using Framework.Cancellation;
using Framework.Settings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Framework;

/// <summary>
/// Some method implementations are omitted to keep things clean.
/// </summary>
internal abstract class ServiceBus : IServiceBus
{
    private readonly BusConfigurator _busConfigurator = new();
    private readonly IServiceProviderIsService _serviceProviderIsService;

    public ServiceBus(IServiceProviderIsService serviceProviderIsService)
    {
        _serviceProviderIsService = serviceProviderIsService;
    }

    /// <summary>
    /// Gets the endpoint - handler type+settings.
    /// </summary>
    protected internal IList<EndpointSettings> Endpoints => _busConfigurator.EndpointMap;

    public virtual Task CancelAsync(Guid messageId, CancellationToken token)
        => PublishAsync(new MessageToCancel { MessageIdToCancel = messageId }, token);

    public abstract Task PublishAsync<T>(T message, CancellationToken token)
        where T : class, IMessage;

    public abstract Task SendAsync<T>(string destination, T message, CancellationToken token)
        where T : class, IMessage;

    public abstract Task SendAsync<T>(Uri address, T message, CancellationToken token)
        where T : class, IMessage;

    public virtual void Start() => Start(Environment.ProcessorCount);

    public abstract void Start(int concurrencyLimit);

    public virtual Task StartAsync(CancellationToken token = default) => StartAsync(Environment.ProcessorCount, token);

    public abstract Task StartAsync(int concurrencyLimit, CancellationToken token = default);

    public abstract void Stop();

    public abstract Task StopAsync(CancellationToken token);

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

            var markerHandler = setting.HandlerTypes.FirstOrDefault(handler => handler.IsAssignableTo(typeof(IMessageHandler<IMessage>)));

            if (markerHandler is not null)
            {
                throw new Exception($"Type '{markerHandler.FullName}' handles {nameof(IMessage)} itself. Handlers must target a concrete message type.");
            }
        }
    }
}