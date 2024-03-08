namespace Framework;

internal interface IScopedMessageHandler
{
    IServiceProvider ServiceProvider { get; }

    Task Handle<TMessage>(TMessage message, IMessageContext context)
        where TMessage : class, IMessage;
}