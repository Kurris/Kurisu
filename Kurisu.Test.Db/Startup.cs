using System;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Internal;
using Kurisu.Test.Db.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Db
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            DbInjectHelper.InjectDbContext<IAppMasterDb>(services);
            DbInjectHelper.InjectDbContext<IAppSlaveDb>(services);

            //主从库操作
            services.AddScoped(typeof(IAppMasterDb), provider => provider.GetService<Func<Type, IDbService>>()?.Invoke(typeof(IAppMasterDb)));
            services.AddScoped(typeof(IAppSlaveDb), provider => provider.GetService<Func<Type, IDbService>>()?.Invoke(typeof(IAppSlaveDb)));

            //读写分离操作
            services.AddScoped<IAppDbService, AppDbService>();
        }
    }
}