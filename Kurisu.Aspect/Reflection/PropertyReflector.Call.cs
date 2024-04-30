using System.Reflection;
using System.Reflection.Emit;
using Kurisu.Aspect.Reflection.Emit;

namespace Kurisu.Aspect.Reflection;

internal partial class PropertyReflector
{
    public class CallPropertyReflector : PropertyReflector
    {
        public CallPropertyReflector(PropertyInfo reflectionInfo)
            : base(reflectionInfo)
        {
            }

        protected override Func<object, object> CreateGetter()
        {
                var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object) }, Current.Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                ilGen.EmitLoadArgument(0);
                ilGen.EmitConvertFromObject(Current.DeclaringType);
                if (Current.DeclaringType.GetTypeInfo().IsValueType)
                {
                    var local = ilGen.DeclareLocal(Current.DeclaringType);
                    ilGen.Emit(OpCodes.Stloc, local);
                    ilGen.Emit(OpCodes.Ldloca, local);
                }
                ilGen.Emit(OpCodes.Call, Current.GetMethod);
                if (Current.PropertyType.GetTypeInfo().IsValueType)
                    ilGen.EmitConvertToObject(Current.PropertyType);
                ilGen.Emit(OpCodes.Ret);
                return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
            }

        protected override Action<object, object> CreateSetter()
        {
                var dynamicMethod = new DynamicMethod($"setter-{Guid.NewGuid()}", typeof(void), new Type[] { typeof(object), typeof(object) }, Current.Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                ilGen.EmitLoadArgument(0);
                ilGen.EmitConvertFromObject(Current.DeclaringType);
                if (Current.DeclaringType.GetTypeInfo().IsValueType)
                {
                    var local = ilGen.DeclareLocal(Current.DeclaringType);
                    ilGen.Emit(OpCodes.Stloc, local);
                    ilGen.Emit(OpCodes.Ldloca, local);
                }
                ilGen.EmitLoadArgument(1);
                ilGen.EmitConvertFromObject(Current.PropertyType);
                ilGen.Emit(OpCodes.Call, Current.SetMethod);
                ilGen.Emit(OpCodes.Ret);
                return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
            }
    }
}