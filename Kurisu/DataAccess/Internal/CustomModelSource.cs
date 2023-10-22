//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Infrastructure;
//using Microsoft.EntityFrameworkCore.Metadata;
//using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

//namespace Kurisu.DataAccess.Internal;

//public class CustomModelSource : ModelSource
//{
//    public CustomModelSource(ModelSourceDependencies dependencies) : base(dependencies)
//    {
//    }


//    protected override IModel CreateModel(DbContext context, IConventionSetBuilder conventionSetBuilder, ModelDependencies modelDependencies)
//    {
//        return base.CreateModel(context, conventionSetBuilder, modelDependencies);
//    }
//}