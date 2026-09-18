using Framework.Cancellation;
using Framework.EasyNetQ;
using Framework.Extensions;
using Framework.MassTransit;
using Framework.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework;

public static class MessageBrokerServiceCollectionExtension
{
    /// <summary>
    /// 1. Registers <see cref="IServiceBus"/> as <see cref="MassTransitRabbitMqServiceBus"/>, <see cref="EasyNetQServiceBus"/> or <see cref="MassTransitInMemoryServiceBus"/> depending on <see cref="MessageBrokerSettings.Transport"/>.<br/>
    /// 2. Registers <see cref="MessageHandlerCancellation"/> and ICache feature.<br/>
    /// 3. Adds <see cref="MessageBrokerSettings"/> to <see cref="IOptions{TOptions}"/>
    /// </summary>
    public static IServiceCollection RegisterMessageBroker<TSettings>(this IServiceCollection services, IConfiguration configuration)
        where TSettings : MessageBrokerSettings, IConfigurationSetting
    {
        services.RegisterMessageBrokerOptions<TSettings>(configuration);
        services.RegisterMessageBrokerServices<TSettings>(configuration);
        return services;
    }

    public static IServiceCollection RegisterMessageBroker(this IServiceCollection services, IConfiguration configuration)
        => services.RegisterMessageBroker<MessageBrokerSettings>(configuration);

    public static IServiceCollection RegisterMessageBrokerEndpoint(this IServiceCollection services, Action<IBusConfigurator> busConfig)
    {
        services.AddTransient(x => busConfig);
        return services;
    }

    private static OptionsBuilder<TSettings> RegisterMessageBrokerOptions<TSettings>(this IServiceCollection services, IConfiguration configuration)
        where TSettings : MessageBrokerSettings, IConfigurationSetting
    {
        return services.TryAddOptions<TSettings>(configuration)
            .Validate(
                settings => settings.Transport == MessageBrokerTransport.InMemory || !string.IsNullOrWhiteSpace(settings.ConnectionString),
                $"{TSettings.ConfigurationKey}:ConnectionString is required unless {TSettings.ConfigurationKey}:Transport is {nameof(MessageBrokerTransport.InMemory)}."
            )
            .Validate(
                settings => settings.ConcurrencyLimit is null or (> 0 and <= ushort.MaxValue),
                $"{TSettings.ConfigurationKey}:ConcurrencyLimit must be greater than zero when set."
            );
    }

    private static IServiceCollection RegisterMessageBrokerServices<TSettings>(this IServiceCollection services, IConfiguration configuration)
        where TSettings : MessageBrokerSettings, IConfigurationSetting
    {
        services.AddLogging(cfg => cfg.AddConsole());

        var settings = configuration.GetSection(TSettings.ConfigurationKey).Get<TSettings>();
        var provider = settings?.Provider ?? default;
        var transport = settings?.Transport ?? default;

        if (transport != MessageBrokerTransport.InMemory && provider == MessageBrokerProvider.EasyNetQ)
        {
            services.RegisterEasyNetQServiceBus<TSettings>();
        }
        else
        {
            services.RegisterMassTransitServiceBus<TSettings>();
        }

        services.TryAddSingleton<CancelHandlerExecution>();

        services.TryAddSingleton<IServiceBus>(svc =>
        {
            var settings = svc.GetRequiredService<IOptions<TSettings>>();
            var actions = svc.GetServices<Action<IBusConfigurator>>();

            ServiceBus bus = (settings.Value.Transport, settings.Value.Provider) switch
            {
                (MessageBrokerTransport.InMemory, _) or (_, MessageBrokerProvider.MassTransit) => svc.GetRequiredService<MassTransitServiceBus>(),
                _ => svc.GetRequiredService<EasyNetQServiceBus>()
            };

            bus.ConfigureEndpoints(actions);

            return bus;
        });

        services.TryAddSingleton<MessageHandlerCancellation>();

        return services;
    }
}