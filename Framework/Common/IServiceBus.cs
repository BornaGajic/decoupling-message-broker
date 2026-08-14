namespace Framework;

public interface IServiceBus
{
    Task CancelAsync(Guid messageId, CancellationToken token = default);

    Task PublishAsync<T>(T message, CancellationToken token = default) where T : class, IMessage;

    Task SendAsync<T>(Uri address, T message, CancellationToken token = default) where T : class, IMessage;

    Task SendAsync<T>(string destination, T message, CancellationToken token = default) where T : class, IMessage;

    void Start();

    void Stop();
}