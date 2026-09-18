using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Framework.MassTransit;

internal abstract class MassTransitServiceBus : ServiceBus
{
    private IBusControl _bus;

    public MassTransitServiceBus(IServiceProviderIsService serviceProviderIsService)
        : base(serviceProviderIsService)
    {
    }

    protected abstract Uri HostAdress { get; }

    public override async Task PublishAsync<T>(T message, CancellationToken token)
    {
        await _bus.Publish(message, token);
    }

    public override Task SendAsync<T>(string destination, T message, CancellationToken token)
    {
        return SendAsync(new Uri(HostAdress, destination), message, token);
    }

    public override async Task SendAsync<T>(Uri address, T message, CancellationToken token)
    {
        var sendEndpoint = await _bus.GetSendEndpoint(address);
        await sendEndpoint.Send(message, token);
    }

    public override void Start(int concurrencyLimit)
    {
        if (_bus is not null)
            return;

        var retryRabbitMqPolicy = Policy
            .Handle<RabbitMqConnectionException>()
            .WaitAndRetry(
                3,
                attempt => TimeSpan.FromSeconds(10)
            );

        _bus = retryRabbitMqPolicy.Execute(() =>
        {
            var bus = Setup(concurrencyLimit, CancellationToken.None);
            bus.Start();
            return bus;
        }) ?? throw new Exception("Service bus failed to initialize.");
    }

    public override async Task StartAsync(int concurrencyLimit, CancellationToken token = default)
    {
        if (_bus is not null)
            return;

        var retryRabbitMqPolicy = Policy
            .Handle<RabbitMqConnectionException>()
            .WaitAndRetryAsync(
                5,
                attempt => TimeSpan.FromSeconds(60)
            );

        _bus = await retryRabbitMqPolicy.ExecuteAsync(async (ct) =>
        {
            var bus = Setup(concurrencyLimit, ct);
            var busHandle = await bus.StartAsync(ct);
            await busHandle.Ready;
            return bus;
        }, token) ?? throw new Exception("Service bus failed to initialize.");
    }

    public override void Stop() => _bus.Stop();

    public override Task StopAsync(CancellationToken token) => _bus.StopAsync(token);

    /// <summary>
    /// Setup and create a Bus Control instance
    /// </summary>
    protected abstract IBusControl Setup(int concurrencyLimit = 1, CancellationToken token = default);
}