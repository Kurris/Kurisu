using System.Reflection;

namespace AspectCore.Extensions.Reflection
{
    public partial class ParameterReflector : ICustomAttributeReflectorProvider
    {
        private readonly ParameterInfo _reflectionInfo;
        private readonly CustomAttributeReflector[] _customAttributeReflectors;

        public CustomAttributeReflector[] CustomAttributeReflectors => _customAttributeReflectors;

        public string Name => _reflectionInfo.Name;

        public bool HasDeflautValue { get; }

        public object DefalutValue { get; }

        public int Position { get; }

        public Type ParameterType { get; }

        private ParameterReflector(ParameterInfo reflectionInfo)
        {
            _reflectionInfo = reflectionInfo ?? throw new ArgumentNullException(nameof(reflectionInfo));

            if (!reflectionInfo.ParameterType.IsTupleType())
            {
                _customAttributeReflectors = _reflectionInfo.CustomAttributes.Select(data => CustomAttributeReflector.Create(data)).ToArray();
            }

            HasDeflautValue = reflectionInfo.HasDefaultValueByAttributes();
            if (HasDeflautValue)
            {
                DefalutValue = reflectionInfo.DefaultValueSafely();
            }

            Position = reflectionInfo.Position;
            ParameterType = reflectionInfo.ParameterType;
        }

        public ParameterInfo GetParameterInfo()
        {
            return _reflectionInfo;
        }

        public override string ToString() => $"Parameter : {_reflectionInfo}  ParameterType : {ParameterType}";
        
        internal static ParameterReflector Create(ParameterInfo parameterInfo)
        {
            if (parameterInfo == null)
            {
                throw new ArgumentNullException(nameof(parameterInfo));
            }
            return ReflectorCacheUtils<ParameterInfo, ParameterReflector>.GetOrAdd(parameterInfo, info => new ParameterReflector(info));
        }
    }
}