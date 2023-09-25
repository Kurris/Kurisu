using System;
using System.Collections.Generic;
using Kurisu.DataAccess.Functions.Default;
using Kurisu.DataAccess.Functions.Default.Abstractions;
using Kurisu.DataAccess.Functions.Default.Internal;
using Kurisu.DataAccess.Functions.ReadWriteSplit.Internal;
using Kurisu.DataAccess.Functions.ReadWriteSplit.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.Db.StartupReadWriteSplit;

[Trait("db", "startup.readwritesplit")]
public class TestReadWriteSplit
{
    private readonly IServiceProvider _serviceProvider;

    public TestReadWriteSplit(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Fact]
    public void GetSlaveDb_Return_NotNull()
    {
        var slaveDb = _serviceProvider.GetService<IDbRead>();
        Assert.NotNull(slaveDb);
    }


    [Fact]
    public void GetConnectionResolver_Return_ReadWriteConnectionResolver()
    {
        var resolver = _serviceProvider.GetService<IDbConnectStringResolver>();

        var type = resolver.GetType();

        Assert.Equal(typeof(DefaultReadWriteDbConnectStringResolver), type);
    }


    [Fact]
    public void GetConnectionString_Return_MatchDbConnectionString()
    {
        var resolver = _serviceProvider.GetService<IDbConnectStringResolver>();

        var masterDbString = resolver.GetConnectionString(typeof(DefaultAppDbContext<IDbWrite>));
        var dbString = "server=isawesome.cn;port=3306;userid=root;password=root;database=demo;Charset=utf8mb4;";
        Assert.Equal(dbString, masterDbString);

        var slaveDbString = resolver.GetConnectionString(typeof(DefaultAppDbContext<IDbRead>));
        Assert.Contains(slaveDbString, new List<string>
        {
            "server=isawesome.cn;port=32001;userid=root;password=zxc111;database=demo;Charset=utf8mb4;"
        });
    }

    [Fact]
    public void GetDbContext_Return_MatchDbContext()
    {
        var masterDb = _serviceProvider.GetService<IDbWrite>();

        var masterType = masterDb.GetDbContext().GetType();
        Assert.Equal(typeof(DefaultAppDbContext<IDbWrite>), masterType);

        var slaveDb = _serviceProvider.GetService<IDbRead>();

        var slaveType = slaveDb.GetDbContext().GetType();
        Assert.Equal(typeof(DefaultAppDbContext<IDbRead>), slaveType);
    }


    [Fact]
    public void GetIAppXXXDb_Return_MatchImplementation()
    {
        var masterDb = _serviceProvider.GetService<IDbWrite>();

        var masterType = masterDb.GetType();
        Assert.Equal(typeof(WriteImplementation), masterType);

        var slaveDb = _serviceProvider.GetService<IDbRead>();

        var slaveDbType = slaveDb.GetType();
        Assert.Equal(typeof(ReadImplementation), slaveDbType);
    }

    [Fact]
    public void GetIAppDbService_Return_ReadWriteAppDbService()
    {
        var dbService = _serviceProvider.GetService<IDbService>();

        var type = dbService.GetType();

        Assert.Equal(typeof(ReadWriteSplitAppDbService), type);
    }
}