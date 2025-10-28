// filepath: e:\github\Kurisu\src\Kurisu.RemoteCall\Utils\ICommonUtils.cs

namespace Kurisu.RemoteCall.Utils;

internal interface ICommonUtils
{
    bool IsReferenceType(Type type);
    Dictionary<string, object> ToObjDictionary(string prefix, object obj);
}