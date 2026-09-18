using Microsoft.Extensions.DependencyInjection;
using Framework.Cancellation;
using Framework.Common;

namespace Framework;

/// <summary>
/// This class wraps <see cref="IMessageHandler{TMessage}"/> where it then creates the instance using <see cref="IServiceProvider"/> that was created via <see cref="IServiceScope"/>
/// </summary>
/// <typeparam name="TConsumer">Should implement <see cref="IMessageHandler{TMessage}"/></typeparam>
internal class ScopedMessageHandler<TConsumer> : IScopedMessageHandler
    where TConsumer : class, IMessageHandler
{
    private bool _disposed;

    public ScopedMessageHandler(IServiceScopeFactory serviceScopeFactory)
    {
        Scope = serviceScopeFactory.CreateAsyncScope();
    }

    public IServiceScope Scope { get; }

    public IServiceProvider ServiceProvider => !_disposed ? Scope.ServiceProvider : throw new ObjectDisposedException(null, "Scope is disposed.");

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async Task Handle<TMessage>(TMessage message, IMessageContext context)
        where TMessage : class, IMessage
    {
        var consumerType = typeof(TConsumer);

        if (!consumerType.IsAssignableTo(typeof(IMessageHandler<TMessage>)))
        {
            throw new MessageHandlerException($"Handler type {typeof(TConsumer).Name} does not handle message type {typeof(TMessage).Name}.");
        }
        if (!ServiceProvider.GetRequiredService<IServiceProviderIsService>().IsService(consumerType))
        {
            throw new MessageHandlerException($"Handler type {typeof(TConsumer).Name} is not registered with IServiceCollection.");
        }

        var handler = ServiceProvider.GetRequiredService(consumerType) as IMessageHandler<TMessage>;
        var cancellationRegistration = await ServiceProvider
            .GetRequiredService<MessageHandlerCancellation>()
            .RegisterCancellationAsync(message.Id, context);

        try
        {
            await handler.Handle(message, context);
        }
        finally
        {
            cancellationRegistration.Unregister();
        }
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