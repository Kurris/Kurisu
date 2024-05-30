namespace Kurisu.Test.WebApi_A.Services.Implements;

public class TestService : ITestService
{
    private readonly ILogger<TestService> _logger;

    public TestService(ILogger<TestService> logger)
    {
        _logger = logger;
    }
    
    public Task<string> SayAsync()
    {
        _logger.LogInformation("doing");
        return Task.FromResult("hello");
    }

    
    public async Task DoAsync()
    {
        Add();
    }

    private void Add()
    {
    }
}