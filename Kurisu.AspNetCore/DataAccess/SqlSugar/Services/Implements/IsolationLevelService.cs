using System.Data;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services.Implements;

internal class IsolationLevelService : IIsolationLevelService
{
    private IsolationLevel _isolationLevel = IsolationLevel.RepeatableRead;

    /// <inheritdoc />
    public void Set(IsolationLevel isolationLevel)
    {
        _isolationLevel = isolationLevel;
    }

    /// <inheritdoc />
    public IsolationLevel Get()
    {
        return _isolationLevel;
    }
}