namespace Framework.EasyNetQ;

internal sealed class EasyNetQMessageContext : IMessageContext
{
    private readonly EasyNetQServiceBus _bus;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public EasyNetQMessageContext(EasyNetQServiceBus bus)
    {
        _bus = bus;
    }

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public Task Publish<T>(T message) where T : IMessage, new()
        => _bus.PublishAsync(message, typeof(T), CancellationToken.None);

    public Task Send<T>(string destination, T message) where T : IMessage, new()
        => _bus.SendAsync(destination, message, typeof(T), CancellationToken.None);
}