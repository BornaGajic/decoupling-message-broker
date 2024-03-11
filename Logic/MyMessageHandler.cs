using Framework;

namespace Logic
{
    public class MyMessageHandler : IMessageHandler<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageContext context)
        {
            Console.WriteLine($"Message received: {message}");
            return Task.CompletedTask;
        }
    }
}