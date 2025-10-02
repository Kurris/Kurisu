using System.Reflection;

namespace AspectCore.Extensions.Reflection
{
    public abstract class MemberReflector<TMemberInfo> : ICustomAttributeReflectorProvider where TMemberInfo : MemberInfo
    {
        private readonly CustomAttributeReflector[] _customAttributeReflectors;

        protected TMemberInfo _reflectionInfo;
        public virtual string Name => _reflectionInfo.Name;

        protected MemberReflector(TMemberInfo reflectionInfo)
        {
            _reflectionInfo = reflectionInfo ?? throw new ArgumentNullException(nameof(reflectionInfo));
            _customAttributeReflectors = _reflectionInfo.CustomAttributes.Select(data => CustomAttributeReflector.Create(data)).ToArray();
        }

        public override string ToString() => $"{_reflectionInfo.MemberType} : {_reflectionInfo}  DeclaringType : {_reflectionInfo.DeclaringType}";
        public TMemberInfo GetMemberInfo() => _reflectionInfo;
        public virtual string DisplayName => _reflectionInfo.Name;

        public CustomAttributeReflector[] CustomAttributeReflectors => _customAttributeReflectors;
    }
}