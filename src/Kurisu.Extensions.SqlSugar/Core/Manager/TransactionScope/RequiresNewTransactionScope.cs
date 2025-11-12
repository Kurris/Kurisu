using System.Data;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

public class RequiresNewTransactionScope : RequiredTransactionScope
{
    public RequiresNewTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action<bool> afterScope)
        : base(client, isolationLevel, false, afterScope)
    {
    }
}