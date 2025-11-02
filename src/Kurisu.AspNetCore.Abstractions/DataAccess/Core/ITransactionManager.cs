using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

public interface ITransactionManager
{
    ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null);
}