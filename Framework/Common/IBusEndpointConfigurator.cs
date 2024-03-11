namespace Framework;

public interface IBusEndpointConfigurator
{
    void AddHandler<T>() where T : IMessageHandler;
}