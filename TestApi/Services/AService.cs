using System;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DependencyInjection.Abstractions;
using TestApi.Entities;

namespace TestApi.Services
{
    public class AService : ITransient
    {
        private readonly IMasterDbService _db;

        public AService(IMasterDbService db)
        {
            _db = db;
        }

        public async Task Delete(Guid guid)
        {
            await _db.DeleteAsync<User>(guid);
            await Task.CompletedTask;
        }
    }
}