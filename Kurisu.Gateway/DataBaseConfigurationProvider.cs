using System;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.Repository;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Kurisu.Gateway
{
    public class DataBaseConfigurationProvider
    {
        public static OcelotMiddlewareConfigurationDelegate Get = async app =>
        {
            var fileConfigRepo = app.ApplicationServices.GetService(typeof(IFileConfigurationRepository)) as IFileConfigurationRepository;
            var internalConfigCreator = app.ApplicationServices.GetService(typeof(IInternalConfigurationCreator)) as IInternalConfigurationCreator;
            var internalConfigRepo = app.ApplicationServices.GetService(typeof(IInternalConfigurationRepository)) as IInternalConfigurationRepository;
            await SetFileConfigInDataBase(fileConfigRepo, internalConfigCreator, internalConfigRepo);
        };

        private static async Task SetFileConfigInDataBase(
            IFileConfigurationRepository fileConfigRepo,
            IInternalConfigurationCreator internalConfigCreator,
            IInternalConfigurationRepository internalConfigRepo)
        {

            //从数据库获取配置
            var fileConfigFromDataBase = await fileConfigRepo.Get();

            if (IsError(fileConfigFromDataBase))
            {
                ThrowToStopOcelotStarting(fileConfigFromDataBase);
            }
            else
            {
                await fileConfigRepo.Set(fileConfigFromDataBase.Data);
                var internalConfig = await internalConfigCreator.Create(fileConfigFromDataBase.Data);

                if (IsError(internalConfig))
                {
                    ThrowToStopOcelotStarting(internalConfig);
                }
                else
                {
                    var response = internalConfigRepo.AddOrReplace(internalConfig.Data);

                    if (IsError(response))
                    {
                        ThrowToStopOcelotStarting(response);
                    }
                }

                if (IsError(internalConfig))
                {
                    ThrowToStopOcelotStarting(internalConfig);
                }
            }
        }

        private static void ThrowToStopOcelotStarting(Response config)
        {
            throw new Exception($"Unable to start Ocelot, errors are: {string.Join(",", config.Errors.Select(x => x.ToString()))}");
        }

        private static bool IsError(Response response)
        {
            return response == null || response.IsError;
        }
    }
}
