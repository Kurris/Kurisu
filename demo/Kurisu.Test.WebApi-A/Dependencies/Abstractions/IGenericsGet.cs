using Kurisu.Test.Framework.DI.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DI.Dependencies.Abstractions;

public interface IGenericsGet<TAnimal> where TAnimal : Animal
{
    public TAnimal Animal { get; set; }
    
    string Say();
}

// public interface IGenericsGet<TAnimal>  where TAnimal : Animal
// {
//     public TAnimal Animal { get; set; }
//     
//     string Say();
// }