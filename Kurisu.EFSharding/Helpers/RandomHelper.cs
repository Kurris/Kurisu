namespace Kurisu.EFSharding.Helpers;

public  static class RandomHelper
{
    static int seed = Environment.TickCount;

    static readonly ThreadLocal<Random> random =
        new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

    public static int Next()
    {
        return random.Value.Next();
    }
    public static int Next(int max)
    {
        return random.Value.Next(max);
    }
    public static int Next(int min,int max)
    {
        return random.Value.Next(min,max);
    }
}