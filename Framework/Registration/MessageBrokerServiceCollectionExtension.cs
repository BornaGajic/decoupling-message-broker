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
    /// 2. Registers <see cref="MessageHandlerCancellation"/> and ICache feature depending on <see cref="MessageBrokerStartupOptions.UseCancellation"/>.<br/>
    /// 3. Adds <see cref="MessageBrokerSettings"/> to <see cref="IOptions{TOptions}"/>
    /// </summary>
    public static IServiceCollection RegisterMessageBroker(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterMessageBrokerOptions(configuration);
        services.RegisterMessageBrokerServices();
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

        services.TryAddSingleton<IServiceBus>(svc =>
        {
            var settings = svc.GetRequiredService<IOptions<MessageBrokerSettings>>();

            return settings.Value.Transport switch
            {
                MessageBrokerTransport.RabbitMq => svc.GetRequiredService<RabbitMqServiceBus>(),
                _ => svc.GetRequiredService<InMemoryServiceBus>()
            };
        });

        return services;
    }
}