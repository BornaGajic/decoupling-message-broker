namespace Framework.Cancellation;

internal class MessageHandlerCancellation
{
    private readonly Lazy<MemoryCache> _cache;

    public MessageHandlerCancellation()
    {
        // example, memory cache is used because each process will check their own memory for a cancellation flag.
        // Distributed cache in not needed for this implementation.
        _cache = new Lazy<MemoryCache>(() => new MemoryCache("message-handler-cancellation"));
    }

    public void Cancel(Guid messageId)
    {
        if (_cache.Value.TryGetValue<IMessageContext>(messageId.ToString(), out var messageContext))
        {
            messageContext.Cancel();
            UnregisterCancellation(messageId);
        }
    }

    internal async ValueTask<HandlerCancellationRegistration> RegisterCancellationAsync(Guid messageId, IMessageContext messageContext)
    {
        await _cache.Value.SetAsync(messageId.ToString(), messageContext, DateTime.UtcNow.AddMinutes(15));
        return new HandlerCancellationRegistration(this, messageId);
    }

    internal void UnregisterCancellation(Guid messageId)
    {
        _cache.Value.Remove(messageId.ToString());
    }
}