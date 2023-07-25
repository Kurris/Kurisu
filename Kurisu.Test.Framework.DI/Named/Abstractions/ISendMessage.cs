using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DI.Named.Abstractions;

public interface ISendMessage : ISingletonDependency
{
    string Send();
}