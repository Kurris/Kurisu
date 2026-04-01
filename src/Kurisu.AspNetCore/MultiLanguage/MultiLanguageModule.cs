using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kurisu.AspNetCore.Abstractions.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.MultiLanguage;

public class MultiLanguageModule : AppModule
{

    public override string Name => "多语言模块";

    public override int Order => -998;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddMvcFilter<EnableMultiLanguageAttribute>();

        services.AddLocalization();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supported = new[] { new CultureInfo("zh-CN"), new CultureInfo("en-US") };

            options.DefaultRequestCulture = new RequestCulture("zh-CN");
            options.SupportedCultures = supported.ToList();
            options.SupportedUICultures = supported.ToList();
            // 将自定义 header provider 放在优先位置
            options.RequestCultureProviders.Insert(0, new CustomHeaderRequestCultureProvider(App.StartupOptions.LanguageHeaderName, new Dictionary<string, string>
            {
                ["zh"] = "zh-CN",
                ["en"] = "en-US",
            }));
        });
    }

    public override void Configure(IApplicationBuilder app)
    {
        app.UseRequestLocalization();
    }
}
