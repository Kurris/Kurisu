using System;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.Default.Resolvers;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
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
        var slaveDb = _serviceProvider.GetService<IAppSlaveDb>();
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
        var masterDb = _serviceProvider.GetService<IAppMasterDb>();

        var type = masterDb.GetMasterDbContext().GetType();

        Assert.Equal(typeof(DefaultAppDbContext<IAppMasterDb>), type);
    }


    [Fact]
    public void GetIAppMasterDb_Return_WriteImplementation()
    {
        var masterDb = _serviceProvider.GetService<IAppMasterDb>();

        var type = masterDb.GetType();

        Assert.Equal(typeof(WriteImplementation), type);
    }
        

    [Fact]
    public void GetIAppDbService_Return_DefaultAppDbService()
    {
        var dbService = _serviceProvider.GetService<IAppDbService>();

        var type = dbService.GetType();

        Assert.Equal(typeof(DefaultAppDbService), type);
    }
}