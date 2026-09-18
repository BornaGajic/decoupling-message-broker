using Framework.Cancellation;
using Framework.Settings;

namespace Framework;

internal class BusConfigurator : IBusConfigurator
{
    internal List<EndpointSettings> _endpointMap = [];
    public IList<EndpointSettings> EndpointMap => _endpointMap;

    /// <inheritdoc/>
    public void ReceiveEndpoint(string endpointName, int concurrency, Action<IBusEndpointConfigurator> endpointConfigurator)
    {
        var handlers = new HashSet<Type>();
        _endpointMap.Add(new EndpointSettings { Name = endpointName, HandlerTypes = handlers, Concurrency = concurrency });

        endpointConfigurator(new BusEndpointConfigurator(handlers));

        // Each registered endpoint gets its own cancellation endpoint.
        // This way all endpoints will be able to cancel their own handlers.
        // Ideally, these queues will be deleted once the bus stops (set AutoDelete and QueueExpiration if possible).
        _endpointMap.Add(new EndpointSettings
        {
            Name = $"{Guid.NewGuid()}-{endpointName}-cancellation",
            HandlerTypes = [typeof(CancelHandlerExecution)],
            Concurrency = 1
        });
    }

    internal class BusEndpointConfigurator : IBusEndpointConfigurator
    {
        private readonly HashSet<Type> _types;

        public BusEndpointConfigurator(HashSet<Type> types)
        {
            _types = types;
        }

        public void AddHandler<T>() where T : IMessageHandler => _types.Add(typeof(T));
    }
}