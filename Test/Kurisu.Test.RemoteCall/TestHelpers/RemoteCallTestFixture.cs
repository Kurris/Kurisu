using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Kurisu.Test.RemoteCall.Api;

namespace Kurisu.Test.RemoteCall.TestHelpers;

public class RemoteCallTestFixture
{
    public IServiceProvider ServiceProvider { get; }

    public RemoteCallTestFixture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRemoteCall(new[] { typeof(IGetApi), typeof(IPostApi), typeof(IWeatherApi), typeof(IHeaderRouteApi), typeof(IContentApi), typeof(IAuthApi), typeof(IResponseApi), typeof(ICustomContentApi) });

        // register test helper handlers so attributes' Handler types can be resolved from DI
        services.AddSingleton<TestAuthTokenHandler>();
        services.AddSingleton<TestRemoteCallResultHandler>();
        services.AddSingleton<CustomContentHandler>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();
}
