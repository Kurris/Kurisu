using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

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

    // 新增：Mandatory 传播性 - 如果没有外部事务应抛出 InvalidOperationException
    [Transactional(Propagation = Propagation.Mandatory)]
    public async Task InnerMandatoryAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
    }

    [Transactional(Propagation = Propagation.Mandatory)]
    public async Task InnerMandatoryAndThrowAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
        throw new Exception("mandatory inner fail");
    }

    // 新增：Nested 传播性 - 在有外层事务时使用 savepoint
    [Transactional(Propagation = Propagation.Nested)]
    public async Task InnerNestedAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
    }

    [Transactional(Propagation = Propagation.Nested)]
    public async Task InnerNestedAndThrowAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
        throw new Exception("nested inner fail");
    }

    // 新增：Never 传播性 - 在存在外层事务时应抛出异常；无外层事务时正常执行
    [Transactional(Propagation = Propagation.Never)]
    public async Task InnerNeverAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
    }
}
