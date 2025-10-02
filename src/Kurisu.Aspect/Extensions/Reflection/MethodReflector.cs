﻿using System.Reflection;
using System.Reflection.Emit;
using AspectCore.Extensions.Reflection.Emit;

namespace AspectCore.Extensions.Reflection
{
    public class MethodReflector : MemberReflector<MethodInfo>, IParameterReflectorProvider
    {
        protected readonly Func<object, object[], object> _invoker;
        private readonly ParameterReflector[] _parameterReflectors;


        private MethodReflector(MethodInfo reflectionInfo) : base(reflectionInfo)
        {
            _displayName = GetDisplayName(reflectionInfo);
            _invoker = CreateInvoker();
            _parameterReflectors = reflectionInfo.GetParameters().Select(x => ParameterReflector.Create(x)).ToArray();
        }

        private readonly string _displayName;

        public override string DisplayName => _displayName;
        public ParameterReflector[] ParameterReflectors => _parameterReflectors;

        private static string GetDisplayName(MethodInfo method)
        {
            var name = $"{method.ReturnType.GetReflector().DisplayName} {method.Name}";
            if (method.IsGenericMethod)
            {
                name += "<";
                var arguments = method.GetGenericArguments();
                name += arguments[0].GetReflector().DisplayName;
                for (var i = 1; i < arguments.Length; i++)
                {
                    name += ("," + arguments[i].GetReflector().DisplayName);
                }

                name += ">";
            }

            var parameterTypes = method.GetParameterTypes();
            name += "(";
            if (parameterTypes.Length == 0)
            {
                name += ")";
                return name;
            }

            name += parameterTypes[0].GetReflector().DisplayName;
            for (var i = 1; i < parameterTypes.Length; i++)
            {
                name += ("," + parameterTypes[i].GetReflector().DisplayName);
            }

            name += ")";
            return name;
        }


        protected virtual Func<object, object[], object> CreateInvoker()
        {
            DynamicMethod dynamicMethod = new DynamicMethod($"invoker_{_displayName}",
                typeof(object), new Type[] { typeof(object), typeof(object[]) }, _reflectionInfo.Module, true);

            ILGenerator ilGen = dynamicMethod.GetILGenerator();
            var parameterTypes = _reflectionInfo.GetParameterTypes();

            ilGen.EmitLoadArg(0);
            ilGen.EmitConvertFromObject(_reflectionInfo.DeclaringType);

            if (parameterTypes.Length == 0)
            {
                return CreateDelegate();
            }

            var refParameterCount = parameterTypes.Count(x => x.IsByRef);
            if (refParameterCount == 0)
            {
                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    ilGen.EmitLoadArg(1);
                    ilGen.EmitInt(i);
                    ilGen.Emit(OpCodes.Ldelem_Ref);
                    ilGen.EmitConvertFromObject(parameterTypes[i]);
                }

                return CreateDelegate();
            }

            var indexedLocals = new IndexedLocalBuilder[refParameterCount];
            var index = 0;
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                ilGen.EmitLoadArg(1);
                ilGen.EmitInt(i);
                ilGen.Emit(OpCodes.Ldelem_Ref);
                if (parameterTypes[i].IsByRef)
                {
                    var defType = parameterTypes[i].GetElementType();
                    var indexedLocal = new IndexedLocalBuilder(ilGen.DeclareLocal(defType), i);
                    indexedLocals[index++] = indexedLocal;
                    ilGen.EmitConvertFromObject(defType);
                    ilGen.Emit(OpCodes.Stloc, indexedLocal.LocalBuilder);
                    ilGen.Emit(OpCodes.Ldloca, indexedLocal.LocalBuilder);
                }
                else
                {
                    ilGen.EmitConvertFromObject(parameterTypes[i]);
                }
            }

            return CreateDelegate(() =>
            {
                for (var i = 0; i < indexedLocals.Length; i++)
                {
                    ilGen.EmitLoadArg(1);
                    ilGen.EmitInt(indexedLocals[i].Index);
                    ilGen.Emit(OpCodes.Ldloc, indexedLocals[i].LocalBuilder);
                    ilGen.EmitConvertToObject(indexedLocals[i].LocalType);
                    ilGen.Emit(OpCodes.Stelem_Ref);
                }
            });

            Func<object, object[], object> CreateDelegate(Action callback = null)
            {
#if NET6_0
                //https://github.com/dotnet/runtime/issues/67084
                ilGen.Emit(OpCodes.Callvirt, _reflectionInfo);
#else
                ilGen.EmitCall(OpCodes.Callvirt, _reflectionInfo, null);
#endif
                callback?.Invoke();
                if (_reflectionInfo.ReturnType == typeof(void)) ilGen.Emit(OpCodes.Ldnull);
                else if (_reflectionInfo.ReturnType.GetTypeInfo().IsValueType)
                    ilGen.EmitConvertToObject(_reflectionInfo.ReturnType);
                ilGen.Emit(OpCodes.Ret);
                return (Func<object, object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>));
            }
        }

        public virtual object Invoke(object instance, params object[] parameters)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return _invoker(instance, parameters);
        }

        public virtual object StaticInvoke(params object[] parameters)
        {
            throw new InvalidOperationException($"Method {_reflectionInfo.Name} must be static to call this method. For invoke instance method, call 'Invoke'.");
        }


        private class CallMethodReflector : MethodReflector
        {
            public CallMethodReflector(MethodInfo reflectionInfo)
                : base(reflectionInfo)
            {
            }

            protected override Func<object, object[], object> CreateInvoker()
            {
                DynamicMethod dynamicMethod = new DynamicMethod($"invoker_{_displayName}",
                    typeof(object), new Type[] { typeof(object), typeof(object[]) }, _reflectionInfo.Module, true);

                ILGenerator ilGen = dynamicMethod.GetILGenerator();
                var parameterTypes = _reflectionInfo.GetParameterTypes();

                ilGen.EmitLoadArg(0);
                ilGen.EmitConvertFromObject(_reflectionInfo.DeclaringType);
                if (_reflectionInfo.DeclaringType.GetTypeInfo().IsValueType)
                {
                    var local = ilGen.DeclareLocal(_reflectionInfo.DeclaringType);
                    ilGen.Emit(OpCodes.Stloc, local);
                    ilGen.Emit(OpCodes.Ldloca, local);
                }

                if (parameterTypes.Length == 0)
                {
                    return CreateDelegate();
                }

                var refParameterCount = parameterTypes.Count(x => x.IsByRef);
                if (refParameterCount == 0)
                {
                    for (var i = 0; i < parameterTypes.Length; i++)
                    {
                        ilGen.EmitLoadArg(1);
                        ilGen.EmitInt(i);
                        ilGen.Emit(OpCodes.Ldelem_Ref);
                        ilGen.EmitConvertFromObject(parameterTypes[i]);
                    }

                    return CreateDelegate();
                }

                var indexedLocals = new IndexedLocalBuilder[refParameterCount];
                var index = 0;
                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    ilGen.EmitLoadArg(1);
                    ilGen.EmitInt(i);
                    ilGen.Emit(OpCodes.Ldelem_Ref);
                    if (parameterTypes[i].IsByRef)
                    {
                        var defType = parameterTypes[i].GetElementType();
                        var indexedLocal = new IndexedLocalBuilder(ilGen.DeclareLocal(defType), i);
                        indexedLocals[index++] = indexedLocal;
                        ilGen.EmitConvertFromObject(defType);
                        ilGen.Emit(OpCodes.Stloc, indexedLocal.LocalBuilder);
                        ilGen.Emit(OpCodes.Ldloca, indexedLocal.LocalBuilder);
                    }
                    else
                    {
                        ilGen.EmitConvertFromObject(parameterTypes[i]);
                    }
                }

                return CreateDelegate(() =>
                {
                    for (var i = 0; i < indexedLocals.Length; i++)
                    {
                        ilGen.EmitLoadArg(1);
                        ilGen.EmitInt(indexedLocals[i].Index);
                        ilGen.Emit(OpCodes.Ldloc, indexedLocals[i].LocalBuilder);
                        ilGen.EmitConvertToObject(indexedLocals[i].LocalType);
                        ilGen.Emit(OpCodes.Stelem_Ref);
                    }
                });

                Func<object, object[], object> CreateDelegate(Action callback = null)
                {
                    ilGen.EmitCall(OpCodes.Call, _reflectionInfo, null);
                    callback?.Invoke();
                    if (_reflectionInfo.ReturnType == typeof(void)) ilGen.Emit(OpCodes.Ldnull);
                    else if (_reflectionInfo.ReturnType.GetTypeInfo().IsValueType)
                        ilGen.EmitConvertToObject(_reflectionInfo.ReturnType);
                    ilGen.Emit(OpCodes.Ret);
                    return (Func<object, object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>));
                }
            }
        }

        private class OpenGenericMethodReflector : MethodReflector
        {
            public OpenGenericMethodReflector(MethodInfo reflectionInfo)
                : base(reflectionInfo)
            {
            }

            protected override Func<object, object[], object> CreateInvoker()
            {
                return null;
            }

            public override object Invoke(object instance, params object[] parameters)
            {
                throw new InvalidOperationException("Late bound operations cannot be performed on types or methods for which ContainsGenericParameters is true.");
            }

            public override object StaticInvoke(params object[] parameters)
            {
                throw new InvalidOperationException("Late bound operations cannot be performed on types or methods for which ContainsGenericParameters is true.");
            }
        }

        private class StaticMethodReflector : MethodReflector
        {
            public StaticMethodReflector(MethodInfo reflectionInfo)
                : base(reflectionInfo)
            {
            }

            protected override Func<object, object[], object> CreateInvoker()
            {
                DynamicMethod dynamicMethod = new DynamicMethod($"invoker_{_displayName}",
                    typeof(object), new Type[] { typeof(object), typeof(object[]) }, _reflectionInfo.Module, true);

                ILGenerator ilGen = dynamicMethod.GetILGenerator();
                var parameterTypes = _reflectionInfo.GetParameterTypes();

                if (parameterTypes.Length == 0)
                {
                    return CreateDelegate();
                }

                var refParameterCount = parameterTypes.Count(x => x.IsByRef);
                if (refParameterCount == 0)
                {
                    for (var i = 0; i < parameterTypes.Length; i++)
                    {
                        ilGen.EmitLoadArg(1);
                        ilGen.EmitInt(i);
                        ilGen.Emit(OpCodes.Ldelem_Ref);
                        ilGen.EmitConvertFromObject(parameterTypes[i]);
                    }

                    return CreateDelegate();
                }

                var indexedLocals = new IndexedLocalBuilder[refParameterCount];
                var index = 0;
                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    ilGen.EmitLoadArg(1);
                    ilGen.EmitInt(i);
                    ilGen.Emit(OpCodes.Ldelem_Ref);
                    if (parameterTypes[i].IsByRef)
                    {
                        var defType = parameterTypes[i].GetElementType();
                        var indexedLocal = new IndexedLocalBuilder(ilGen.DeclareLocal(defType), i);
                        indexedLocals[index++] = indexedLocal;
                        ilGen.EmitConvertFromObject(defType);
                        ilGen.Emit(OpCodes.Stloc, indexedLocal.LocalBuilder);
                        ilGen.Emit(OpCodes.Ldloca, indexedLocal.LocalBuilder);
                    }
                    else
                    {
                        ilGen.EmitConvertFromObject(parameterTypes[i]);
                    }
                }

                return CreateDelegate(() =>
                {
                    for (var i = 0; i < indexedLocals.Length; i++)
                    {
                        ilGen.EmitLoadArg(1);
                        ilGen.EmitInt(indexedLocals[i].Index);
                        ilGen.Emit(OpCodes.Ldloc, indexedLocals[i].LocalBuilder);
                        ilGen.EmitConvertToObject(indexedLocals[i].LocalType);
                        ilGen.Emit(OpCodes.Stelem_Ref);
                    }
                });

                Func<object, object[], object> CreateDelegate(Action callback = null)
                {
                    ilGen.EmitCall(OpCodes.Call, _reflectionInfo, null);
                    callback?.Invoke();
                    if (_reflectionInfo.ReturnType == typeof(void)) ilGen.Emit(OpCodes.Ldnull);
                    else if (_reflectionInfo.ReturnType.GetTypeInfo().IsValueType)
                        ilGen.EmitConvertToObject(_reflectionInfo.ReturnType);
                    ilGen.Emit(OpCodes.Ret);
                    return (Func<object, object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>));
                }
            }

            public override object Invoke(object instance, params object[] parameters)
            {
                return _invoker(null, parameters);
            }

            public override object StaticInvoke(params object[] parameters)
            {
                return _invoker(null, parameters);
            }
        }

        internal static MethodReflector Create(MethodInfo reflectionInfo, CallOptions callOption)
        {
            if (reflectionInfo == null)
            {
                throw new ArgumentNullException(nameof(reflectionInfo));
            }

            return ReflectorCacheUtils<ValueTuple<MethodInfo, CallOptions>, MethodReflector>.GetOrAdd( ValueTuple.Create(reflectionInfo, callOption), CreateInternal);

            MethodReflector CreateInternal(ValueTuple<MethodInfo, CallOptions> item)
            {
                var methodInfo = item.Item1;
                if (methodInfo.ContainsGenericParameters)
                {
                    return new OpenGenericMethodReflector(methodInfo);
                }

                if (methodInfo.IsStatic)
                {
                    return new StaticMethodReflector(methodInfo);
                }

                if (methodInfo.DeclaringType.GetTypeInfo().IsValueType || callOption == CallOptions.Call)
                {
                    return new CallMethodReflector(methodInfo);
                }

                return new MethodReflector(methodInfo);
            }
        }
    }
}