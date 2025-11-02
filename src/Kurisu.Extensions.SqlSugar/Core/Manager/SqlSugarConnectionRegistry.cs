using Kurisu.AspNetCore.Abstractions.DataAccess.Core;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

internal class SqlSugarConnectionRegistry : IDbConnectionRegistry
{
    public void Register(Dictionary<string, string> connectionStrings)
    {
        throw new NotImplementedException();
    }

    public void Register(string name, string connectionString)
    {
        throw new NotImplementedException();
    }

    public string GetConnectionString(string name)
    {
        throw new NotImplementedException();
    }

    public bool Exists(string name)
    {
        throw new NotImplementedException();
    }
}