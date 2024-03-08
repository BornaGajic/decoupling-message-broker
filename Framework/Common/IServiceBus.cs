namespace Framework;

public interface IServiceBus
{
    Task PublishAsync<T>(T message) where T : class, IMessage;

    Task SendAsync<T>(Uri address, T message) where T : class, IMessage;

    Task SendAsync<T>(string destination, T message) where T : class, IMessage;

    void Start();

    Task StartAsync();

    void Stop();

    Task StopAsync();
}