using Kurisu.Test.Framework.DI.Dtos;

namespace Kurisu.Test.Framework.DI.Dependencies.Abstractions
{
    public interface IGenericsGet<TAnimal> where TAnimal : Animal
    {
        public TAnimal Animal { get; set; }
        string Say();
    }
}