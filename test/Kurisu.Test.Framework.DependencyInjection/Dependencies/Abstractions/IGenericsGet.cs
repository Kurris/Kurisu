using Kurisu.Test.Framework.DependencyInjection.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DependencyInjection.Dependencies.Abstractions;

public interface IGenericsGet<TAnimal> where TAnimal : Animal
{
    public TAnimal Animal { get; set; }
    
    string Say();
}