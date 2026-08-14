namespace Framework.Cancellation;

internal readonly struct HandlerCancellationRegistration : IDisposable
{
    private readonly Guid _messageId;
    private readonly MessageHandlerCancellation _source;

    internal HandlerCancellationRegistration(MessageHandlerCancellation source, Guid messageId)
    {
        _source = source;
        _messageId = messageId;
    }

    public readonly void Dispose() => Unregister();

    public readonly void Unregister() => _source.UnregisterCancellation(_messageId);
}