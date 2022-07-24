using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Dto;
using Kurisu.DataAccessor.Extensions;
using Kurisu.Gateway.Dto.Input;
using Kurisu.Gateway.Dto.Output;
using Kurisu.Gateway.Entities;
using Kurisu.Gateway.Services.Interfaces;
using Kurisu.Utils.Extensions;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Route = Kurisu.Gateway.Entities.Route;

namespace Kurisu.Gateway.Services.Internals
{
    public class RouteService : IRouteService
    {
        private readonly IServiceProvider _serviceProvider;

        public RouteService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public async Task<Pagination<GetProjectPageListOutput>> GetProjectPageList(GetProjectPageListInput input)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetService<IAppDbService>();

            return await dbService.Queryable<Project>()
                  .WhereIf(input.Enable.HasValue, x => x.Enable == input.Enable.Value)
                  .WhereIf(input.Name.IsNullOrEmpty(), x => x.Name.Contains(input.Name))
                  .ProjectToType<GetProjectPageListOutput>()
                  .ToPageAsync(input);
        }

        public async Task<GetProjectOutput> GetProject(int id)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetService<IAppDbService>();

            var project = await dbService.Queryable<Project>()
                .Select<GetProjectOutput>()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            return project;
        }

        public async Task SetProjectEnabled(int id, bool enable)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetService<IAppDbService>();

            var project = await dbService.Queryable<Project>(true)
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (project != null)
            {
                project.Enable = enable;
            }
        }

        public async Task SaveProject(SaveProjectInput input)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetService<IAppDbService>();

            var project = input.Adapt<Project>();
            await dbService.SaveAsync(project);
        }

        public async Task SaveRoute(SaveRouteInput input)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbService = scope.ServiceProvider.GetService<IAppDbService>();
                using (var trans = await dbService.GetMasterDbContext().Database.BeginTransactionAsync(default))
                {
                    try
                    {
                        var route = await dbService.Where<Route>(x => x.Id == input.Id, true).FirstOrDefaultAsync();
                        var id = 0;

                        if (route == null)
                        {
                            route = input.Adapt<Route>();
                            route.UpstreamHttpMethod = input.UpstreamHttpMethodList == null ? "" : string.Join(",", input.UpstreamHttpMethodList);

                            id = await dbService.InsertReturnIdentityAsync<int, Route>(route);
                        }
                        else
                        {
                            id = route.Id;
                            route = input.Adapt(route);
                            route.UpstreamHttpMethod = input.UpstreamHttpMethodList == null ? "" : string.Join(",", input.UpstreamHttpMethodList);

                            await dbService.UpdateAsync(route, true);
                        }

                        await SaveHostPort(dbService, id, input.HostPort);

                        await dbService.SaveChangesAsync();
                        await trans.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await trans.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<GetRoutesOutput> GetRoutes(int id)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetService<IAppDbService>();

            var result = await dbService.Queryable<Route>().Select<GetRoutesOutput>().FirstOrDefaultAsync(route => route.Id == id);
            if (result == null) return new GetRoutesOutput() { };

            result.HostPort = await dbService.Where<RouteHostPort>(n => n.RouteId == result.Id).Select<GetHostPortOutput>().ToListAsync();
            return result;
        }

        public async Task<Pagination<GetProjectRoutesPageListOutput>> GetProjectRoutesPageList(GetProjectRoutesPageListInput input)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetService<IAppDbService>();

            return await dbService.Where<Route>(x => x.ProjectId == input.ProjectId)
                   .WhereIf(input.Enable.HasValue, n => n.Enable == input.Enable.Value)
                   .WhereIf(!input.Keyword.IsNullOrEmpty(), n => n.DownstreamPathTemplate.Contains(input.Keyword) || n.UpstreamPathTemplate.Contains(input.Keyword))
                   .Select<GetProjectRoutesPageListOutput>()
                   .ToPageAsync(input);
        }

        public async Task<List<GetAllRoutesOutput>> GetAllRoutes()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetService<IAppDbService>();

            var projects = await dbService.Where<Project>(x => x.Enable).ToListAsync();
            var projectIds = projects.Select(n => n.Id).ToList();

            var routes = await dbService
                .Where<Route>(x => x.Enable)
                .Where(x => projectIds.Contains(x.ProjectId))
                .Select<GetAllRoutesOutput>().ToListAsync();

            if (!routes.Any())
            {
                return routes;
            }

            var routesIds = routes.Select(n => n.Id).ToList();
            var hostPorts = await dbService.Where<RouteHostPort>(n => routesIds.Contains(n.RouteId)).Select<GetHostPortOutput>().ToListAsync();


            foreach (var item in routes)
            {
                item.HostPort = hostPorts?.Where(n => n.RouteId == item.Id).ToList();
            }
            return routes;
        }



        private async Task SaveHostPort(IAppDbService dbService, int routeId, List<SaveHostPortInput> list)
        {
            var routeHostPorts = await dbService.Queryable<RouteHostPort>(true)
                      .Where(x => x.RouteId == routeId)
                      .ToListAsync();

            if (routeHostPorts.Any())
            {
                await dbService.DeleteRangeAsync(routeHostPorts);
            }


            if (list == null || list.Count == 0) return;

            var newRouteHostPorts = list.Select(x => new RouteHostPort()
            {
                Host = x.Host,
                Port = x.Port,
                RouteId = routeId
            });

            await dbService.InsertRangeAsync(newRouteHostPorts);
        }
    }
}
