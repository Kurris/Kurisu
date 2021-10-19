using System;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DependencyInjection.Abstractions;
using TestApi.Entities;

namespace TestApi.Services
{
    public class AService : ITransient
    {
        private readonly IMasterDbImplementation _db;

        public AService(IMasterDbImplementation db)
        {
            _db = db;
        }

        public async Task Delete()
        {
            await Task.CompletedTask;
        }
    }
}