using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Kurisu.AspNetCore.Scope;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

[Trait("di", "manualScoped")]
public class TestScoped
{
    // [Fact]
    // public void CreateScope_ReturnUnDisposedObject()
    // {
    // }

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