namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

public abstract class ReadAbstractDbContext<TOperationClient> : IReadDbContext where TOperationClient : class
{
    protected ReadAbstractDbContext(IServiceProvider serviceProvider)
    {
    }
}
