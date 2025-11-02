using System.Data;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

public class MandatoryTransactionScope : RequiredTransactionScope
{
    public MandatoryTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action<bool> afterScope)
        : base(client, isolationLevel, client.Ado.IsAnyTran(), afterScope)
    {
    }
}