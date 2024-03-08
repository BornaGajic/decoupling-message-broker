using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Framework.Test
{
    public class MessageBrokerTest() : MessageBrokerTestBase
    {
        [Fact]
        public async Task HelloWorldMessageHandlers()
        {
            var receivedA = Container.GetRequiredKeyedService<TaskCompletionSource<MessageA>>(nameof(MessageA));
            var receivedB = Container.GetRequiredKeyedService<TaskCompletionSource<MessageB>>(nameof(MessageB));

            var messageA = new MessageA() { Value = "Hello", Id = Guid.NewGuid() };
            var messageB = new MessageB() { Value = "World", Id = Guid.NewGuid() };

            await Bus.PublishAsync(messageA);
            await Bus.PublishAsync(messageB);

            var resultA = await receivedA.Task;
            var resultB = await receivedB.Task;

            resultA.ToString().Should().BeEquivalentTo(messageA.ToString());
            resultB.ToString().Should().BeEquivalentTo(messageB.ToString());
        }
    }
}