namespace Kurisu.EFSharding.Core.TrackerManagers;

public interface ITrackerManager
{
    bool AddDbContextModel(Type entityType,bool hasKey);
    bool EntityUseTrack(Type entityType);
    bool IsDbContextModel(Type entityType);
    Type TranslateEntityType(Type entityType);
}