using System.Collections.Concurrent;
using System.Reflection;
using EasyNetQ;
using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Topology;
using Framework.Settings;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using RabbitMQ.Client.Exceptions;

using Easy = EasyNetQ;

namespace Framework.EasyNetQ;

internal class EasyNetQServiceBus : ServiceBus
{
    private static readonly IAsyncPolicy _handlerRetryPolicy = Policy
        .Handle<RabbitMQClientException>()
        .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(5));

    private readonly IAdvancedBus _bus;
    private readonly List<IAsyncDisposable> _consumers = [];
    private readonly ConcurrentDictionary<Type, Task<Exchange>> _exchanges = new();
    private readonly IServiceProvider _serviceProvider;
    private bool _started;

    public EasyNetQServiceBus(
        IBus bus,
        IServiceProvider serviceProvider,
        IServiceProviderIsService serviceProviderIsService
    ) : base(serviceProviderIsService)
    {
        _bus = bus.Advanced;
        _serviceProvider = serviceProvider;
    }

    public override Task PublishAsync<T>(T message, CancellationToken token)
        => PublishAsync(message, typeof(T), token);

    public override Task SendAsync<T>(string destination, T message, CancellationToken token)
        => SendAsync(destination, message, typeof(T), token);

    public override Task SendAsync<T>(Uri address, T message, CancellationToken token)
        => SendAsync(QueueName(address), message, token);

    public override void Start(int concurrencyLimit)
        => StartAsync(retryCount: 3, retryDelay: TimeSpan.FromSeconds(10), CancellationToken.None).GetAwaiter().GetResult();

    public override Task StartAsync(int concurrencyLimit, CancellationToken token = default)
        => StartAsync(retryCount: 5, retryDelay: TimeSpan.FromSeconds(60), token);

    public override void Stop() => StopAsync(CancellationToken.None).GetAwaiter().GetResult();

    public override async Task StopAsync(CancellationToken token)
    {
        foreach (var consumer in _consumers)
        {
            await consumer.DisposeAsync();
        }

        _consumers.Clear();
        _exchanges.Clear();
        _started = false;
    }

    internal async Task PublishAsync(object message, Type messageType, CancellationToken token)
    {
        var exchange = await DeclareExchangeAsync(messageType, token);
        await _bus.PublishAsync(exchange.Name, string.Empty, mandatory: null, publisherConfirms: null, Envelope(message, messageType), token);
    }

    internal Task SendAsync(string destination, object message, Type messageType, CancellationToken token)
        => _bus.PublishAsync(Exchange.DefaultName, destination, mandatory: null, publisherConfirms: null, Envelope(message, messageType), token);

    private static Easy.IMessage Envelope(object message, Type messageType)
        => (Easy.IMessage)Activator.CreateInstance(typeof(Message<>).MakeGenericType(messageType), message);

    private static IEnumerable<Type> ParentMessageTypes(Type messageType)
    {
        var baseType = messageType.BaseType is { } type && type != typeof(object) && type.IsAssignableTo(typeof(IMessage))
            ? type
            : null;

        var interfaces = messageType.GetInterfaces()
            .Where(i => i != typeof(IMessage) && i.IsAssignableTo(typeof(IMessage)))
            .Except(baseType?.GetInterfaces() ?? []);

        return baseType is null ? interfaces : interfaces.Prepend(baseType);
    }

    private static string QueueName(Uri address)
    {
        var queueName = Uri.UnescapeDataString(address.Segments[^1].TrimEnd('/'));

        return queueName.Length > 0
            ? queueName
            : throw new ArgumentException($"'{address}' does not address a queue.", nameof(address));
    }

    private async Task<Exchange> DeclareExchangeAsync(Type messageType, CancellationToken token)
    {
        var declaration = _exchanges.GetOrAdd(messageType, type => DeclareExchangeHierarchyAsync(type, token));

        try
        {
            return await declaration;
        }
        catch
        {
            _exchanges.TryRemove(new KeyValuePair<Type, Task<Exchange>>(messageType, declaration));
            throw;
        }
    }

    /// <summary>
    /// Mirrors MassTransit's topology: a message is published to the exchange of its concrete type, which fans out into the
    /// exchanges of its base type and interfaces, so a handler bound to a base type receives derived messages.
    /// </summary>
    private async Task<Exchange> DeclareExchangeHierarchyAsync(Type messageType, CancellationToken token)
    {
        var exchange = await _bus.ExchangeDeclareAsync(messageType.FullName, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: token);

        foreach (var parentType in ParentMessageTypes(messageType))
        {
            var parentExchange = await DeclareExchangeAsync(parentType, token);
            await _bus.ExchangeBindAsync(parentExchange.Name, exchange.Name, string.Empty, cancellationToken: token);
        }

        return exchange;
    }

    private void RegisterHandler<TMessage>(IHandlerRegistration handlers, IEnumerable<Type> handlerTypes)
        where TMessage : class, IMessage
    {
        var scopedHandlerTypes = handlerTypes
            .Select(handlerType => typeof(ScopedMessageHandler<>).MakeGenericType(handlerType))
            .ToList();

        handlers.Add<TMessage>(async (envelope, receivedInfo, cancellationToken) =>
        {
            var executions = Task.WhenAll(scopedHandlerTypes.Select(scopedHandlerType =>
            {
                return _handlerRetryPolicy.ExecuteAsync(async ct =>
                {
                    using var scopedHandler = (IScopedMessageHandler)ActivatorUtilities.CreateInstance(_serviceProvider, scopedHandlerType);
                    await scopedHandler.Handle(envelope.Body, new EasyNetQMessageContext(this));
                }, cancellationToken);
            }));

            try
            {
                await executions;
            }
            catch when (executions.Exception?.InnerExceptions.Count > 1)
            {
                // Awaiting WhenAll surfaces only the first failure; the error queue should record every handler that failed.
                throw executions.Exception;
            }
        });
    }

    private async Task StartAsync(int retryCount, TimeSpan retryDelay, CancellationToken token)
    {
        if (_started)
            return;

        try
        {
            await Policy
                .Handle<BrokerUnreachableException>()
                .WaitAndRetryAsync(retryCount, attempt => retryDelay)
                .ExecuteAsync(async ct =>
                {
                    await _bus.EnsureConnectedAsync(PersistentConnectionType.Producer, ct);
                    await _bus.EnsureConnectedAsync(PersistentConnectionType.Consumer, ct);
                }, token);

            var registerHandlerMethod = GetType().GetMethod(nameof(RegisterHandler), BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var endpoint in Endpoints)
            {
                await StartEndpointAsync(registerHandlerMethod, endpoint, token);
            }

            _started = true;
        }
        catch
        {
            await StopAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task StartEndpointAsync(MethodInfo registerHandlerMethod, EndpointSettings endpoint, CancellationToken token)
    {
        var queue = await _bus.QueueDeclareAsync(endpoint.Name, durable: true, exclusive: false, autoDelete: true, cancellationToken: token);
        await _bus.QueuePurgeAsync(queue.Name, token);

        // One handler can implement multiple IMessageHandler<> interfaces. Create a dictionary of Message Type : [Message Handlers]
        // e.g.
        // A1 : IMessageHandler<A_Message>
        // A2 : IMessageHandler<A_Message>
        // ----> A_Message : [A1, A2]
        var handlersByMessageType = endpoint.HandlerTypes
            .SelectMany(handlerType => handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
                .Select(i => (MessageType: i.GetGenericArguments()[0], HandlerType: handlerType))
            )
            .ToLookup(pair => pair.MessageType, pair => pair.HandlerType);

        foreach (var messageHandlers in handlersByMessageType)
        {
            var exchange = await DeclareExchangeAsync(messageHandlers.Key, token);
            await _bus.BindAsync(exchange, queue, string.Empty, token);
        }

        var consumer = await _bus.ConsumeAsync(
            queue,
            handlers =>
            {
                foreach (var messageHandlers in handlersByMessageType)
                {
                    registerHandlerMethod
                        .MakeGenericMethod(messageHandlers.Key)
                        .Invoke(this, [handlers, messageHandlers]);
                }
            },
            configuration => configuration.WithPrefetchCount((ushort)endpoint.Concurrency)
        );

        _consumers.Add(consumer);
    }
}