using System.Threading.Tasks;

namespace Kurisu.Test.DataAccess.Trans.Mock;

public interface ITransactionalInnerService
{
    Task InsertAsync(string name);
    Task InsertAndThrowAsync(string name);
    Task InsertAndThrowNoRollbackAsync(string name);
    Task InsertAndThrowNoRollbackRequiresNewAsync(string name);
    Task InsertAndThrowAndSwallowAsync(string name);

    Task InnerRequiredAsync(string name);
    Task InnerRequiresNewAsync(string name);
    Task InnerRequiresNewAndThrowAsync(string name);
}
