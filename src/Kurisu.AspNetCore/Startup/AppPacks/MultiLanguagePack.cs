// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Kurisu.AspNetCore.Startup.AppPacks;
//
// public class MultiLanguagePack : BaseAppPack
// {
//     public override int Order => -999;
//
//     public override void ConfigureServices(IServiceCollection services)
//     {
//     }
//
//     public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
//     {
//         var supportedCultures = new[] { "en-US", "zh-CN" };
//         var localizationOptions = new RequestLocalizationOptions()
//             .AddSupportedCultures(supportedCultures)
//             .AddSupportedUICultures(supportedCultures)
//             .SetDefaultCulture("zh-CN"); // 设置默认文化
//
//         // 将自定义 Header 提供者添加到提供者列表的最前面（最高优先级）
//         localizationOptions.RequestCultureProviders.Insert(0, new CustomHeaderRequestCultureProvider(App.StartupOptions.MultiLanguageKey));
//         
//         // localizationOptions.RequestCultureProviders.Insert(1, new QueryStringRequestCultureProvider());
//         // localizationOptions.RequestCultureProviders.Insert(2, new CookieRequestCultureProvider());
//         // localizationOptions.RequestCultureProviders.Insert(3, new AcceptLanguageHeaderRequestCultureProvider());
//
//         app.UseRequestLocalization(localizationOptions);
//     }
// }