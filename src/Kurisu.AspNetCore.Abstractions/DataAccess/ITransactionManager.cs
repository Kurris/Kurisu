using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess;

public interface ITransactionManager
{
    Task<ITransactionScopeManager> BeginAsync(Propagation propagation);
    Task<ITransactionScopeManager> BeginAsync(Propagation propagation, IsolationLevel isolationLevel);
}