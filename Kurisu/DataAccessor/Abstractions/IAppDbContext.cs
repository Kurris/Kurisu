namespace Kurisu.DataAccessor.Abstractions
{
    public interface IAppDbContext
    {
        public bool IsAutomaticSaveChanges { get; set; }
    }
}