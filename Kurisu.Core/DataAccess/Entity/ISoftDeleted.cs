namespace Kurisu.Core.DataAccess.Entity;

public interface ISoftDeleted
{
    bool IsDeleted { get; set; }
}