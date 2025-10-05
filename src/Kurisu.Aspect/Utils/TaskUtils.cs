namespace AspectCore.Utils;

internal static class TaskUtils
{
    internal static readonly Task CompletedTask = Task.CompletedTask;
}

internal static class TaskUtils<T>
{
    internal static readonly Task<T> CompletedTask = Task.FromResult(default(T));
}