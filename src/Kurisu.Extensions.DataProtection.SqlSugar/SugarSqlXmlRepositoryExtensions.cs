using System.Xml.Linq;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.Extensions.SqlSugar.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Kurisu.Extensions.DataProtection.SqlSugar;

/// <summary>
/// sugar xml repo
/// </summary>
public static class SugarSqlXmlRepositoryExtensions
{
    /// <summary>
    /// 持续化密钥到数据库（默认仓储实现）。
    /// </summary>
    /// <param name="builder">数据保护构建器。</param>
    /// <returns>数据保护构建器。</returns>
    public static IDataProtectionBuilder PersistKeysToDb(
        this IDataProtectionBuilder builder)
    {
        return builder.PersistKeysToDb<SugarSqlXmlRepository>();
    }

    /// <summary>
    /// 持续化密钥到数据库（自定义仓储实现）。
    /// </summary>
    /// <param name="builder">数据保护构建器。</param>
    /// <typeparam name="TRepository">仓储类型。</typeparam>
    /// <returns>数据保护构建器。</returns>
    public static IDataProtectionBuilder PersistKeysToDb<TRepository>(
        this IDataProtectionBuilder builder) where TRepository : BaseXmlRepository, new()
    {
        builder.Services.AddSingleton((Func<IServiceProvider, IConfigureOptions<KeyManagementOptions>>)(services =>
        {
            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var repository = new TRepository
            {
                Services = services,
                LoggerFactory = loggerFactory
            };
            return new ConfigureOptions<KeyManagementOptions>(options => options.XmlRepository = repository);
        }));

        return builder;
    }
}

/// <summary>
/// SqlSugar 数据保护密钥仓储实现。
/// </summary>
internal class SugarSqlXmlRepository : BaseXmlRepository
{
    /// <summary>
    /// 获取所有密钥元素。
    /// </summary>
    /// <returns>密钥元素集合。</returns>
    public override IReadOnlyCollection<XElement> GetAllElements()
    {
        // forces complete enumeration
        return GetAllElementsCore().ToList().AsReadOnly();

        IEnumerable<XElement> GetAllElementsCore()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
            db.AsSqlSugarDbContext().Client.CodeFirst.InitTables<DataProtectionKey>();
            var data = db.Queryable<DataProtectionKey>().ToList();
            foreach (var key in data)
            {
                if (!string.IsNullOrEmpty(key.Xml))
                {
                    yield return XElement.Parse(key.Xml);
                }
            }
        }
    }

    /// <summary>
    /// 存��密钥元素到数据库。
    /// </summary>
    /// <param name="element">密��元素。</param>
    /// <param name="friendlyName">友好名称。</param>
    public override void StoreElement(XElement element, string friendlyName)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
        db.AsSqlSugarDbContext().Client.CodeFirst.InitTables<DataProtectionKey>();
        var newKey = new DataProtectionKey
        {
            FriendlyName = friendlyName,
            Xml = element.ToString(SaveOptions.DisableFormatting)
        };

        db.Insert(newKey);
    }
}

/// <summary>
/// xml 仓储基类，定义数据保护密钥的存储与获取接口。
/// </summary>
public abstract class BaseXmlRepository : IXmlRepository
{
    /// <summary>
    /// 服务提供器。
    /// </summary>
    public IServiceProvider Services { get; set; }

    /// <summary>
    /// 日志工厂。
    /// </summary>
    public ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// 获取所有密钥元素。
    /// </summary>
    /// <returns>密钥元素集合。</returns>
    public abstract IReadOnlyCollection<XElement> GetAllElements();

    /// <summary>
    /// 存储密钥元素。
    /// </summary>
    /// <param name="element">密钥元���。</param>
    /// <param name="friendlyName">友好名称。</param>
    public abstract void StoreElement(XElement element, string friendlyName);
}

/// <summary>
/// 数据保护密钥实体。
/// </summary>
public class DataProtectionKey
{
    /// <summary>
    /// 主键 Id。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 密钥友好名称。
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string FriendlyName { get; set; }

    /// <summary>
    /// xml 密钥数据。
    /// </summary>
    [SugarColumn(IsNullable = true, ColumnDataType = "text")]
    public string Xml { get; set; }
}