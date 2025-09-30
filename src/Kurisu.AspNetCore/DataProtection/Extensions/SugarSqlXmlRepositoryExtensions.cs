using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Kurisu.AspNetCore.DataProtection.Extensions;

/// <summary>
/// sugar xml repo
/// </summary>
public static class SugarSqlXmlRepositoryExtensions
{
    /// <summary>
    /// 持续化到db总
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IDataProtectionBuilder PersistKeysToDb(
        this IDataProtectionBuilder builder)
    {
        return builder.PersistKeysToDb<SugarSqlXmlRepository>();
    }

    /// <summary>
    /// 持续化到数据库
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TRepository"></typeparam>
    /// <returns></returns>
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

internal class SugarSqlXmlRepository : BaseXmlRepository
{
    /// <inheritdoc />
    public override IReadOnlyCollection<XElement> GetAllElements()
    {
        // forces complete enumeration
        return GetAllElementsCore().ToList().AsReadOnly();

        IEnumerable<XElement> GetAllElementsCore()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
            db.Client.CodeFirst.InitTables<DataProtectionKey>();
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

    /// <inheritdoc />
    public override void StoreElement(XElement element, string friendlyName)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
        db.Client.CodeFirst.InitTables<DataProtectionKey>();
        var newKey = new DataProtectionKey
        {
            FriendlyName = friendlyName,
            Xml = element.ToString(SaveOptions.DisableFormatting)
        };

        db.Insert(newKey);
    }
}

/// <summary>
/// xml repo base
/// </summary>
public abstract class BaseXmlRepository : IXmlRepository
{
    /// <summary>
    /// services
    /// </summary>
    public IServiceProvider Services { get; set; }
    
    /// <summary>
    /// logger factory
    /// </summary>
    public ILoggerFactory LoggerFactory { get; set; }

    /// <inheritdoc />
    public abstract IReadOnlyCollection<XElement> GetAllElements();

    /// <inheritdoc />
    public abstract void StoreElement(XElement element, string friendlyName);
}

/// <summary>
/// 数据保护密钥
/// </summary>
public class DataProtectionKey
{
    /// <summary>
    /// id
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// key
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string FriendlyName { get; set; }

    /// <summary>
    /// xml密钥数据
    /// </summary>
    [SugarColumn(IsNullable = true, ColumnDataType = "text")]
    public string Xml { get; set; }
}