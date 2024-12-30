using System.Reflection;
using Kurisu.Aspect.Reflection.Extensions;

namespace Kurisu.Aspect.Reflection.Reflectors;

internal class ParameterReflector : ICustomAttributeReflectorProvider
{
    private readonly ParameterInfo _current;

    public CustomAttributeReflector[] CustomAttributeReflectors { get; }

    public string Name => _current.Name;

    public bool HasDefaultValue { get; }

    public object DefaultValue { get; }

    public int Position { get; }

    public Type ParameterType { get; }

    public ParameterReflector(ParameterInfo reflectionInfo)
    {
        _current = reflectionInfo ?? throw new ArgumentNullException(nameof(reflectionInfo));

        if (!reflectionInfo.ParameterType.IsTupleType())
        {
            CustomAttributeReflectors = _current.CustomAttributes.Select(CustomAttributeReflector.Create).ToArray();
        }

        HasDefaultValue = reflectionInfo.HasDefaultValueByAttributes();
        if (HasDefaultValue)
        {
            DefaultValue = reflectionInfo.DefaultValueSafely();
        }

        Position = reflectionInfo.Position;
        ParameterType = reflectionInfo.ParameterType;
    }

    public ParameterInfo GetParameterInfo()
    {
        return _current;
    }

    public override string ToString() => $"Parameter : {_current}  ParameterType : {ParameterType}";
}