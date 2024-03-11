namespace Framework
{
    internal class BusConfigurator : IBusConfigurator
    {
        protected internal Dictionary<string, IEnumerable<Type>> EndpointMap { get; set; } = [];

        public void ReceiveEndpoint(string endpointName, Action<IBusEndpointConfigurator> endpointConfigurator)
        {
            var busEndpointConfig = new BusEndpointConfigurator();
            endpointConfigurator(busEndpointConfig);
            EndpointMap[endpointName] = busEndpointConfig.Handlers;
        }
    }
}