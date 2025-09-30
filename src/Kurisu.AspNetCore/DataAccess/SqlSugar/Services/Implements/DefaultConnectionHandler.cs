using Kurisu.AspNetCore.DataAccess.SqlSugar.Options;
using Microsoft.Extensions.Options;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services.Implements;

/// <summary>
/// 默认Db数据库连接处理
/// </summary>
public class DefaultConnectionHandler : IDbConnectionFactory
{
    private readonly SqlSugarOptions _options;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    public DefaultConnectionHandler(IOptions<SqlSugarOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public string GetConnectionString()
    {
        return _options.DefaultConnectionString;
    }
}