using System.Data;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

internal class MandatoryTransactionScope : RequiredTransactionScope
{
    public MandatoryTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action afterScope)
        : base(client, isolationLevel, client.Ado.IsAnyTran(), afterScope)
    {
    }
}