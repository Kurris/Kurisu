namespace AspectCore.DynamicProxy
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public class NonAspectAttribute : Attribute
    {
    }
}
