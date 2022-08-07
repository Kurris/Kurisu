using System;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
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
            services.AddScoped(typeof(IAppMasterDb), provider => provider.GetService<Func<Type, IBaseDbService>>()?.Invoke(typeof(IAppMasterDb)));
            services.AddScoped(typeof(IAppSlaveDb), provider => provider.GetService<Func<Type, IBaseDbService>>()?.Invoke(typeof(IAppSlaveDb)));

            //读写分离操作
            services.AddScoped<IAppDbService, DefaultAppDbService>();
        }
    }
}