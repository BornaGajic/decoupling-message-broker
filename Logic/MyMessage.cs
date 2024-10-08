using Framework;

namespace Logic;

public record MyMessage : IMessage
{
    public Guid Id { get; init; }
    public string Text { get; set; } = default!; // Microsoft: "don't worry, I'm not null! ;)"
}