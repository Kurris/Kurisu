using System.Data;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

internal class RequiresNewTransactionScope : RequiredTransactionScope
{
    public RequiresNewTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action afterScope)
        : base(client, isolationLevel, false, afterScope)
    {
    }
}