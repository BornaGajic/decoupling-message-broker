namespace Framework;

internal interface IScopedMessageHandler : IDisposable
{
    IServiceProvider ServiceProvider { get; }

    Task Handle<TMessage>(TMessage message, IMessageContext context)
        where TMessage : class, IMessage;
}