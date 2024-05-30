using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Startup.AppPacks;

/// <summary>
/// 自动注入kurisu相关引用
/// </summary>
public class AutoRegisterPack : BaseAppPack
{
    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        var referencedAssemblies = Assembly.GetEntryAssembly()?.GetReferencedAssemblies() ?? Array.Empty<AssemblyName>();
        var packages = new List<string> { "Kurisu.RemoteCall", "Kurisu.Aspect" };
        const string extensionsType = "DependencyInjectionExtensions";
        const string inject = "Inject";

        foreach (var assembly in packages.Select(package => referencedAssemblies.FirstOrDefault(x => x.Name == package)))
        {
            if (assembly == null)
            {
                continue;
            }

            var injector = Assembly.Load(assembly).GetTypes().FirstOrDefault(x => x.Name == extensionsType)!;
            var method = injector.GetMethod(inject)!;
            method?.Invoke(null, new object[] { services });
        }
    }
}