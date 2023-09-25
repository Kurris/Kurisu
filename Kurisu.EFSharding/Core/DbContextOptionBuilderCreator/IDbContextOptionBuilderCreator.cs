using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Core.DbContextOptionBuilderCreator;

public interface IDbContextOptionBuilderCreator
{
    DbContextOptionsBuilder CreateDbContextOptionBuilder();
}