namespace Framework.Cancellation;

internal record MessageToCancel : IMessage
{
    public Guid MessageIdToCancel { get; init; }
    public Guid Id { get; init; }
}