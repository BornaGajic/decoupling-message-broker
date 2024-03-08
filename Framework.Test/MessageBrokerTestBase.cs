using Microsoft.Extensions.DependencyInjection;

namespace Framework.Test
{
    public class MessageBrokerTestBase : TestSetup
    {
        public MessageBrokerTestBase()
        {
            var config = SetupConfiguration();

            Container = SetupContainer(builder =>
            {
                builder.AddKeyedSingleton<TaskCompletionSource<MessageA>>(nameof(MessageA));
                builder.AddSingleton<IMessageHandler<MessageA>, MessageBrokerTestHandler>();

                builder.AddKeyedSingleton<TaskCompletionSource<MessageB>>(nameof(MessageB));
                builder.AddSingleton<IMessageHandler<MessageB>, MessageBrokerTestHandler>();

                builder.RegisterMessageBroker(config);
            });

            Bus = Container.GetRequiredService<IServiceBus>();
            Bus.Start();
        }

        public IServiceBus Bus { get; private set; }
        public IServiceProvider Container { get; }
    }
}