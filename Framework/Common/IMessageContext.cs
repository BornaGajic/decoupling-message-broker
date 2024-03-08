namespace Framework;

public interface IMessageContext
{
    CancellationToken CancellationToken { get; }

    void Cancel();

    Task Publish<T>(T message) where T : IMessage, new();

    Task Send<T>(string destination, T message) where T : IMessage, new();
}