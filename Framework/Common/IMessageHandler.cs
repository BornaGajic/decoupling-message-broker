namespace Framework;

public interface IMessageHandler;

public interface IMessageHandler<TMessage> : IMessageHandler
    where TMessage : class, IMessage
{
    Task Handle(TMessage message, IMessageContext context);
}