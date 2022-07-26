using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Kurisu.Gateway
{
    public class OcelotApiDescriptionProvider : IApiDescriptionProvider
    {
        private readonly DefaultApiDescriptionProvider _descriptionProvider;

        public OcelotApiDescriptionProvider(DefaultApiDescriptionProvider descriptionProvider)
        {
            _descriptionProvider = descriptionProvider;
        }

        public void OnProvidersExecuting(ApiDescriptionProviderContext context)
        {
            _descriptionProvider.OnProvidersExecuting(context);

            //移除ocelot公开的接口
            var needRemoves = new List<ApiDescription>();
            foreach (var result in context.Results)
            {
                var controller = result.ActionDescriptor as ControllerActionDescriptor;
                if (controller.ControllerName.Equals("FileConfiguration", StringComparison.OrdinalIgnoreCase)
                    || controller.ControllerName.Equals("OutputCache", StringComparison.OrdinalIgnoreCase))
                {
                    needRemoves.Add(result);
                }
            }

            needRemoves.ForEach(x => context.Results.Remove(x));
        }

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
            _descriptionProvider.OnProvidersExecuted(context);
        }

        public int Order => _descriptionProvider.Order;
    }
}