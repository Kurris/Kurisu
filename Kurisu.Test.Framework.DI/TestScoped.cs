using System.Threading.Tasks;
using Xunit;

namespace Kurisu.Test.Framework.DI;

[Trait("di", "manualScoped")]
public class TestScoped
{
    [Fact]
    public async Task CreateScope_ReturnUnDisposedObject()
    {
        // var service = Scoped.Request.Value.Create(provider => provider.GetService<IDbService>());
        // var trans = await service.BeginTransactionAsync();
        // Assert.NotNull(trans);
        //
        // App.DisposeObjects();
    }

    [Fact]
    public async Task CreateScope_ReturnDisposedObject()
    {
        // var service = Scoped.Temp.Value.Create(provider => provider.GetService<IDbService>());
        //
        // await Assert.ThrowsAsync<ObjectDisposedException>(async () => await service.BeginTransactionAsync());
    }
}