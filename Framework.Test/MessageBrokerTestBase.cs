namespace Framework.Test
{
    public class MessageBrokerTestBase : TestSetup
    {
        static MessageBrokerTestBase()
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

        public static IServiceBus Bus { get; }
        public static IServiceProvider Container { get; }
    }
}