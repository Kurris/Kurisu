namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IShardingTransaction
{
    void NotifyShardingTransaction();
    void Rollback();
    void Commit();
    Task RollbackAsync(CancellationToken cancellationToken = new());
    Task CommitAsync(CancellationToken cancellationToken = new());
    void CreateSavepoint(string name);
    Task CreateSavepointAsync(string name, CancellationToken cancellationToken = new());
    void RollbackToSavepoint(string name);
    Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
    void ReleaseSavepoint(string name);
    Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);
}