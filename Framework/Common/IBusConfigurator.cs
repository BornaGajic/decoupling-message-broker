namespace Framework;

public interface IBusConfigurator
{
    /// <summary>
    /// Message handlers registered under the same <paramref name="endpointName"/> will compete with each other at who will handle the message.<br/>
    /// If you want to have all handlers handle the message, create an endpoint for each handler.
    /// </summary>
    void ReceiveEndpoint(string endpointName, int concurrency, Action<IBusEndpointConfigurator> endpointConfigurator);
}