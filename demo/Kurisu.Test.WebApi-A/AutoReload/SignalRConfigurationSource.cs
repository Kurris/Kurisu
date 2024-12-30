namespace Kurisu.Test.WebApi_A.AutoReload;
public class SignalRConfigurationSource : IConfigurationSource
{
    private readonly SignalROptions _options;

    public SignalRConfigurationSource(SignalROptions options)
    {
        _options = options;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SignalRConfigurationProvider(null,_options);
    }
}
