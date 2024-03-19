namespace Framework;

public interface IServiceBus
{
    Task PublishAsync<T>(T message) where T : class, IMessage;

    void Start();

    void Stop();
}