using Framework.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Framework.MassTransit;

internal static class MassTransitServiceBusStartupExtensions
{
    /// <summary>
    /// Registers <see cref="MassTransitServiceBus"/> as <see cref="MassTransitRabbitMqServiceBus"/> or <see cref="MassTransitInMemoryServiceBus"/> depending on <see cref="MessageBrokerSettings.Transport"/>.
    /// </summary>
    public static IServiceCollection RegisterMassTransitServiceBus<TSettings>(this IServiceCollection services)
        where TSettings : MessageBrokerSettings, IConfigurationSetting
    {
        services.TryAddSingleton<MassTransitRabbitMqServiceBus>();
        services.TryAddSingleton<MassTransitInMemoryServiceBus>();

        services.TryAddSingleton<MassTransitServiceBus>(svc =>
        {
            var settings = svc.GetRequiredService<IOptions<TSettings>>();

            return settings.Value.Transport switch
            {
                MessageBrokerTransport.RabbitMq => svc.GetRequiredService<MassTransitRabbitMqServiceBus>(),
                _ => svc.GetRequiredService<MassTransitInMemoryServiceBus>()
            };
        });

        return services;
    }
}