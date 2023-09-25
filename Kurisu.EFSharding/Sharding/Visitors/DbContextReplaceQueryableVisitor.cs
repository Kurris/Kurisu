using System.Linq.Expressions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Kurisu.EFSharding.Sharding.Visitors;

internal class DbContextInnerMemberReferenceReplaceQueryableVisitor : ShardingExpressionVisitor
{
    private readonly DbContext _dbContext;
    protected bool RootIsVisit;

    public DbContextInnerMemberReferenceReplaceQueryableVisitor(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override Expression VisitMember
        (MemberExpression memberExpression)
    {
        // Recurse down to see if we can simplify...
        if (memberExpression.IsMemberQueryable()) //2x,3x 路由 单元测试 分表和不分表
        {
            var expressionValue = GetExpressionValue(memberExpression);
            if (expressionValue is IQueryable queryable)
            {
                return ReplaceMemberExpression(queryable);
            }

            if (expressionValue is DbContext dbContext)
            {
                return ReplaceMemberExpression(dbContext);
            }
        }

        return base.VisitMember(memberExpression);
    }

    private MemberExpression ReplaceMemberExpression(IQueryable queryable)
    {
        var dbContextReplaceQueryableVisitor = new DbContextReplaceQueryableVisitor(_dbContext);
        var newExpression = dbContextReplaceQueryableVisitor.Visit(queryable.Expression);
        var newQueryable = dbContextReplaceQueryableVisitor.Source.Provider.CreateQuery(newExpression);
        var tempVariableGenericType = typeof(TempVariable<>).GetGenericType0(queryable.ElementType);
        var tempVariable = Activator.CreateInstance(tempVariableGenericType, newQueryable);
        MemberExpression queryableMemberReplaceExpression =
            Expression.Property(ConstantExpression.Constant(tempVariable), nameof(TempVariable<object>.Queryable));
        return queryableMemberReplaceExpression;
    }

    private MemberExpression ReplaceMemberExpression(DbContext dbContext)
    {
        var tempVariableGenericType = typeof(TempDbVariable<>).GetGenericType0(dbContext.GetType());
        var tempVariable = Activator.CreateInstance(tempVariableGenericType, _dbContext);
        MemberExpression dbContextMemberReplaceExpression =
            Expression.Property(ConstantExpression.Constant(tempVariable),
                nameof(TempDbVariable<object>.DbContext));
        return dbContextMemberReplaceExpression;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (RootIsVisit && node.Method.ReturnType.IsMethodReturnTypeQueryableType() && node.Method.ReturnType.IsGenericType)
        {
            var notRoot = node.Arguments.IsEmpty();

            if (notRoot)
            {
                var entityType = node.Method.ReturnType.GenericTypeArguments[0];

                var whereCallExpression = ReplaceMethodCallExpression(node, entityType);
                return whereCallExpression;
            }
        }

        return base.VisitMethodCall(node);
    }

    private MethodCallExpression ReplaceMethodCallExpression(MethodCallExpression methodCallExpression,
        Type entityType)
    {
        MethodCallExpression whereCallExpression = Expression.Call(
            typeof(IShardingQueryableExtension),
            nameof(IShardingQueryableExtension.ReplaceDbContextQueryableWithType),
            new Type[] {entityType},
            methodCallExpression, Expression.Constant(_dbContext)
        );
        return whereCallExpression;
    }

    internal sealed class TempVariable<T1>
    {
        public IQueryable<T1> Queryable { get; }

        public TempVariable(IQueryable<T1> queryable)
        {
            Queryable = queryable;
        }

        public IQueryable<T1> GetQueryable()
        {
            return Queryable;
        }
    }

    internal sealed class TempDbVariable<T1>
    {
        public T1 DbContext { get; }

        public TempDbVariable(T1 dbContext)
        {
            DbContext = dbContext;
        }
    }
}

internal class DbContextReplaceQueryableVisitor : DbContextInnerMemberReferenceReplaceQueryableVisitor
{
    private readonly DbContext _dbContext;
    public IQueryable Source;

    public DbContextReplaceQueryableVisitor(DbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    protected override Expression VisitExtension(Expression node)
    {
        if (node is QueryRootExpression queryRootExpression)
        {
            var dbContextDependencies =
                typeof(DbContext).GetTypePropertyValue(_dbContext, "DbContextDependencies") as
                    IDbContextDependencies;

            var targetIQ =
                (IQueryable) ((IDbSetCache) _dbContext).GetOrAddSet(dbContextDependencies.SetSource,
                    queryRootExpression.EntityType.ClrType);


            var newQueryable = targetIQ.Provider.CreateQuery(targetIQ.Expression);
            if (Source == null)
                Source = newQueryable;
            RootIsVisit = true;
            if (queryRootExpression is FromSqlQueryRootExpression fromSqlQueryRootExpression)
            {
                var sqlQueryRootExpression = new FromSqlQueryRootExpression(newQueryable.Provider as IAsyncQueryProvider,
                    queryRootExpression.EntityType, fromSqlQueryRootExpression.Sql,
                    fromSqlQueryRootExpression.Argument);

                return base.VisitExtension(sqlQueryRootExpression);
            }
            else
            {
                var replaceQueryRoot = new ReplaceSingleQueryRootExpressionVisitor();
                replaceQueryRoot.Visit(newQueryable.Expression);
                return base.VisitExtension(replaceQueryRoot.QueryRootExpression);
            }
        }

        return base.VisitExtension(node);
    }

    internal sealed class ReplaceSingleQueryRootExpressionVisitor : ExpressionVisitor
    {
        public QueryRootExpression QueryRootExpression { get; set; }

        protected override Expression VisitExtension(Expression node)
        {
            if (node is QueryRootExpression queryRootExpression)
            {
                if (QueryRootExpression != null)
                    throw new ShardingCoreException("replace query root more than one query root");
                QueryRootExpression = queryRootExpression;
            }

            return base.VisitExtension(node);
        }
    }
}