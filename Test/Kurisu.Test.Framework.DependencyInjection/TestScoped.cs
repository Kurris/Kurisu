using System;
using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Scope;
using Kurisu.Extensions.SqlSugar.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

[Trait("di", "manualScoped")]
public class TestScoped
{
    private readonly IServiceProvider _serviceProvider;

    public TestScoped()
    {
        _serviceProvider = TestHelper.GetServiceProvider();
    }

    [Fact]
    public void CreateScope_ReturnDisposedObject()
    {
        var (a, b) = Scoped.Temp.Value.Invoke(provider =>
        {
            var dba = provider.GetService<IDbContext>();
            var dbb = provider.GetService<IDbContext>();

            return (dba, dbb);
        });

        Assert.Equal(a, b);

        var dbO = Scoped.Temp.Value.Invoke(provider =>
        {
            var db = provider.GetService<IDbContext>();
            Assert.NotNull(db);
            return db;
        });

        var db1 = Scoped.Temp.Value.Invoke(provider =>
        {
            var db = provider.GetService<IDbContext>();
            Assert.NotNull(db);
            return db;
        });

        Assert.NotEqual(dbO, db1);
    }
}