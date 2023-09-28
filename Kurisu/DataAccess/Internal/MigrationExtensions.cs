using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Kurisu.DataAccess.Extensions;
using Kurisu.DataAccess.Functions.Default;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccess.Internal;

/**
 *
      <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="6.0.20">
       <!-- <PrivateAssets>all</PrivateAssets>  -->
      </PackageReference>
 *
 */
public static class MigrationExtensions
{
    private const string Namespace = "DynamicMigrations";
    private const string ClassName = "EfContentModelSnapshot";
    private const string FullName = $"{Namespace}.{ClassName}";

    /// <summary>
    /// 动态迁移
    /// </summary>
    /// <param name="dbContext"></param>
    public static async Task RunDynamicMigrationAsync(this DbContext dbContext)
    {
        IModel lastModel = null;
        var now = DateTime.Now;

        if (await dbContext.Database.IsTableExistsAsync(dbContext.Model.FindEntityType(typeof(AutoMigrationsHistory))!.GetTableName()))
        {
            // 读取迁移记录,还原modelSnapshot
            var lastMigration = dbContext.Set<AutoMigrationsHistory>()
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(lastMigration?.SnapshotDefine))
            {
                var snapshot = CreateModelSnapshot(FullName, lastMigration.SnapshotDefine);
                lastModel = snapshot.Model;
            }
        }

        var differ = dbContext.GetService<IMigrationsModelDiffer>();
        var lastRelationalModel = lastModel == null
            ? null
            : dbContext.GetService<IModelRuntimeInitializer>()
                .Initialize(((IMutableModel) lastModel).FinalizeModel())
                .GetRelationalModel();
        var currentRelationalModel = dbContext.GetService<IDesignTimeModel>().Model.GetRelationalModel();

        //判断是否存在更改
        if (differ.HasDifferences(lastRelationalModel, currentRelationalModel))
        {
            var operations = differ.GetDifferences(lastRelationalModel, currentRelationalModel);

            //执行迁移
            await dbContext.MigrationAsync(operations);

            //生成新的快照
            var snapshotCode = new DesignTimeServicesBuilder(typeof(DefaultAppDbContext).Assembly, Assembly.GetExecutingAssembly(), new OperationReporter(new OperationReportHandler()), Array.Empty<string>())
                .Build(dbContext)
                .GetRequiredService<IMigrationsCodeGenerator>()
                .GenerateSnapshot(Namespace, typeof(DefaultAppDbContext), ClassName, currentRelationalModel.Model);

            dbContext.Set<AutoMigrationsHistory>().Add(new AutoMigrationsHistory
            {
                SnapshotDefine = snapshotCode,
                MigrationTime = now
            });

            await dbContext.SaveChangesAsync();
        }
    }


    /// <summary>
    /// 创建模型快照
    /// </summary>
    /// <param name="fullName"></param>
    /// <param name="codeDefine"></param>
    /// <returns></returns>
    private static ModelSnapshot CreateModelSnapshot(string fullName, string codeDefine)
    {
        var references = typeof(DefaultAppDbContext).Assembly
            .GetReferencedAssemblies()
            .Select(e => MetadataReference.CreateFromFile(Assembly.Load(e).Location))
            .Union(new MetadataReference[]
            {
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DefaultAppDbContext).Assembly.Location)
            });
        var compilation = CSharpCompilation.Create(Namespace)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(references)
            .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(codeDefine));

        using var stream = new MemoryStream();
        var compileResult = compilation.Emit(stream);

        if (compileResult.Success)
        {
            var obj = Assembly.Load(stream.GetBuffer()).CreateInstance(fullName);
            return obj as ModelSnapshot;
        }

        return null;
    }


    /// <summary>
    /// 迁移
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="operations"></param>
    private static async Task MigrationAsync(this DbContext dbContext, IReadOnlyList<MigrationOperation> operations)
    {
        var sqlGenerator = dbContext.GetService<IMigrationsSqlGenerator>();

        var list = sqlGenerator
            .Generate(operations, dbContext.Model)
            .Select(p => p.CommandText).ToList();

        await dbContext.Database.ExecuteSqlRawAsync(string.Join(Environment.NewLine, list));
    }
}