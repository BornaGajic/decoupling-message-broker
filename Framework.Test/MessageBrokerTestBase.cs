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
                builder.AddSingleton<MessageBrokerTestHandler>();
                builder.AddSingleton<TaskCompletionSource<MessageA>>();
                builder.AddSingleton<TaskCompletionSource<MessageB>>();

                builder.RegisterMessageBroker(config, cfg =>
                {
                    cfg.ReceiveEndpoint("app-default", ep =>
                    {
                        ep.AddHandler<MessageBrokerTestHandler>();
                    });
                });
            });

            Bus = Container.GetRequiredService<IServiceBus>();
            Bus.Start();
        }

        public IServiceBus Bus { get; private set; }
        public IServiceProvider Container { get; }
    }
}