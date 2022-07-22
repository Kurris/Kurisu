using System.Collections.Generic;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.UnitOfWork.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiDemo.Entities;

namespace WebApiDemo.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class DbContextController : ControllerBase
    {
        private readonly IAppDbService _dbService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbService"></param>
        public DbContextController(IAppDbService dbService)
        {
            _dbService = dbService;
        }


        /// <summary>
        /// 查询所有菜单
        /// </summary>
        /// <returns></returns>
        [HttpGet("doSomething")]
        public async Task<IEnumerable<Menu>> QueryMenus()
        {
            await _dbService.Queryable<Menu>().ToListAsync();
            await _dbService.Queryable<Menu>(true).ToListAsync();

            await _dbService.FirstOrDefaultAsync<Menu>();
            return await _dbService.ToListAsync<Menu>();
        }

        [UnitOfWork(true)]
        [HttpPost]
        public async Task AddMenu(Menu menu)
        {
            await _dbService.InsertAsync(menu);
        }
    }
}