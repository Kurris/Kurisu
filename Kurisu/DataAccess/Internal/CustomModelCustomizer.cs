using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kurisu.DataAccess.Internal;

public class CustomModelCustomizer : ModelCustomizer
{
    public CustomModelCustomizer(ModelCustomizerDependencies dependencies) : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
    }
}