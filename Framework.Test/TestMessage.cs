namespace Framework.Test
{
    public record MessageA : IMessage
    {
        public Guid Id { get; init; }
        public string Value { get; init; } = default!;
    }

    public record MessageB : IMessage
    {
        public Guid Id { get; init; }
        public string Value { get; init; } = default!;
    }
}