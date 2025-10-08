using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.Test.WebApi_A.Aops;

namespace Kurisu.Test.WebApi_A.Services.Implements;

[DiInject]
public class TestService : ITestService
{
    private readonly ILogger<TestService> _logger;

    [DiInject]
    public IDbContext DbContext { get; set; }

    public TestService(ILogger<TestService> logger)
    {
        _logger = logger;
    }

    [TestAop]
    public async Task<string> SayAsync()
    {
        _logger.LogInformation("doing");
        //await DbContext.Queryable<GainfoFaceEntity>().ToListAsync();
        return "a";
    }


    public async Task DoAsync()
    {
        Add();
    }

    private void Add()
    {
    }
}