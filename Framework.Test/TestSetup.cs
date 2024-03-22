using Microsoft.Extensions.Configuration;

namespace Framework.Test;

public abstract class TestSetup
{
    public static IConfigurationRoot SetupConfiguration()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, false)
            .AddEnvironmentVariables("TEST_")
            .Build();
    }

    public static IServiceProvider SetupContainer(Action<IServiceCollection> callback)
    {
        var services = new ServiceCollection();
        callback?.Invoke(services);
        return services.BuildServiceProvider();
    }
}