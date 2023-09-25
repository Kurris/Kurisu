using Kurisu.EFSharding.Core.RuntimeContexts;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kurisu.EFSharding.EFCores.EFCore6x;

public sealed class ScriptMigrationGenerator : AbstractScriptMigrationGenerator
{
    private readonly string _fromMigration;
    private readonly string _toMigration;
    private readonly MigrationsSqlGenerationOptions _options;

    public ScriptMigrationGenerator(IShardingRuntimeContext shardingRuntimeContext, string fromMigration = null,
        string toMigration = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default) : base(shardingRuntimeContext)
    {
        _fromMigration = fromMigration;
        _toMigration = toMigration;
        _options = options;
    }

    protected override string GenerateScriptSql(IMigrator migrator)
    {
        return migrator.GenerateScript(_fromMigration, _toMigration, _options);
    }
}