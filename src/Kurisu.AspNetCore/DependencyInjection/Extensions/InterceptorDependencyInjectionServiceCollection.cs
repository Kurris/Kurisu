// using System;
// using System.Linq;
// using Kurisu.Aspect;
// using Kurisu.AspNetCore;
// using Kurisu.AspNetCore.DependencyInjection;
//
// // ReSharper disable once CheckNamespace
// namespace Microsoft.Extensions.DependencyInjection;
//
// /// <summary>
// /// aop拦截注入
// /// </summary>
// [SkipScan]
// public static class InterceptorDependencyInjectionServiceCollection
// {
//     /// <summary>
//     /// 添加Aop依赖注入
//     /// </summary>
//     /// <param name="services"></param>
//     /// <returns></returns>
//     public static IServiceCollection AddAop(this IServiceCollection services)
//     {
//         services.ReplaceProxyService(App.DependencyServices.Select(service =>
//         {
//             var (named, lifeTime, interfaceTypes) = DependencyInjectionHelper.GetInjectInfos(service);
//
//             return new ReplaceProxyServiceItem
//             {
//                 Lifetime = lifeTime,
//                 Service = service,
//                 InterfaceTypes = interfaceTypes,
//                 Named = named
//             };
//         }));
//
//         return services;
//     }
// }