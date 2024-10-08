using MassTransit.Metadata;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Framework
{
    /// <summary>
    /// This class wraps <see cref="IMessageHandler{TMessage}"/> where it then creates the instance using <see cref="IServiceProvider"/> that was created via <see cref="IServiceScope"/>
    /// </summary>
    /// <typeparam name="TConsumer">Should implement <see cref="IMessageHandler{TMessage}"/></typeparam>
    internal class ScopedMessageHandler<TConsumer> : IScopedMessageHandler, IDisposable
        where TConsumer : class, IMessageHandler
    {
        private bool _disposed;

        public ScopedMessageHandler(IServiceScopeFactory serviceScopeFactory)
        {
            Scope = serviceScopeFactory.CreateScope();
        }

        public IServiceScope Scope { get; }
        public IServiceProvider ServiceProvider => !_disposed ? Scope.ServiceProvider : throw new ObjectDisposedException(null, "Scope is disposed.");

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task Handle<TMessage>(TMessage message, IMessageContext context)
            where TMessage : class, IMessage
        {
            var consumerType = typeof(TConsumer);

            if (!consumerType.IsAssignableTo(typeof(IMessageHandler<TMessage>)))
            {
                throw new ConsumerMessageException($"Consumer type {TypeMetadataCache<TConsumer>.ShortName} is not a consumer of message type {TypeMetadataCache<TMessage>.ShortName}");
            }

            if (!ServiceProvider.GetRequiredService<IServiceProviderIsService>().IsService(consumerType))
            {
                throw new ConsumerMessageException($"Consumer type {TypeMetadataCache<TConsumer>.ShortName} is not registered with IServiceCollection.");
            }

            return (ServiceProvider.GetRequiredService(consumerType) as IMessageHandler<TMessage>).Handle(message, context);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Scope.Dispose();
                }

                _disposed = true;
            }
        }
    }
}