namespace Framework.Test
{
    public class MessageBrokerTest() : MessageBrokerTestBase
    {
        [Fact]
        public async Task HelloWorldMessageHandlers()
        {
            var receivedA = Container.GetRequiredService<TaskCompletionSource<MessageA>>();
            var receivedB = Container.GetRequiredService<TaskCompletionSource<MessageB>>();

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