using System.Data;
using System.Threading;
using System.Collections.Generic;
using Kurisu.AspNetCore.Abstractions.DataAccess;

public class DefaultTransactionManager : ITransactionManager
{
    private readonly AsyncLocal<Stack<ITransactionScopeManager>> _scopeStack = new();

    private Stack<ITransactionScopeManager> ScopeStack
    {
        get
        {
            if (_scopeStack.Value == null)
                _scopeStack.Value = new Stack<ITransactionScopeManager>();
            return _scopeStack.Value;
        }
    }

    public bool HasTransaction() => ScopeStack.Count > 0;

    public async Task<ITransactionScopeManager> BeginAsync(Propagation propagation)
    {
        throw new NotImplementedException();
    }

    public async Task<ITransactionScopeManager> BeginAsync(Propagation propagation, IsolationLevel isolationLevel)
    {
        switch (propagation)
        {
            case Propagation.Required:
                if (HasTransaction())
                    return ScopeStack.Peek();
                var requiredScope = await CreateScopeAsync(isolationLevel);
                ScopeStack.Push(requiredScope);
                return requiredScope;
            case Propagation.RequiresNew:
                var newScope = await CreateScopeAsync(isolationLevel);
                ScopeStack.Push(newScope);
                return newScope;
            case Propagation.Supports:
                return HasTransaction() ? ScopeStack.Peek() : NullTransactionScope.Instance;
            case Propagation.NotSupported:
                return NullTransactionScope.Instance;
            default:
                throw new NotSupportedException();
        }
    }

    private Task<ITransactionScopeManager> CreateScopeAsync(IsolationLevel? isolationLevel)
    {
        return Task.FromResult<ITransactionScopeManager>(new RealTransactionScope(isolationLevel, this));
    }

    // 事务作用域结束时调用
    public void EndScope()
    {
        if (HasTransaction())
            ScopeStack.Pop();
    }
}