using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.Test.DataAccess.Trans;
using System;
using System.Threading.Tasks;

namespace Kurisu.Test.DataAccess.Trans.Mock;

public class TransactionalInnerService : ITransactionalInnerService
{
    private readonly IDbContext _dbContext;

    public TransactionalInnerService(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Transactional]
    public async Task InsertAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
    }

    [Transactional]
    public async Task InsertAndThrowAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
        throw new Exception("test rollback");
    }

    [Transactional(Propagation = Propagation.Required)]
    public async Task InnerRequiredAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
    }

    [Transactional(Propagation = Propagation.RequiresNew)]
    public async Task InnerRequiresNewAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
    }

    [Transactional(Propagation = Propagation.RequiresNew)]
    public async Task InnerRequiresNewAndThrowAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
        throw new Exception("inner fail");
    }

    [Transactional(NoRollbackFor = typeof(TestNotRollbackException))]
    public async Task InsertAndThrowNoRollbackAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
        throw new TestNotRollbackException("do not rollback");
    }

    [Transactional(Propagation = Propagation.RequiresNew, NoRollbackFor = typeof(TestNotRollbackException))]
    public async Task InsertAndThrowNoRollbackRequiresNewAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
        throw new TestNotRollbackException("do not rollback (requires new)");
    }

    [Transactional]
    public async Task InsertAndThrowAndSwallowAsync(string name)
    {
        try
        {
            await _dbContext.InsertAsync(new TxTest { Name = name });
            throw new TestNotRollbackException("do not rollback but swallowed");
        }
        catch (TestNotRollbackException)
        {
            // swallow intentionally
        }
    }
}
