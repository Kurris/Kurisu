using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Extensions;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.UnitOfWork.Attributes;
using Kurisu.UnifyResultAndValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiDemo.Dto.Output;
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

        [HttpGet("throw")]
        public void Throw()
        {
            throw new UserFriendlyException("异常不会在console显示");
        }


        /// <summary>
        /// 查询所有菜单
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("doSomething")]
        public async Task<IEnumerable<MenuOutput>> QueryMenus()
        {
            return await _dbService.Queryable<Menu>().Select<MenuOutput>().ToListAsync();
        }

        [Authorize]
        [UnitOfWork(true)]
        [HttpPost]
        public async Task AddMenu(Menu menu)
        {
            await _dbService.InsertAsync(menu);
        }

        [UnitOfWork]
        [HttpPost("returnIdentity")]
        public async Task<int> AddMenuAndReturnIdentity(Menu menu)
        {
            return await _dbService.InsertReturnIdentityAsync<int, Menu>(menu);
        }

        [Authorize]
        [UnitOfWork]
        [HttpPost("TestUseTransaction")]
        public async Task TestUseTransaction()
        {
            await _dbService.UseTransactionAsync(async () =>
            {
                var newMenu = new Menu()
                {
                    Code = "1",
                    DisplayName = "diyige caidan",
                    Icon = "icon1",
                    PCode = string.Empty,
                    Route = "route",
                    Visible = true,
                };
                await _dbService.InsertAsync(newMenu);
            });
        }
    }
}