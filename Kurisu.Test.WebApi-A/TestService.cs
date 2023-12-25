using Kurisu.Core.Proxy.Attributes;
using Kurisu.SqlSugar.Attributes;

namespace Kurisu.Test.WebApi_A;

public class TestService : ITryTestService
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

public interface ITryTestService : IScopeDependency
{
    [Log]
    [Transactional]
    Task<string> Say();

    Task Wall();
}

public class LogAttribute : AopAttribute
{
    public LogAttribute()
    {
        Interceptors = new[] { typeof(Log) };
    }
}