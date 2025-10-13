using System.Threading.Tasks;

namespace Kurisu.Test.DataAccess.Trans.Mock;

public interface ITransactionalOuterService
{
    Task OuterRequiredAsync(string outerName, string innerName);
    Task OuterRequiredOnExceptionAsync(string outerName, string innerName);
    Task OuterRequiresNewAsync(string outerName, string innerName);
    Task OuterRequiresNewRollbackAsync(string outerName, string innerName);
    Task OuterRequiresNewNoCatchAsync(string outerName, string innerName);
    Task OuterRequiredInnerThrowsCatchAsync(string outerName, string innerName);
    Task OuterRequiredInnerThrowsNoCatchAsync(string outerName, string innerName);
    Task OuterRequiresNewInnerThrowsCatchAsync(string outerName, string innerName);
    Task OuterRequiresNewInnerThrowsNoCatchAsync(string outerName, string innerName);
    Task OuterRequiredInnerNoRollbackAsync(string outerName, string innerName);
    Task OuterRequiredInnerSwallowAsync(string outerName, string innerName);
    Task OuterRequiredInnerRequiresNewNoCatchAsync(string outerName, string innerName);
}
