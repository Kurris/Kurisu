using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

internal sealed class TransactionCallbackRegistry(ILogger<TransactionCallbackRegistry> logger) : ITransactionCallbackRegistry
{
    private readonly Stack<TransactionCallbackFrame> _frames = new();

    public async Task RegisterAfterCommitAsync(Func<Task> callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        if (_frames.Count == 0)
        {
            await ExecuteCallbackAsync(callback);
            return;
        }

        _frames.Peek().AfterCommitCallbacks.Add(callback);
    }

    internal void BeginRoot()
    {
        _frames.Push(new TransactionCallbackFrame());
    }

    internal void BeginNested()
    {
        _frames.Push(new TransactionCallbackFrame());
    }

    internal async Task CommitRootAsync()
    {
        var frame = PopFrame();
        await ExecuteCallbacksAsync(frame.AfterCommitCallbacks);
    }

    internal void RollbackRoot()
    {
        PopFrame();
    }

    internal void CommitNested()
    {
        var frame = PopFrame();
        if (_frames.Count == 0)
        {
            _frames.Push(frame);
            return;
        }

        _frames.Peek().AfterCommitCallbacks.AddRange(frame.AfterCommitCallbacks);
    }

    internal void RollbackNested()
    {
        PopFrame();
    }

    private TransactionCallbackFrame PopFrame()
    {
        return _frames.Count == 0
            ? new TransactionCallbackFrame()
            : _frames.Pop();
    }

    private async Task ExecuteCallbacksAsync(IEnumerable<Func<Task>> callbacks)
    {
        foreach (var callback in callbacks)
        {
            await ExecuteCallbackAsync(callback);
        }
    }

    private async Task ExecuteCallbackAsync(Func<Task> callback)
    {
        try
        {
            await callback();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "事务提交后回调执行失败: {error}", ex.Message);
        }
    }

    private sealed class TransactionCallbackFrame
    {
        public List<Func<Task>> AfterCommitCallbacks { get; } = [];
    }
}