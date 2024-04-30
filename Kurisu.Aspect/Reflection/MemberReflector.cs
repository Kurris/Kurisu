using System.Reflection;

namespace Kurisu.Aspect.Reflection;

/// <summary>
/// 成员反射器
/// </summary>
/// <typeparam name="TMemberInfo"></typeparam>
internal abstract class MemberReflector<TMemberInfo> : ICustomAttributeReflectorProvider where TMemberInfo : MemberInfo
{
    protected TMemberInfo Current { get; }

    public CustomAttributeReflector[] CustomAttributeReflectors { get; }

    protected MemberReflector(TMemberInfo reflectionInfo)
    {
        Current = reflectionInfo ?? throw new ArgumentNullException(nameof(reflectionInfo));
        CustomAttributeReflectors = Current.CustomAttributes.Select(CustomAttributeReflector.Create).ToArray();
    }

    public TMemberInfo GetMemberInfo() => Current;

    public virtual string Name => Current.Name;

    public virtual string DisplayName => Current.Name;


    public override string ToString() => $"{Current.MemberType} : {Current}  DeclaringType : {Current.DeclaringType}";
}

internal interface ICustomAttributeReflectorProvider
{
    CustomAttributeReflector[] CustomAttributeReflectors { get; }
}