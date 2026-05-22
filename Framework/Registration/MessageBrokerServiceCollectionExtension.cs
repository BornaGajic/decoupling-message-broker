using Framework.Cancellation;
using Framework.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Framework;

public static class MessageBrokerServiceCollectionExtension
{
    /// <summary>
    /// 1. Registers <see cref="IServiceBus"/> as <see cref="RabbitMqServiceBus"/> or <see cref="InMemoryServiceBus"/> depending on <see cref="MessageBrokerSettings.Transport"/>.<br/>
    /// 2. Adds <see cref="MessageBrokerSettings"/> to <see cref="IOptions{TOptions}"/>
    /// </summary>
    public static IServiceCollection RegisterMessageBroker(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterMessageBrokerOptions(configuration);
        services.RegisterMessageBrokerServices();
        return services;
    }

    public static IServiceCollection RegisterMessageBrokerEndpoint(this IServiceCollection services, Action<IBusConfigurator> busConfig)
    {
        services.AddTransient(x => busConfig);
        return services;
    }

    private static OptionsBuilder<MessageBrokerSettings> RegisterMessageBrokerOptions(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddOptions<MessageBrokerSettings>().Bind(configuration.GetRequiredSection(MessageBrokerSettings.ConfigurationKey));
    }

    private static IServiceCollection RegisterMessageBrokerServices(this IServiceCollection services)
    {
        services.TryAddSingleton<RabbitMqServiceBus>();
        services.TryAddSingleton<InMemoryServiceBus>();
        services.TryAddSingleton<CancelHandlerExecution>();
        services.TryAddSingleton<MessageHandlerCancellation>();

        services.TryAddSingleton<IServiceBus>(svc =>
        {
            var settings = svc.GetRequiredService<IOptions<MessageBrokerSettings>>();
            var actions = svc.GetServices<Action<IBusConfigurator>>();

            ServiceBus bus = settings.Value.Transport switch
            {
                MessageBrokerTransport.RabbitMq => svc.GetRequiredService<RabbitMqServiceBus>(),
                _ => svc.GetRequiredService<InMemoryServiceBus>()
            };

            bus.ConfigureEndpoints(actions);

            return bus;
        });

        return services;
    }
}