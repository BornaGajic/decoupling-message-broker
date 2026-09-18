using EasyNetQ;
using EasyNetQ.ConnectionString;
using Framework.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Framework.EasyNetQ;

internal static class EasyNetQServiceBusStartupExtensions
{
    /// <summary>
    /// Registers EasyNetQ's <see cref="IBus"/>, connected with <see cref="MessageBrokerSettings.ConnectionString"/>, and <see cref="EasyNetQServiceBus"/> on top of it.
    /// </summary>
    public static IServiceCollection RegisterEasyNetQServiceBus<TSettings>(this IServiceCollection services)
        where TSettings : MessageBrokerSettings, IConfigurationSetting
    {
        services
            .AddEasyNetQ(svc =>
            {
                var settings = svc.GetRequiredService<IOptions<TSettings>>();
                var concurrencyLimit = (ushort)(settings.Value.ConcurrencyLimit ?? MessageBrokerSettings.DefaultConcurrencyLimit);

                var configuration = new AmqpConnectionStringParser().Parse(settings.Value.ConnectionString);
                configuration.ConsumerDispatcherConcurrency = concurrencyLimit;
                configuration.PrefetchCount = concurrencyLimit;
                configuration.PublisherConfirms = true; // disables Fire and Forget (waits until server recognizes the message)

                return configuration;
            })
            .UseSystemTextJsonV2(new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            });

        services.TryAddSingleton<EasyNetQServiceBus>();

        return services;
    }
}