namespace Kurisu.Aspect.Reflection.Reflectors;

internal interface IParameterReflectorProvider
{
    ParameterReflector[] ParameterReflectors { get; }
}