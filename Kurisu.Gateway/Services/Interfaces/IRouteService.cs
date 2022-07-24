using System.Collections.Generic;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Dto;
using Kurisu.Gateway.Dto.Input;
using Kurisu.Gateway.Dto.Output;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Gateway.Services.Interfaces
{
    public interface IRouteService : ISingletonDependency
    {
        /// <summary>
        /// 获取项目分页列表
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<Pagination<GetProjectPageListOutput>> GetProjectPageList(GetProjectPageListInput input);

        /// <summary>
        /// 获取项目信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<GetProjectOutput> GetProject(int id);

        /// <summary>
        /// 设置项目启用/禁用
        /// </summary>
        /// <param name="id"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        Task SetProjectEnabled(int id, bool enable);


        /// <summary>
        /// 保存项目
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task SaveProject(SaveProjectInput input);


        /// <summary>
        /// 保存路由设置
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task SaveRoute(SaveRouteInput input);

        /// <summary>
        /// 获取路由信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<GetRoutesOutput> GetRoutes(int id);

        /// <summary>
        /// 获取项目路由分页列表
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<Pagination<GetProjectRoutesPageListOutput>> GetProjectRoutesPageList(GetProjectRoutesPageListInput input);

        /// <summary>
        /// 获取所有路由
        /// </summary>
        /// <returns></returns>
        Task<List<GetAllRoutesOutput>> GetAllRoutes();
    }
}
