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

    // 新增：Mandatory 传播性方法，用于测试当存在或不存在 ambient 事务时的行为
    Task InnerMandatoryAsync(string name);
    Task InnerMandatoryAndThrowAsync(string name);

    // 新增：Nested 传播性方法
    Task InnerNestedAsync(string name);
    Task InnerNestedAndThrowAsync(string name);

    // 新增：Never 传播性方法
    Task InnerNeverAsync(string name);
}
