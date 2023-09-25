using System.Linq.Expressions;
using System.Reflection;
using Kurisu.EFSharding.Core.DbContextCreator;
using Kurisu.EFSharding.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Helpers;

public class ShardingCoreHelper
{
    private ShardingCoreHelper()
    {
    }

    /// <summary>
    /// c#默认的字符串gethashcode只是进程内一致如果程序关闭开启后那么就会乱掉所以这边建议重写string的gethashcode或者使用shardingcore提供的
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int GetStringHashCode(string value)
    {
        var h = 0; // 默认值是0
        if (value.Length > 0)
        {
            h = value.Aggregate(h, (current, t) => 31 * current + t);
        }

        return h;
    }

    public static DateTime ConvertLongToDateTime(long timeStamp)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).LocalDateTime;
    }

    public static long ConvertDateTimeToLong(DateTime localDateTime)
    {
        return new DateTimeOffset(localDateTime).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 获取当月第一天
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static DateTime GetCurrentMonthFirstDay(DateTime time)
    {
        return time.AddDays(1 - time.Day).Date;
    }

    /// <summary>
    /// 获取下个月第一天
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static DateTime GetNextMonthFirstDay(DateTime time)
    {
        return time.AddDays(1 - time.Day).Date.AddMonths(1);
    }

    public static DateTime GetCurrentMonday(DateTime time)
    {
        DateTime dateTime1 = new DateTime(time.Year, time.Month, time.Day);
        int num = (int) (time.DayOfWeek - 1);
        if (num == -1)
            num = 6;
        return dateTime1.AddDays(-num);
    }

    public static DateTime GetCurrentSunday(DateTime time)
    {
        return GetCurrentMonday(time).AddDays(6);
    }


    /// <summary>
    /// check TContext ctor is <see cref="DbContextOptions"/>
    /// </summary>
    /// <typeparam name="TContext">DbContext</typeparam>
    public static void CheckContextConstructors<TContext>()
        where TContext : DbContext
    {
        var contextType = typeof(TContext);
        var constructorInfos = contextType.GetConstructors().ToList();
        if (constructorInfos.Count(x => !x.IsStatic) != 1)
        {
            throw new ArgumentException($"DbContext : {contextType} declared constructor count more {contextType},if u want support multi constructor params plz replace ${nameof(IDbContextCreator)} interface");
        }

        var defaultDeclaredConstructor = constructorInfos.First(o => !o.IsStatic);
        if (defaultDeclaredConstructor.GetParameters().Length != 1)
        {
            throw new ArgumentException($"DbContext : {contextType} declared constructor parameters more ,if u want support multi constructor params plz replace ${nameof(IDbContextCreator)} interface");
        }

        var paramType = defaultDeclaredConstructor.GetParameters()[0].ParameterType;
        if (paramType != typeof(ShardingDbContextOptions) && paramType != typeof(DbContextOptions) && paramType != typeof(DbContextOptions<TContext>))
        {
            throw new ArgumentException($"DbContext : {contextType} declared constructor parameters should use {typeof(ShardingDbContextOptions)} or {typeof(DbContextOptions)} or {typeof(DbContextOptions<TContext>)},if u want support multi constructor params plz replace ${nameof(IDbContextCreator)} interface ");
        }
    }

    public static Func<ShardingDbContextOptions, DbContext> CreateActivator<TContext>() where TContext : DbContext
    {
        var constructors = typeof(TContext).GetConstructors()
            .Where(c => !c.IsStatic && c.IsPublic)
            .ToArray();

        var parameters = constructors[0].GetParameters();
        var parameterType = parameters[0].ParameterType;


        if (parameterType == typeof(ShardingDbContextOptions))
        {
            return CreateShardingDbContextOptionsActivator<TContext>(constructors[0], parameterType);
        }

        if (parameterType.IsAssignableTo(typeof(DbContextOptions)))
        {
            if (parameterType != typeof(DbContextOptions)
                && parameterType != typeof(DbContextOptions<TContext>))
            {
                throw new ShardingCoreException("cant create activator");
            }

            return CreateDbContextOptionsGenericActivator<TContext>(constructors[0], parameterType);
        }

        var po = Expression.Parameter(parameterType, "o");
        var new1 = Expression.New(constructors[0], po);
        var inner = Expression.Lambda(new1, po);

        var args = Expression.Parameter(typeof(ShardingDbContextOptions), "args");
        var body = Expression.Invoke(inner, Expression.Convert(args, po.Type));
        var outer = Expression.Lambda<Func<ShardingDbContextOptions, TContext>>(body, args);
        var func = outer.Compile();
        return func;


        static Func<ShardingDbContextOptions, DbContext> CreateDbContextOptionsGenericActivator<TContext>(ConstructorInfo constructor, Type paramType) where TContext : DbContext
        {
            var parameterExpression = Expression.Parameter(typeof(ShardingDbContextOptions));
            var paramMemberExpression = Expression.Property(parameterExpression, nameof(ShardingDbContextOptions.DbContextOptions));

            //参数为ShardingDbContextOptions的构造函数
            var newExpression = Expression.New(constructor, Expression.Convert(paramMemberExpression, paramType));
            var inner = Expression.Lambda(newExpression, parameterExpression);

            var args = Expression.Parameter(typeof(ShardingDbContextOptions));
            var body = Expression.Invoke(inner, Expression.Convert(args, parameterExpression.Type));
            var outer = Expression.Lambda<Func<ShardingDbContextOptions, TContext>>(body, args);

            return outer.Compile();
        }

        static Func<ShardingDbContextOptions, DbContext> CreateShardingDbContextOptionsActivator<TContext>(ConstructorInfo constructor, Type paramType) where TContext : DbContext
        {
            var po = Expression.Parameter(paramType, "o");
            var newExpression = Expression.New(constructor, po);
            var inner = Expression.Lambda(newExpression, po);

            var args = Expression.Parameter(typeof(ShardingDbContextOptions), "args");
            var body = Expression.Invoke(inner, Expression.Convert(args, po.Type));
            var outer = Expression.Lambda<Func<ShardingDbContextOptions, TContext>>(body, args);
            var func = outer.Compile();
            return func;
        }
    }
}