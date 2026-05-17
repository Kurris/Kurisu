using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.Test.Framework.DependencyInjection.Dependencies.Abstractions;
using Kurisu.Test.Framework.DependencyInjection.Models;

namespace Kurisu.Test.Framework.DependencyInjection.Dependencies;

[DiInject]
public class GenericsGet<TAnimal> : IGenericsGet<TAnimal> where TAnimal : Animal, new()
{
    public TAnimal Animal { get; set; } = new();

    public string Say()
    {
        return Animal.Say(); 
    }
}