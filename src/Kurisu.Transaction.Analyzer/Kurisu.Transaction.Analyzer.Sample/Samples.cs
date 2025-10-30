using System;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Microsoft.Extensions.DependencyInjection;


class Program
{
    void Main()
    {
        IServiceCollection services = new ServiceCollection();
        IServiceProvider provider = services.BuildServiceProvider();

        var nestedCaller2 = provider.GetRequiredService<INestedCaller>();
        nestedCaller2.Run();
        
        var nestedCaller22 = provider.GetRequiredService<NestedCaller>();
        nestedCaller22.Run();
    }
}

public interface IService
{
    void DoMandatory();
}

public class Service : IService
{
    [Transactional(Propagation = Propagation.Mandatory)]
    public void DoMandatory()
    {
    }
}

public interface INestedCaller
{
    void Run();
}

public interface ITriggerCaller
{
    //[Transactional]
    void Run();
}

public class TriggerCaller : ITriggerCaller
{
    private readonly IService _svc;
    private readonly Service _service;

    public TriggerCaller(IService svc, Service service)
    {
        _svc = svc;
        _service = service;
    }

    public void Run()
    {
        _svc.DoMandatory();
        _service.DoMandatory();
    }
}

public class NestedCaller : INestedCaller
{
    private readonly ITriggerCaller _caller;

    public NestedCaller(ITriggerCaller caller)
    {
        _caller = caller;
    }

    [Transactional]
    public void Run()
    {
        _caller.Run();
    }
}