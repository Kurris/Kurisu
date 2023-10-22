//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Kurisu.DataAccess.Sharding.VirtualRoute.TableRoute.Abstractions;

//public abstract class VirtualTableModRoute<TEntity> : AbstractVirtualTableRoute<TEntity> where TEntity : class, new()
//{
//    private readonly int _tailLength;
//    private readonly int _mod;

//    protected VirtualTableModRoute(int tailLength, int mod)
//    {
//        _tailLength = tailLength;
//        _mod = mod;
//    }

//    protected virtual char Padding => '0';

//    protected virtual string ToTail(object value)
//    {
//        var s = value.ToString()!;
//        var h = 0;
//        if (s.Length > 0)
//        {
//            h = s.Aggregate(h, (current, t) => 31 * current + t);
//        }

//        return Math.Abs(h % _mod).ToString().PadLeft(_tailLength, Padding);
//    }

//    public override IEnumerable<string> GetTails()
//    {
//        return Enumerable.Range(0, _mod).Select(x => x.ToString().PadLeft(_tailLength, Padding));
//    }
//}