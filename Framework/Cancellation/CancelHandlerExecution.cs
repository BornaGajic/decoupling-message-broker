namespace Framework.Cancellation;

internal class CancelHandlerExecution(MessageHandlerCancellation cancellation) : IMessageHandler<MessageToCancel>
{
    public Task Handle(MessageToCancel message, IMessageContext context)
    {
        cancellation.Cancel(message.MessageIdToCancel);
        return Task.CompletedTask;
    }
}