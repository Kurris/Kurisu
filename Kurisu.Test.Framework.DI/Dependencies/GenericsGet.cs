using Kurisu.Test.Framework.DI.Dependencies.Abstractions;
using Kurisu.Test.Framework.DI.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DI.Dependencies;

public class GenericsGet<TAnimal> : IGenericsGet<TAnimal>, IScopeDependency where TAnimal : Animal, new()
{
    public TAnimal Animal { get; set; } = new();

    public string Say()
    {
        return Animal.Say();
    }
}