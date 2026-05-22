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

                builder.RegisterMessageBrokerEndpoint(cfg =>
                {
                    cfg.ReceiveEndpoint("app-default", 2, ep =>
                    {
                        ep.AddHandler<MessageBrokerTestHandler>();
                    });
                });
                builder.RegisterMessageBroker(config);
            });

            Bus = Container.GetRequiredService<IServiceBus>();
            Bus.Start();
        }

        public static IServiceBus Bus { get; }
        public static IServiceProvider Container { get; }
    }
}