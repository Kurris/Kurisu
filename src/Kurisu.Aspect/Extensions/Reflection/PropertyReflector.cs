using System.Reflection;
using System.Reflection.Emit;
using AspectCore.Extensions.Reflection.Emit;

namespace AspectCore.Extensions.Reflection
{
    public class PropertyReflector : MemberReflector<PropertyInfo>
    {
        protected readonly bool _canRead;
        protected readonly bool _canWrite;
        protected readonly Func<object, object> _getter;
        protected readonly Action<object, object> _setter;

        private PropertyReflector(PropertyInfo reflectionInfo) : base(reflectionInfo)
        {
            _getter = reflectionInfo.CanRead ? CreateGetter() : ins => throw new InvalidOperationException($"Property {_reflectionInfo.Name} does not define get accessor.");
            _setter = reflectionInfo.CanWrite ? CreateSetter() : (ins, val) => { throw new InvalidOperationException($"Property {_reflectionInfo.Name} does not define get accessor."); };
        }

        protected virtual Func<object, object> CreateGetter()
        {
            var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object) }, _reflectionInfo.Module, true);
            var ilGen = dynamicMethod.GetILGenerator();
            ilGen.EmitLoadArg(0);
            ilGen.EmitConvertFromObject(_reflectionInfo.DeclaringType);
            ilGen.Emit(OpCodes.Callvirt, _reflectionInfo.GetMethod);
            if (_reflectionInfo.PropertyType.GetTypeInfo().IsValueType)
                ilGen.EmitConvertToObject(_reflectionInfo.PropertyType);
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
            ilGen.EmitConvertFromObject(_reflectionInfo.PropertyType);
            ilGen.Emit(OpCodes.Callvirt, _reflectionInfo.SetMethod);
            ilGen.Emit(OpCodes.Ret);
            return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
        }

        public virtual object GetValue(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return _getter.Invoke(instance);
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
            throw new InvalidOperationException($"Property {_reflectionInfo.Name} must be static to call this method. For get instance property value, call 'GetValue'.");
        }

        public virtual void SetStaticValue(object value)
        {
            throw new InvalidOperationException($"Property {_reflectionInfo.Name} must be static to call this method. For set instance property value, call 'SetValue'.");
        }


        private class CallPropertyReflector : PropertyReflector
        {
            public CallPropertyReflector(PropertyInfo reflectionInfo)
                : base(reflectionInfo)
            {
            }

            protected override Func<object, object> CreateGetter()
            {
                var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object) }, _reflectionInfo.Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                ilGen.EmitLoadArg(0);
                ilGen.EmitConvertFromObject(_reflectionInfo.DeclaringType);
                if (_reflectionInfo.DeclaringType.GetTypeInfo().IsValueType)
                {
                    var local = ilGen.DeclareLocal(_reflectionInfo.DeclaringType);
                    ilGen.Emit(OpCodes.Stloc, local);
                    ilGen.Emit(OpCodes.Ldloca, local);
                }

                ilGen.Emit(OpCodes.Call, _reflectionInfo.GetMethod);
                if (_reflectionInfo.PropertyType.GetTypeInfo().IsValueType)
                    ilGen.EmitConvertToObject(_reflectionInfo.PropertyType);
                ilGen.Emit(OpCodes.Ret);
                return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
            }

            protected override Action<object, object> CreateSetter()
            {
                var dynamicMethod = new DynamicMethod($"setter-{Guid.NewGuid()}", typeof(void), new Type[] { typeof(object), typeof(object) }, _reflectionInfo.Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                ilGen.EmitLoadArg(0);
                ilGen.EmitConvertFromObject(_reflectionInfo.DeclaringType);
                if (_reflectionInfo.DeclaringType.GetTypeInfo().IsValueType)
                {
                    var local = ilGen.DeclareLocal(_reflectionInfo.DeclaringType);
                    ilGen.Emit(OpCodes.Stloc, local);
                    ilGen.Emit(OpCodes.Ldloca, local);
                }

                ilGen.EmitLoadArg(1);
                ilGen.EmitConvertFromObject(_reflectionInfo.PropertyType);
                ilGen.Emit(OpCodes.Call, _reflectionInfo.SetMethod);
                ilGen.Emit(OpCodes.Ret);
                return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
            }
        }

        private class OpenGenericPropertyReflector : PropertyReflector
        {
            public OpenGenericPropertyReflector(PropertyInfo reflectionInfo) : base(reflectionInfo)
            {
            }

            public override object GetValue(object instance) => throw new InvalidOperationException("Late bound operations cannot be performed on property with types for which Type.ContainsGenericParameters is true");

            public override void SetValue(object instance, object value) => throw new InvalidOperationException("Late bound operations cannot be performed on property with types for which Type.ContainsGenericParameters is true");

            public override object GetStaticValue() => throw new InvalidOperationException("Late bound operations cannot be performed on property with types for which Type.ContainsGenericParameters is true");

            public override void SetStaticValue(object value) => throw new InvalidOperationException("Late bound operations cannot be performed on property with types for which Type.ContainsGenericParameters is true");
        }

        private class StaticPropertyReflector : PropertyReflector
        {
            public StaticPropertyReflector(PropertyInfo reflectionInfo)
                : base(reflectionInfo)
            {
            }

            protected override Func<object, object> CreateGetter()
            {
                var dynamicMethod = new DynamicMethod($"getter-{Guid.NewGuid()}", typeof(object), new Type[] { typeof(object) }, _reflectionInfo.Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                ilGen.Emit(OpCodes.Call, _reflectionInfo.GetMethod);
                if (_reflectionInfo.PropertyType.GetTypeInfo().IsValueType)
                    ilGen.EmitConvertToObject(_reflectionInfo.PropertyType);
                ilGen.Emit(OpCodes.Ret);
                return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
            }

            protected override Action<object, object> CreateSetter()
            {
                var dynamicMethod = new DynamicMethod($"setter-{Guid.NewGuid()}", typeof(void), new Type[] { typeof(object), typeof(object) }, _reflectionInfo.Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                ilGen.EmitLoadArg(1);
                ilGen.EmitConvertFromObject(_reflectionInfo.PropertyType);
                ilGen.Emit(OpCodes.Call, _reflectionInfo.SetMethod);
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

        internal static PropertyReflector Create(PropertyInfo reflectionInfo, CallOptions callOption)
        {
            if (reflectionInfo == null)
            {
                throw new ArgumentNullException(nameof(reflectionInfo));
            }

            return ReflectorCacheUtils<ValueTuple<PropertyInfo, CallOptions>, PropertyReflector>.GetOrAdd(new ValueTuple<PropertyInfo, CallOptions>(reflectionInfo, callOption), CreateInternal);

            PropertyReflector CreateInternal(ValueTuple<PropertyInfo, CallOptions> item)
            {
                var property = item.Item1;
                if (property.DeclaringType.GetTypeInfo().ContainsGenericParameters)
                {
                    return new OpenGenericPropertyReflector(property);
                }

                if ((property.CanRead && property.GetMethod.IsStatic) || (property.CanWrite && property.SetMethod.IsStatic))
                {
                    return new StaticPropertyReflector(property);
                }

                if (property.DeclaringType.GetTypeInfo().IsValueType || item.Item2 == CallOptions.Call)
                {
                    return new CallPropertyReflector(property);
                }

                return new PropertyReflector(property);
            }
        }
    }
}