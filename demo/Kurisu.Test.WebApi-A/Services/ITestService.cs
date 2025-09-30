namespace Kurisu.Test.WebApi_A.Services;

public interface ITestService
{
    Task<string> SayAsync();

    Task DoAsync();
}