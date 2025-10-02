namespace AspectCore.DependencyInjection
{
    internal interface IServiceResolveCallbackProvider
    {
        IServiceResolveCallback[] ServiceResolveCallbacks { get; }
    }
}