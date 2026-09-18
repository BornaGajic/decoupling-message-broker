namespace Framework.Settings;

public record MessageBrokerSettings : IConfigurationSetting
{
    public static string ConfigurationKey => "MessageBroker";

    public const int DefaultConcurrencyLimit = 16;

    /// <summary>
    /// Maximum number of messages handled concurrently across all endpoints of this host, 1 to 65535.
    /// <see cref="DefaultConcurrencyLimit"/> when unset.
    /// </summary>
    public int? ConcurrencyLimit { get; init; }

    public string ConnectionString { get; init; }
    public MessageBrokerTransport Transport { get; init; }
    public MessageBrokerProvider Provider { get; init; }
}