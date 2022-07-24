using System;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.Gateway.Dto.Input;
using Kurisu.Gateway.Dto.Output;
using Kurisu.Gateway.Services.Interfaces;
using Manon.Gateway.ApplicationCore.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Gateway.Services.Internals
{
    public class GlobalConfigurationService : IGlobalConfigurationService
    {
        private readonly IServiceProvider _serviceProvider;

        public GlobalConfigurationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task SaveGlobalConfiguration(SaveGlobalConfigurationInput input)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetService<IAppDbService>();

            var globalConfiguration = input.Adapt<GlobalConfiguration>();
            await dbService.SaveAsync(globalConfiguration);
        }

        public async Task<GetGlobalConfigurationOutput> GetGlobalConfiguration()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetService<IAppDbService>();

            var output = await dbService.Queryable<GlobalConfiguration>()
             .ProjectToType<GetGlobalConfigurationOutput>()
             .FirstOrDefaultAsync();

            return output ?? new GetGlobalConfigurationOutput();
        }
    }
}
