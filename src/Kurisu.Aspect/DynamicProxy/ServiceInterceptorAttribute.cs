// using System.Reflection;
//
// namespace Kurisu.Aspect.DynamicProxy;
//
// public sealed class ServiceInterceptorAttribute : AbstractInterceptorAttribute, IEquatable<ServiceInterceptorAttribute>
// {
//     public Type InterceptorType { get; }
//
//     public ServiceInterceptorAttribute(Type interceptorType)
//     {
//         if (interceptorType == null) throw new ArgumentNullException(nameof(interceptorType));
//
//         if (!typeof(IInterceptor).GetTypeInfo().IsAssignableFrom(interceptorType.GetTypeInfo()))
//         {
//             throw new ArgumentException($"{interceptorType} is not an interceptor.", nameof(interceptorType));
//         }
//
//         InterceptorType = interceptorType;
//     }
//
//     public override Task Invoke(AspectContext context, AspectDelegate next)
//     {
//         if (context.ServiceProvider.GetService(InterceptorType) is not IInterceptor instance)
//         {
//             throw new InvalidOperationException($"Cannot resolve type  '{InterceptorType}' of service interceptor.");
//         }
//
//         return instance.Invoke(context, next);
//     }
//
//     public bool Equals(ServiceInterceptorAttribute other)
//     {
//         if (other == null) return false;
//
//         return InterceptorType == other.InterceptorType;
//     }
//
//     public override bool Equals(object obj)
//     {
//         var other = obj as ServiceInterceptorAttribute;
//         return Equals(other);
//     }
//
//     public override int GetHashCode()
//     {
//         return InterceptorType.GetHashCode();
//     }
// }