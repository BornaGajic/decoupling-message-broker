namespace Framework.Settings;

public record EndpointSettings
{
    public required int Concurrency { get; init; } = 1;
    public required string Name { get; init; }
    public HashSet<Type> HandlerTypes { get; init; } = [];
}