using Kurisu.AspNetCore.EventBus;
using Kurisu.AspNetCore.EventBus.Abstractions;

namespace Kurisu.Test.WebApi_A;

public class TestEventBusPipeline : IEventBusPipeline<TestRequest, int>
{
    public async Task<int> InvokeAsync(TestRequest @in, InvokeDelegate<int> next)
    {
        return await next();
    }
}



public class TestEventBusPipeline1 : IEventBusPipeline<int, string>
{
    public Task<string> InvokeAsync(int request, InvokeDelegate<string> next)
    {
        throw new NotImplementedException();
    }
}

