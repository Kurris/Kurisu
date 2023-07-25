using System;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.Scope;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.DI;

[Trait("di", "manualScoped")]
public class TestScoped
{
    [Fact]
    public async Task CreateScope_ReturnUnDisposedObject()
    {
        var service = Scoped.Request.Value.Create(provider => provider.GetService<IAppDbService>());
        var trans = await service.BeginTransactionAsync();
        Assert.NotNull(trans);

        App.DisposeObjects();
    }

    [Fact]
    public async Task CreateScope_ReturnDisposedObject()
    {
        var service = Scoped.Temp.Value.Create(provider => provider.GetService<IAppDbService>());

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await service.BeginTransactionAsync());
    }
}