using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Kurisu.AspNetCore.Document.OperationFilters;


public class AddLanguageHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        // 添加一个名为 "X-Api-Key" 的 Header
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = App.StartupOptions.LanguageHeaderName,
            In = ParameterLocation.Header,
            Description = "多语言标记",
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = "string"
            }
        });
    }
}
