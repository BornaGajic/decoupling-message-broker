namespace Framework;

public interface IBusConfigurator
{
    void ReceiveEndpoint(string endpointName, Action<IBusEndpointConfigurator> endpointConfigurator);
}