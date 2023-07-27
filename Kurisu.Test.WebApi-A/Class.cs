using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kurisu.Test.WebApi_A
{
    public class Class : IDesignTimeDbContextFactory<DefaultAppDbContext>
    {
        public DefaultAppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DefaultAppDbContext>();
            //optionsBuilder.UseMySql();
            
            throw new NotImplementedException();
        }
    }
}
