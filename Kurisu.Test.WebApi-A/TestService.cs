using Kurisu.Proxy.Attributes;

namespace Kurisu.Test.WebApi_A;

public class TestService : ITryTestService, IScopeDependency
{
    public async Task<string> Say()
    {
        await Wall();
        return await Task.FromResult("ni hao");
    }

    public async Task Wall()
    {
    
    }
}


public interface ITryTestService
{
    [Log]
    Task<string> Say();


    Task Wall();
}

public class LogAttribute : AopAttribute
{
    public LogAttribute() : base(typeof(Log))
    {

    }
}