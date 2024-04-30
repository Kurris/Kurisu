namespace Kurisu.Aspect.Reflection;

internal interface IParameterReflectorProvider
{
    ParameterReflector[] ParameterReflectors { get; }
}