namespace Framework;

internal class BusEndpointConfigurator : IBusEndpointConfigurator
{
    protected internal HashSet<Type> Handlers { get; set; } = [];

    public void AddHandler<T>() where T : IMessageHandler => Handlers.Add(typeof(T));
}