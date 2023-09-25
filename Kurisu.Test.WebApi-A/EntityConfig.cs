using System.Reflection;
using Kurisu.DataAccess.Functions.Default.Abstractions;

namespace Kurisu.Test.WebApi_A;

public class EntityConfig : IModelConfigurationSourceResolver
{
    public Assembly GetAssembly()
    {
        return Assembly.GetExecutingAssembly();
    }
}