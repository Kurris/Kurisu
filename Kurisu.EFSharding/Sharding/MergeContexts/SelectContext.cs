using Kurisu.EFSharding.Sharding.Visitors.Selects;

namespace Kurisu.EFSharding.Sharding.MergeContexts;

public class SelectContext
{
    public List<SelectOwnerProperty> SelectProperties { get;  } = new List<SelectOwnerProperty>();

    public bool HasAverage()
    {
        return SelectProperties.Any(o => o is SelectAverageProperty);
    }

    public bool HasCount()
    {
        return SelectProperties.Any(o=>o is SelectCountProperty);
    }

    public override string ToString()
    {
        return String.Join(",",SelectProperties.Select(o=>$"{o}"));
    }
}