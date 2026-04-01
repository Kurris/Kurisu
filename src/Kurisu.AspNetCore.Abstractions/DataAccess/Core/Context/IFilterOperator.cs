namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

public interface IFilterOperator
{
    IDisposable CreateDatasourceScope(string name);
    IDisposable CreateDatasourceScope();


    IDisposable IgnoreTenant();

    IDisposable IgnoreSoftDeleted();

    IDisposable EnableDataPermission();

    IDisposable EnableCrossTenant();


    IDisposable IgnoreSharding();
}