namespace Kurisu.AspNetCore.Abstractions.DataAccess;

public enum Propagation
{
    Required,
    RequiresNew,
    Supports,
    NotSupported,
    Mandatory,
    Never
}