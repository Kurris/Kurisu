using Kurisu.AspNetCore.ConfigurableOptions.Attributes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Kurisu.Test.WebApi_A.Services;

[Configuration]
public class AAA : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string name, JwtBearerOptions options)
    {
    }
}