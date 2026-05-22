using MassTransit;

namespace Framework.Common;

// I think Faults cannot be decoupled by default, need to investigate.
internal class FaultConsumer<T> : IConsumer<Fault<T>>
{
    public Task Consume(ConsumeContext<Fault<T>> context) => Task.CompletedTask;
}