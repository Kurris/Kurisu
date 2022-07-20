using System.Collections.Generic;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.UnitOfWork.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiDemo.Entities;

namespace WebApiDemo.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DbContextController : ControllerBase
    {
        private readonly ISlaveDb _slaveDb;
        private readonly IMasterDb _masterDb;

        public DbContextController(ISlaveDb slaveDb
            , IMasterDb masterDb)
        {
            _slaveDb = slaveDb;
            _masterDb = masterDb;
        }


        /// <summary>
        /// 查询所有菜单
        /// </summary>
        /// <returns></returns>
        [HttpGet("doSomething")]
        public async Task<IEnumerable<Menu>> QueryMenus()
        {
            return await _slaveDb.Queryable<Menu>().ToListAsync();
        }

        [UnitOfWork(true)]
        [HttpPost]
        public async Task AddMenu(Menu menu)
        {
            await _masterDb.AddAsync(menu);
        }
    }
}