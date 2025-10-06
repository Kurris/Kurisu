using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore.DynamicApi.Attributes;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Kurisu.AspNetCore.DynamicApi.Defaults;

/// <summary>
/// 默认动态api协议
/// </summary>
public class DefaultDynamicApiConvention : IApplicationModelConvention
{
    /// <summary>
    /// ctor
    /// </summary>
    public DefaultDynamicApiConvention()
    {
    }

    /// <inheritdoc />
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var dynamicApi = controller.ControllerType.GetCustomAttribute<AsApiAttribute>();
            if (dynamicApi == null) continue;

            controller.ApiExplorer.IsVisible = true;
            controller.ControllerName = controller.ControllerName.Replace("Controller", string.Empty).Replace("Service", string.Empty);

            foreach (var action in controller.Actions)
            {
                //定义接口方法才属于接口
                action.ApiExplorer.IsVisible = action.Attributes.Any(x => x.GetType().IsAssignableTo(typeof(HttpMethodAttribute)));
            }

            //action.Selectors[0].AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel();
            //BindingInfo.GetBindingInfo(new[] { new FromBodyAttribute() });
        }
    }
}