using System.Reflection;
using System.Reflection.Emit;
using AspectCore.Extensions.Reflection.Emit;

namespace AspectCore.Extensions.Reflection
{
    public partial class FieldReflector : MemberReflector<FieldInfo>
    {
        protected readonly Func<object, object> _getter;
        protected readonly Action<object, object> _setter;

        protected FieldReflector(FieldInfo reflectionInfo) : base(reflectionInfo)
        {
            _getter = CreateGetter();
            _setter = CreateSetter();
        }

        protected virtual Func<object, object> CreateGetter()
        {
            var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object) }, _reflectionInfo.Module, true);
            var ilGen = dynamicMethod.GetILGenerator();
            ilGen.EmitLoadArg(0);
            ilGen.EmitConvertFromObject(_reflectionInfo.DeclaringType);
            ilGen.Emit(OpCodes.Ldfld, _reflectionInfo);
            ilGen.EmitConvertToObject(_reflectionInfo.FieldType);
            ilGen.Emit(OpCodes.Ret);
            return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
        }

        protected virtual Action<object, object> CreateSetter()
        {
            var dynamicMethod = new DynamicMethod($"setter-{Guid.NewGuid()}", typeof(void), new Type[] { typeof(object), typeof(object) }, _reflectionInfo.Module, true);
            var ilGen = dynamicMethod.GetILGenerator();
            ilGen.EmitLoadArg(0);
            ilGen.EmitConvertFromObject(_reflectionInfo.DeclaringType);
            ilGen.EmitLoadArg(1);
            ilGen.EmitConvertFromObject(_reflectionInfo.FieldType);
            ilGen.Emit(OpCodes.Stfld, _reflectionInfo);
            ilGen.Emit(OpCodes.Ret);
            return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
        }

        public virtual object GetValue(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return _getter(instance);
        }

        public virtual void SetValue(object instance, object value)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            _setter(instance, value);
        }

        public virtual object GetStaticValue()
        {
            throw new InvalidOperationException($"Field {_reflectionInfo.Name} must be static to call this method. For get instance field value, call 'GetValue'.");
        }

        public virtual void SetStaticValue(object value)
        {
            throw new InvalidOperationException($"Field {_reflectionInfo.Name} must be static to call this method. For set instance field value, call 'SetValue'.");
        }


        private class EnumFieldReflector : FieldReflector
        {
            public EnumFieldReflector(FieldInfo reflectionInfo) : base(reflectionInfo)
            {
            }

            protected override Func<object, object> CreateGetter()
            {
                var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object) }, _reflectionInfo.Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                var value = _reflectionInfo.GetValue(null);
                ilGen.EmitConstant(value, _reflectionInfo.FieldType);
                ilGen.EmitConvertToObject(_reflectionInfo.FieldType);
                ilGen.Emit(OpCodes.Ret);
                return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
            }

            protected override Action<object, object> CreateSetter()
            {
                var dynamicMethod = new DynamicMethod($"setter-{Guid.NewGuid()}", typeof(void), new Type[] { typeof(object), typeof(object) }, _reflectionInfo.Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                ilGen.Emit(OpCodes.Ret);
                return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
            }

            public override object GetValue(object instance)
            {
                return _getter(null);
            }

            public override void SetValue(object instance, object value)
            {
                throw new FieldAccessException("Cannot set a constant field");
            }

            public override object GetStaticValue()
            {
                return _getter(null);
            }

            public override void SetStaticValue(object value)
            {
                throw new FieldAccessException("Cannot set a constant field");
            }
        }

        private class OpenGenericFieldReflector : FieldReflector
        {
            public OpenGenericFieldReflector(FieldInfo reflectionInfo) : base(reflectionInfo)
            {
            }

            protected override Func<object, object> CreateGetter() => null;

            protected override Action<object, object> CreateSetter() => null;

            public override object GetValue(object instance) => throw new InvalidOperationException("Late bound operations cannot be performed on fields with types for which Type.ContainsGenericParameters is true");

            public override void SetValue(object instance, object value) => throw new InvalidOperationException("Late bound operations cannot be performed on fields with types for which Type.ContainsGenericParameters is true");

            public override object GetStaticValue() => throw new InvalidOperationException("Late bound operations cannot be performed on fields with types for which Type.ContainsGenericParameters is true");

            public override void SetStaticValue(object value) => throw new InvalidOperationException("Late bound operations cannot be performed on fields with types for which Type.ContainsGenericParameters is true");
        }

        private class StaticFieldReflector : FieldReflector
        {
            public StaticFieldReflector(FieldInfo reflectionInfo) : base(reflectionInfo)
            {
            }

            protected override Func<object, object> CreateGetter()
            {
                var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object) }, _reflectionInfo.Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                ilGen.Emit(OpCodes.Ldsfld, _reflectionInfo);
                ilGen.EmitConvertToObject(_reflectionInfo.FieldType);
                ilGen.Emit(OpCodes.Ret);
                return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
            }

            protected override Action<object, object> CreateSetter()
            {
                var dynamicMethod = new DynamicMethod($"setter-{Guid.NewGuid()}", typeof(void), new Type[] { typeof(object), typeof(object) }, _reflectionInfo.Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                ilGen.EmitLoadArg(1);
                ilGen.EmitConvertFromObject(_reflectionInfo.FieldType);
                ilGen.Emit(OpCodes.Stsfld, _reflectionInfo);
                ilGen.Emit(OpCodes.Ret);
                return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
            }

            public override object GetValue(object instance)
            {
                return _getter(null);
            }

            public override void SetValue(object instance, object value)
            {
                _setter(null, value);
            }

            public override object GetStaticValue()
            {
                return _getter(null);
            }

            public override void SetStaticValue(object value)
            {
                _setter(null, value);
            }
        }
        
        internal static FieldReflector Create(FieldInfo reflectionInfo)
        {
            if (reflectionInfo == null)
            {
                throw new ArgumentNullException(nameof(reflectionInfo));
            }

            return ReflectorCacheUtils<FieldInfo, FieldReflector>.GetOrAdd(reflectionInfo, CreateInternal);

            FieldReflector CreateInternal(FieldInfo field)
            {
                if (field.DeclaringType.GetTypeInfo().ContainsGenericParameters)
                {
                    return new OpenGenericFieldReflector(field);
                }

                if (field.DeclaringType.IsEnum)
                {
                    return new EnumFieldReflector(field);
                }

                if (field.IsStatic)
                {
                    return new StaticFieldReflector(field);
                }

                return new FieldReflector(field);
            }
        }
    }
}