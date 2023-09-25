using System;
using Kurisu.DataAccess.Functions.Default;
using Kurisu.DataAccess.Functions.Default.Abstractions;
using Kurisu.DataAccess.Functions.Default.Internal;
using Kurisu.DataAccess.Functions.Default.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.Db.StartupDefault;

[Trait("db", "startup.default")]
public class TestDefault
{
    private readonly IServiceProvider _serviceProvider;

    public TestDefault(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Fact]
    public void GetSlaveDb_Return_Null()
    {
        var slaveDb = _serviceProvider.GetService<IDbRead>();
        Assert.Null(slaveDb);
    }


    [Fact]
    public void GetConnectionResolver_Return_DefaultConnectionResolver()
    {
        var resolver = _serviceProvider.GetService<IDbConnectStringResolver>();

        var type = resolver.GetType();

        Assert.Equal(typeof(DefaultDbConnectStringResolver), type);
    }


    [Fact]
    public void GetConnectionString_Return_MasterDbConnectionString()
    {
        var resolver = _serviceProvider.GetService<IDbConnectStringResolver>();

        var masterDbString = resolver.GetConnectionString(null);

        var dbString = "server=isawesome.cn;port=3306;userid=root;password=root;database=demo;Charset=utf8mb4;";
        Assert.Equal(dbString, masterDbString);
    }

    [Fact]
    public void GetDbContext_Return_DefaultDbContextWithIAppMasterDb()
    {
        var masterDb = _serviceProvider.GetService<IDbWrite>();

        var type = masterDb.GetDbContext().GetType();

        Assert.Equal(typeof(DefaultAppDbContext<IDbWrite>), type);
    }


    [Fact]
    public void GetIAppMasterDb_Return_WriteImplementation()
    {
        var masterDb = _serviceProvider.GetService<IDbWrite>();

        var type = masterDb.GetType();

        Assert.Equal(typeof(WriteImplementation), type);
    }


    [Fact]
    public void GetIAppDbService_Return_DefaultAppDbService()
    {
        var dbService = _serviceProvider.GetService<IDbService>();

        var type = dbService.GetType();

        Assert.Equal(typeof(DefaultAppDbService), type);
    }
}