using Microsoft.Extensions.DependencyInjection;

namespace Framework.Test
{
    public class MessageBrokerTestHandler(
        [FromKeyedServices(nameof(MessageA))] TaskCompletionSource<MessageA> ReceivedA,
        [FromKeyedServices(nameof(MessageB))] TaskCompletionSource<MessageB> ReceivedB
    ) : IMessageHandler<MessageA>, IMessageHandler<MessageB>
    {
        Task IMessageHandler<MessageA>.Handle(MessageA message, IMessageContext context)
        {
            ReceivedA.TrySetResult(message);
            return Task.CompletedTask;
        }

        Task IMessageHandler<MessageB>.Handle(MessageB message, IMessageContext context)
        {
            ReceivedB.TrySetResult(message);
            return Task.CompletedTask;
        }
    }
}