using MassTransit;

namespace Framework
{
    public sealed class MessageContext : IMessageContext
    {
        private readonly Uri _baseUri;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ConsumeContext _messageHandlerContext;

        public MessageContext(ConsumeContext messageHandlerContext, Uri baseUri)
        {
            _messageHandlerContext = messageHandlerContext;
            _baseUri = baseUri;
        }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task Publish<T>(T message) where T : IMessage, new()
        {
            await _messageHandlerContext.Publish(message);
        }

        public async Task Send<T>(string destination, T message) where T : IMessage, new()
        {
            var endpoint = await _messageHandlerContext.GetSendEndpoint(new Uri(_baseUri, destination));
            await endpoint.Send(message);
        }
    }
}