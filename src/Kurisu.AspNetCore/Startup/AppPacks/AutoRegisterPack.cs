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
    /// <inheritdoc/>
    public override int Order => 99;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        var referencedAssemblies = Assembly.GetEntryAssembly()?.GetReferencedAssemblies() ?? Array.Empty<AssemblyName>();
        referencedAssemblies = referencedAssemblies.Where(x => x.Name!.EndsWith(".Api", StringComparison.InvariantCultureIgnoreCase)).ToArray();
        var packages = new List<string> { "Kurisu.RemoteCall" };

        const string extensionsType = "DependencyInjectionExtensions";
        const string inject = "Inject";

        foreach (var referencedAssembly in referencedAssemblies)
        {
            var currentAssemblyRefs = Assembly.Load(referencedAssembly).GetReferencedAssemblies();
            foreach (var assembly in packages.Select(package => currentAssemblyRefs.FirstOrDefault(x => x.Name == package)))
            {
                if (assembly == null)
                {
                    continue;
                }

                var injector = Assembly.Load(assembly).GetTypes().FirstOrDefault(x => x.Name == extensionsType)!;
                var method = injector.GetMethod(inject)!;
                method.Invoke(null, [services, App.ActiveTypes]);
            }
        }
    }
}