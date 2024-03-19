using MassTransit.Configuration;
using MassTransit.Metadata;
using MassTransit.Middleware;
using MassTransit;

namespace Framework
{
    internal class CustomConsumeConnectorFactory<TConsumer, TMessage> : IMessageConnectorFactory
        where TConsumer : class, IScopedMessageHandler
        where TMessage : class, IMessage
    {
        private readonly ConsumerMessageConnector<TConsumer, TMessage> _consumerConnector;
        private readonly InstanceMessageConnector<TConsumer, TMessage> _instanceConnector;

        public CustomConsumeConnectorFactory()
        {
            var filter = new CustomConsumerMessageFilter<TConsumer, TMessage>();

            _consumerConnector = new ConsumerMessageConnector<TConsumer, TMessage>(filter);
            _instanceConnector = new InstanceMessageConnector<TConsumer, TMessage>(filter);
        }

        IConsumerMessageConnector<T> IMessageConnectorFactory.CreateConsumerConnector<T>()
        {
            return _consumerConnector is not IConsumerMessageConnector<T> result ?
                throw new ArgumentException("The consumer type did not match the connector type")
                : result;
        }

        IInstanceMessageConnector<T> IMessageConnectorFactory.CreateInstanceConnector<T>()
        {
            return _instanceConnector is not IInstanceMessageConnector<T> result
                ? throw new ArgumentException("The consumer type did not match the connector type")
                : result;
        }
    }

    internal class CustomConsumerConvention : IConsumerConvention
    {
        IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<TConsumer>() => new CustomConsumerMessageConvention<TConsumer>();
    }

    internal class CustomConsumerInterfaceType : IMessageInterfaceType
    {
        private readonly Lazy<IMessageConnectorFactory> _consumeConnectorFactory;

        public CustomConsumerInterfaceType(Type messageType, Type consumerType)
        {
            MessageType = messageType;

            _consumeConnectorFactory = new Lazy<IMessageConnectorFactory>(
                () => (IMessageConnectorFactory)Activator.CreateInstance(typeof(CustomConsumeConnectorFactory<,>).MakeGenericType(consumerType, messageType))
            );
        }

        public Type MessageType { get; }

        IConsumerMessageConnector<T> IMessageInterfaceType.GetConsumerConnector<T>()
            => _consumeConnectorFactory.Value.CreateConsumerConnector<T>();

        IInstanceMessageConnector<T> IMessageInterfaceType.GetInstanceConnector<T>()
            => _consumeConnectorFactory.Value.CreateInstanceConnector<T>();
    }

    internal class CustomConsumerMessageConvention<TConsumer> : IConsumerMessageConvention
        where TConsumer : class
    {
        // Example:
        // TConsumer = ScopedMessageHandler<MessageBrokerTestHandler>
        // 1. Extract MessageBrokerTestHandler : IMessageHandler<MessageA>, IMessageHandler<MessageB> from TConsumer
        // 2. Extract MessageA & MessageB from MessageBrokerTestHandler
        public IEnumerable<IMessageInterfaceType> GetMessageTypes()
        {
            // See ScopedMessageHandler, generic parameter is the actual handler.
            var scopedConsumerType = typeof(TConsumer);
            // Actual message consumer that implements IMessageHandler<TMessage>
            var consumerType = scopedConsumerType.IsGenericType && scopedConsumerType.GetGenericTypeDefinition() == typeof(ScopedMessageHandler<>)
                ? scopedConsumerType.GetGenericArguments().First()
                : throw new Exception("Consumer is not wrapped with ScopedMessageHandler.");

            var customConsumerInterfaceTypes = consumerType.GetInterfaces()
                .Where(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
                .Select(messageHandlerType => new CustomConsumerInterfaceType(messageHandlerType.GetGenericArguments().First(), scopedConsumerType))
                .Where(x => !x.MessageType.IsValueType && x.MessageType != typeof(string));

            foreach (var type in customConsumerInterfaceTypes)
                yield return type;
        }
    }

    /// <summary>
    /// Dispatches the ConsumeContext to the consumer method for the specified message type
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type</typeparam>
    /// <typeparam name="TMessage">The message type</typeparam>
    internal class CustomConsumerMessageFilter<TConsumer, TMessage> : IConsumerMessageFilter<TConsumer, TMessage>
        where TConsumer : class, IScopedMessageHandler
        where TMessage : class, IMessage
    {
        void IProbeSite.Probe(ProbeContext context)
        {
            context.CreateScope("consume").Add("method", $"Handle({TypeMetadataCache<TMessage>.ShortName} message)");
        }

        async Task IFilter<ConsumerConsumeContext<TConsumer, TMessage>>.Send(
            ConsumerConsumeContext<TConsumer, TMessage> context,
            IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next
        )
        {
            // Each message handler is internally wrapped with ScopedMessageHandler, first generic argument must be a
            // real IMessageHandler<TMessage>, the one that actually does the work.
            if (!typeof(TConsumer).GetGenericArguments().First().IsAssignableTo(typeof(IMessageHandler<TMessage>)))
            {
                throw new ConsumerMessageException($"Consumer type {TypeMetadataCache<TConsumer>.ShortName} is not a consumer of message type {TypeMetadataCache<TMessage>.ShortName}");
            }

            try
            {
                await context.Consumer.Handle(context.Message, new MessageContext());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}