using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.Test.DataAccess.Trans;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Kurisu.Test.DataAccess.Trans.Mock;

public class TransactionalOuterService : ITransactionalOuterService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContext _dbContext;

    public TransactionalOuterService(IServiceProvider serviceProvider, IDbContext dbContext)
    {
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
    }

    [Transactional]
    public async Task OuterRequiredAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        await inner.InnerRequiredAsync(innerName);
    }

    [Transactional]
    public async Task OuterRequiredOnExceptionAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        await inner.InnerRequiredAsync(innerName);
        throw new Exception("test rollback");
    }

    [Transactional]
    public async Task OuterRequiresNewAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        await inner.InnerRequiresNewAsync(innerName);
    }

    [Transactional]
    public async Task OuterRequiresNewRollbackAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        try
        {
            await inner.InnerRequiresNewAndThrowAsync(innerName);
        }
        catch
        {
            // swallow so outer can commit
        }
    }

    [Transactional]
    public async Task OuterRequiresNewNoCatchAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        await inner.InnerRequiresNewAndThrowAsync(innerName);
    }

    // 新增：Outer 调用内层 InsertAndThrow（Required 传播），外层捕获异常
    [Transactional]
    public async Task OuterRequiredInnerThrowsCatchAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        try
        {
            await inner.InsertAndThrowAsync(innerName);
        }
        catch
        {
            // swallow
        }
    }

    // 新增：Outer 调用内层 InsertAndThrow（Required 传播），外层不捕获异常
    [Transactional]
    public async Task OuterRequiredInnerThrowsNoCatchAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        await inner.InsertAndThrowAsync(innerName);
    }

    // 新增：Outer 为 RequiresNew，内层为 InsertAndThrow（Required 传播），外层捕获异常
    [Transactional(Propagation = Propagation.RequiresNew)]
    public async Task OuterRequiresNewInnerThrowsCatchAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        try
        {
            await inner.InsertAndThrowAsync(innerName);
        }
        catch
        {
            // swallow so outer attempts to commit; but since inner joined the same transaction, it should rollback
        }
    }

    // 新增：Outer 为 RequiresNew，内层为 InsertAndThrow（Required 传播），外层不捕获异常
    [Transactional(Propagation = Propagation.RequiresNew)]
    public async Task OuterRequiresNewInnerThrowsNoCatchAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        await inner.InsertAndThrowAsync(innerName);
    }

    [Transactional]
    public async Task OuterRequiredInnerNoRollbackAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        await inner.InsertAndThrowNoRollbackAsync(innerName);
    }

    [Transactional]
    public async Task OuterRequiredInnerSwallowAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        await inner.InsertAndThrowAndSwallowAsync(innerName);
    }

    [Transactional]
    public async Task OuterRequiredInnerRequiresNewNoCatchAsync(string outerName, string innerName)
    {
        await _dbContext.InsertAsync(new TxTest { Name = outerName });
        var inner = _serviceProvider.GetRequiredService<ITransactionalInnerService>();
        await inner.InsertAndThrowNoRollbackRequiresNewAsync(innerName);
    }
}
