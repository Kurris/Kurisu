using System.Linq.Expressions;
using Kurisu.EFSharding.Extensions.ShardingQueryableExtensions;

namespace Kurisu.EFSharding.Sharding.Visitors.ShardingExtractParameters;

internal class ShardingQueryableExtractParameterVisitor : ExpressionVisitor
{
    private bool isNotSupport;
    private ShardingQueryableUseConnectionModeOptions shardingQueryableUseConnectionModeOptions;
    private ShardingQueryableAsRouteOptions shardingQueryableAsRouteOptions;
    private ShardingQueryableReadWriteSeparationOptions shardingQueryableReadWriteSeparationOptions;
    private ShardingQueryableAsSequenceOptions shardingQueryableAsSequenceOptions;

    public ShardingExtParameter ExtractShardingParameter()
    {
        return new ShardingExtParameter(isNotSupport, shardingQueryableAsRouteOptions, shardingQueryableUseConnectionModeOptions, shardingQueryableReadWriteSeparationOptions, shardingQueryableAsSequenceOptions);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.IsGenericMethod)
        {
            var genericMethodDefinition = node.Method.GetGenericMethodDefinition();

            // find  notsupport extention calls
            if (genericMethodDefinition == EntityFrameworkShardingQueryableExtension.NotSupportMethodInfo)
            {
                isNotSupport = true;
                // cut out extension expression
                return Visit(node.Arguments[0]);
            }
            else if (genericMethodDefinition == EntityFrameworkShardingQueryableExtension.UseConnectionModeMethodInfo)
            {
                shardingQueryableUseConnectionModeOptions = node.Arguments
                    .OfType<ConstantExpression>()
                    .Where(o => o.Value is ShardingQueryableUseConnectionModeOptions)
                    .Select(o => (ShardingQueryableUseConnectionModeOptions) o.Value)
                    .Last();
                return Visit(node.Arguments[0]);
            }
            else if (genericMethodDefinition == EntityFrameworkShardingQueryableExtension.AsRouteMethodInfo)
            {
                shardingQueryableAsRouteOptions = node.Arguments
                    .OfType<ConstantExpression>()
                    .Where(o => o.Value is ShardingQueryableAsRouteOptions)
                    .Select(o => (ShardingQueryableAsRouteOptions) o.Value)
                    .Last();
                return Visit(node.Arguments[0]);
            }
            else if (genericMethodDefinition == EntityFrameworkShardingQueryableExtension.AsSequenceModeMethodInfo)
            {
                shardingQueryableAsSequenceOptions = node.Arguments
                    .OfType<ConstantExpression>()
                    .Where(o => o.Value is ShardingQueryableAsSequenceOptions)
                    .Select(o => (ShardingQueryableAsSequenceOptions) o.Value)
                    .Last();
                return Visit(node.Arguments[0]);
            }
            //else if (genericMethodDefinition == EntityFrameworkShardingQueryableExtension.ReadWriteSeparationMethodInfo)
            //{
            //    shardingQueryableReadWriteSeparationOptions = node.Arguments
            //        .OfType<ConstantExpression>()
            //        .Where(o => o.Value is ShardingQueryableReadWriteSeparationOptions)
            //        .Select(o => (ShardingQueryableReadWriteSeparationOptions)o.Value)
            //        .Last();
            //    return Visit(node.Arguments[0]);
            //}
        }

        return base.VisitMethodCall(node);
    }
}