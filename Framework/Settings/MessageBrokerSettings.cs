namespace Framework.Settings;

public record MessageBrokerSettings : IConfigurationSetting
{
    public static string ConfigurationKey => "MessageBroker";

    public string ConnectionString { get; init; }
    public MessageBrokerTransport Transport { get; init; }
}