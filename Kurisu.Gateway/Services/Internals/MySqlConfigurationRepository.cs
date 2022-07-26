using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.Gateway.Services.Interfaces;
using Mapster;
using Microsoft.Extensions.Options;
using Ocelot.Cache;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Kurisu.Gateway.Services.Internals
{
    /// <summary>
    /// ocelot mysql配置源
    /// </summary>
    public class MySqlConfigurationRepository : IFileConfigurationRepository
    {
        private readonly IOcelotCache<FileConfiguration> _cache;
        private readonly IGlobalConfigurationService _globalConfigurationService;
        private readonly IRouteService _routeService;
        private readonly GatewaySetting _gatewaySetting;

        public MySqlConfigurationRepository(IOcelotCache<FileConfiguration> cache
            , IOptions<GatewaySetting> options
            , IGlobalConfigurationService globalConfigurationService
            , IRouteService routeService)
        {
            _cache = cache;
            _globalConfigurationService = globalConfigurationService;
            _routeService = routeService;
            _gatewaySetting = options.Value;
        }


        public async Task<Response<FileConfiguration>> Get()
        {
            var config = _cache.Get(_gatewaySetting.CachePrefix + "Configuration", "ocelot");

            if (config != null) return new OkResponse<FileConfiguration>(config);

            var globalConfiguration = await _globalConfigurationService.GetGlobalConfiguration();
            var routes = await _routeService.GetAllRoutes();

            var global = globalConfiguration.Adapt<FileGlobalConfiguration>();

            var file = new FileConfiguration
            {
                GlobalConfiguration = global
            };

            if (routes.Any())
            {
                var routeSettings = new List<FileRoute>();
                foreach (var item in routes)
                {
                    var routeSetting = new FileRoute
                    {
                        DownstreamPathTemplate = item.DownstreamPathTemplate,
                        DownstreamScheme = item.DownstreamScheme,
                        DownstreamHostAndPorts = item.HostPort.Adapt<List<FileHostAndPort>>(),
                        UpstreamHttpMethod = item.UpstreamHttpMethodList,
                        UpstreamPathTemplate = item.UpstreamPathTemplate,
                        Priority = item.Priority,
                        Timeout = item.Timeout
                    };

                    routeSettings.Add(routeSetting);
                }

                file.Routes = routeSettings;
            }

            return new OkResponse<FileConfiguration>(file);
        }

        public async Task<Response> Set(FileConfiguration fileConfiguration)
        {
            _cache.AddAndDelete(_gatewaySetting.CachePrefix + "Configuration", fileConfiguration, TimeSpan.FromSeconds(3600), "ocelot");
            return await Task.FromResult((Response) new OkResponse());
        }
    }
}