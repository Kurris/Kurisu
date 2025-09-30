﻿using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Kurisu.Aspect.Core.DynamicProxy;
using Kurisu.Aspect.DynamicProxy;
using Kurisu.Aspect.Reflection.Emit;
using Kurisu.Aspect.Reflection.Extensions;
using Kurisu.Aspect.Reflection.Reflectors;

namespace Kurisu.Aspect.Core.Utils;

internal class ProxyGeneratorUtils
{
    private readonly object _lock = new();

    const string _proxyNameSpace = "Aspect.DynamicGenerated";
    private const string _proxyAssemblyName = "Aspect.DynamicProxy.Generator";
    private readonly ModuleBuilder _moduleBuilder;
    private readonly Dictionary<string, Type> _definedTypes;

    private readonly ProxyNameUtils _proxyNameUtils;

    public static readonly ProxyGeneratorUtils Instance = new();

    private ProxyGeneratorUtils()
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(_proxyAssemblyName), AssemblyBuilderAccess.RunAndCollect);
        _moduleBuilder = assemblyBuilder.DefineDynamicModule("core");

        _definedTypes = new Dictionary<string, Type>();
        _proxyNameUtils = new ProxyNameUtils();
    }

    internal Type CreateInterfaceProxy(Type interfaceType, Type[] additionalInterfaces)
    {
        if (!interfaceType.GetTypeInfo().IsVisible() || !interfaceType.GetTypeInfo().IsInterface)
        {
            throw new InvalidOperationException($"Validate '{interfaceType}' failed because the type does not satisfy the visible conditions.");
        }

        lock (_lock)
        {
            var name = _proxyNameUtils.GetInterfaceImplTypeFullName(interfaceType);
            if (_definedTypes.TryGetValue(name, out var type))
            {
                return type;
            }

            type = CreateInterfaceImplInternal(name, interfaceType, additionalInterfaces);
            _definedTypes[name] = type;

            return type;
        }
    }

    internal Type CreateInterfaceProxy(Type interfaceType, Type implType, Type[] additionalInterfaces)
    {
        if (!interfaceType.GetTypeInfo().IsVisible() || !interfaceType.GetTypeInfo().IsInterface)
        {
            throw new InvalidOperationException($"Validate '{interfaceType}' failed because the type does not satisfy the visible conditions.");
        }

        lock (_lock)
        {
            var name = _proxyNameUtils.GetProxyTypeName(interfaceType, implType);
            if (_definedTypes.TryGetValue(name, out var type))
            {
                return type;
            }

            type = CreateInterfaceProxyInternal(name, interfaceType, implType, additionalInterfaces);
            _definedTypes[name] = type;

            return type;
        }
    }

    public Type CreateClassProxy(Type serviceType, Type implType, Type[] additionalInterfaces)
    {
        if (!serviceType.GetTypeInfo().IsVisible() || !serviceType.GetTypeInfo().IsClass)
        {
            throw new InvalidOperationException($"Validate '{serviceType}' failed because the type does not satisfy the visible conditions.");
        }

        if (!implType.GetTypeInfo().CanInherited())
        {
            throw new InvalidOperationException($"Validate '{implType}' failed because the type does not satisfy the condition to be inherited.");
        }

        lock (_lock)
        {
            var name = _proxyNameUtils.GetProxyTypeName(serviceType, implType);
            if (_definedTypes.TryGetValue(name, out Type type))
            {
                return type;
            }

            type = CreateClassProxyInternal(name, serviceType, implType, additionalInterfaces);
            _definedTypes[name] = type;

            return type;
        }
    }

    private Type CreateInterfaceImplInternal(string name, Type interfaceType, Type[] additionalInterfaces)
    {
        var interfaceTypes = new[] { interfaceType }.Concat(additionalInterfaces).Distinct().ToArray();
        var implTypeBuilder = _moduleBuilder.DefineType(name, TypeAttributes.Public, typeof(object), interfaceTypes)!;

        GenericParameterUtils.DefineGenericParameter(interfaceType, implTypeBuilder);

        ConstructorBuilderUtils.DefineInterfaceImplConstructor(implTypeBuilder);

        MethodBuilderUtils.DefineInterfaceImplMethods(interfaceTypes, implTypeBuilder);

        PropertyBuilderUtils.DefineInterfaceImplProperties(interfaceTypes, implTypeBuilder);

        var implType = implTypeBuilder.CreateTypeInfo()!.AsType();

        var typeDesc = TypeBuilderUtils.DefineType(_moduleBuilder, _proxyNameUtils.GetProxyTypeName(ProxyNameUtils.GetInterfaceImplTypeName(interfaceType), interfaceType, implType),
            interfaceType, typeof(object), interfaceTypes);

        //define constructor
        ConstructorBuilderUtils.DefineInterfaceProxyConstructor(interfaceType, implType, typeDesc);
        //define methods
        MethodBuilderUtils.DefineInterfaceProxyMethods(interfaceType, implType, additionalInterfaces, typeDesc);

        PropertyBuilderUtils.DefineInterfaceProxyProperties(interfaceType, implType, additionalInterfaces, typeDesc);

        return typeDesc.Compile();
    }

    private Type CreateInterfaceProxyInternal(string name, Type interfaceType, Type implType, Type[] additionalInterfaces)
    {
        var interfaces = new[] { interfaceType }.Concat(additionalInterfaces).Distinct().ToArray();

        var typeDesc = TypeBuilderUtils.DefineType(_moduleBuilder, name, interfaceType, typeof(object), interfaces);

        //define constructor
        ConstructorBuilderUtils.DefineInterfaceProxyConstructor(interfaceType, typeDesc);

        //define methods
        MethodBuilderUtils.DefineInterfaceProxyMethods(interfaceType, implType, additionalInterfaces, typeDesc);

        PropertyBuilderUtils.DefineInterfaceProxyProperties(interfaceType, implType, additionalInterfaces, typeDesc);

        return typeDesc.Compile();
    }

    private Type CreateClassProxyInternal(string name, Type serviceType, Type implType, Type[] additionalInterfaces)
    {
        var interfaces = additionalInterfaces.Distinct().ToArray();

        var typeDesc = TypeBuilderUtils.DefineType(_moduleBuilder, name, serviceType, implType, interfaces);

        TypeBuilderUtils.DefineTypeAttributes(implType, typeDesc);

        //define constructor
        ConstructorBuilderUtils.DefineClassProxyConstructors(serviceType, implType, typeDesc);

        //define methods
        MethodBuilderUtils.DefineClassProxyMethods(serviceType, implType, additionalInterfaces, typeDesc);

        PropertyBuilderUtils.DefineClassProxyProperties(serviceType, implType, additionalInterfaces, typeDesc);

        return typeDesc.Compile();
    }

    private class ProxyNameUtils
    {
        private readonly Dictionary<string, ProxyNameIndex> _indexes = new();
        private readonly Dictionary<Tuple<Type, Type>, string> _indexMaps = new();

        private string GetProxyTypeIndex(string className, Type serviceType, Type implementationType)
        {
            if (!_indexes.TryGetValue(className, out var nameIndex))
            {
                nameIndex = new ProxyNameIndex();
                _indexes[className] = nameIndex;
            }

            var key = Tuple.Create(serviceType, implementationType);
            if (_indexMaps.TryGetValue(key, out var index))
            {
                return index;
            }

            var tempIndex = nameIndex.GenIndex();
            index = tempIndex == 0 ? string.Empty : tempIndex.ToString();
            _indexMaps[key] = index;

            return index;
        }

        public static string GetInterfaceImplTypeName(Type interfaceType)
        {
            var className = interfaceType.GetReflector().DisplayName;
            if (className.StartsWith("I", StringComparison.Ordinal))
            {
                className = className.Substring(1);
            }

            return className /*+ "Impl"*/;
        }

        public string GetInterfaceImplTypeFullName(Type interfaceType)
        {
            var className = GetInterfaceImplTypeName(interfaceType);
            return $"{_proxyNameSpace}.{className}{GetProxyTypeIndex(className, interfaceType, interfaceType)}";
        }

        public string GetProxyTypeName(Type serviceType, Type implType)
        {
            return $"{_proxyNameSpace}.{implType.GetReflector().DisplayName}{GetProxyTypeIndex(implType.GetReflector().DisplayName, serviceType, implType)}";
        }

        public string GetProxyTypeName(string className, Type serviceType, Type implType)
        {
            return $"{_proxyNameSpace}.{className}{GetProxyTypeIndex(className, serviceType, implType)}";
        }
    }

    private class ProxyNameIndex
    {
        private int _index = -1;

        public int GenIndex()
        {
            return Interlocked.Increment(ref _index);
        }
    }

    private static class TypeBuilderUtils
    {
        public static TypeDesc DefineType(ModuleBuilder moduleBuilder, string name, Type serviceType, Type parentType, Type[] interfaces)
        {
            //define proxy type for interface service
            var typeBuilder = moduleBuilder.DefineType(name, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, parentType, interfaces);

            //define genericParameter
            GenericParameterUtils.DefineGenericParameter(serviceType, typeBuilder);

            //define default attribute
            typeBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(typeof(NonAspectAttribute)));
            typeBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(typeof(DynamicallyAttribute)));

            //define private field
            var fieldTable = FieldBuilderUtils.DefineFields(serviceType, typeBuilder);

            return new TypeDesc(serviceType, typeBuilder, fieldTable, new MethodConstantTable(typeBuilder));
        }

        public static void DefineTypeAttributes(Type implType, TypeDesc typeDesc)
        {
            //inherit implement type's attributes
            foreach (var customAttributeData in implType.CustomAttributes)
            {
                typeDesc.Builder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(customAttributeData));
            }
        }
    }

    private static class ConstructorBuilderUtils
    {
        internal static void DefineInterfaceImplConstructor(TypeBuilder typeBuilder)
        {
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, MethodUtils.ObjectCtor.CallingConvention, Type.EmptyTypes);
            var ilGen = constructorBuilder.GetILGenerator();
            ilGen.EmitThis();
            ilGen.Emit(OpCodes.Call, MethodUtils.ObjectCtor);
            ilGen.Emit(OpCodes.Ret);
        }

        internal static void DefineInterfaceProxyConstructor(Type interfaceType, Type implType, TypeDesc typeDesc)
        {
            var constructorBuilder = typeDesc.Builder.DefineConstructor(MethodAttributes.Public, MethodUtils.ObjectCtor.CallingConvention, new[] { typeof(IAspectExecutorFactory) });

            constructorBuilder.DefineParameter(1, ParameterAttributes.None, FieldBuilderUtils.ExecutorFactory);

            var ilGen = constructorBuilder.GetILGenerator();
            ilGen.EmitThis();
            ilGen.Emit(OpCodes.Call, MethodUtils.ObjectCtor);

            ilGen.EmitThis();
            ilGen.EmitLoadArgument(1);
            ilGen.Emit(OpCodes.Stfld, typeDesc.Fields[FieldBuilderUtils.ExecutorFactory]);

            ilGen.EmitThis();
            ilGen.Emit(OpCodes.Newobj, implType.GetTypeInfo().GetConstructor(Type.EmptyTypes)!);
            ilGen.Emit(OpCodes.Stfld, typeDesc.Fields[FieldBuilderUtils.Target]);

            ilGen.Emit(OpCodes.Ret);
        }

        internal static void DefineInterfaceProxyConstructor(Type interfaceType, TypeDesc typeDesc)
        {
            var constructorBuilder = typeDesc.Builder.DefineConstructor(MethodAttributes.Public, MethodUtils.ObjectCtor.CallingConvention, new[] { typeof(IAspectExecutorFactory), interfaceType });

            constructorBuilder.DefineParameter(1, ParameterAttributes.None, FieldBuilderUtils.ExecutorFactory);
            constructorBuilder.DefineParameter(2, ParameterAttributes.None, FieldBuilderUtils.Target);

            var ilGen = constructorBuilder.GetILGenerator();
            ilGen.EmitThis();
            ilGen.Emit(OpCodes.Call, MethodUtils.ObjectCtor);

            ilGen.EmitThis();
            ilGen.EmitLoadArgument(1);
            ilGen.Emit(OpCodes.Stfld, typeDesc.Fields[FieldBuilderUtils.ExecutorFactory]);

            ilGen.EmitThis();
            ilGen.EmitLoadArgument(2);
            ilGen.Emit(OpCodes.Stfld, typeDesc.Fields[FieldBuilderUtils.Target]);

            ilGen.Emit(OpCodes.Ret);
        }

        internal static void DefineClassProxyConstructors(Type serviceType, Type implType, TypeDesc typeDesc)
        {
            var constructors = implType.GetTypeInfo().DeclaredConstructors.Where(c => !c.IsStatic && (c.IsPublic || c.IsFamily || c.IsFamilyAndAssembly || c.IsFamilyOrAssembly)).ToArray();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"A suitable constructor for type {serviceType.FullName} could not be located. Ensure the type is concrete and services are registered for all parameters of a public constructor.");
            }

            foreach (var constructor in constructors)
            {
                var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
                var parameters = new[] { typeof(IAspectExecutorFactory) }.Concat(parameterTypes).ToArray();
                var constructorBuilder = typeDesc.Builder.DefineConstructor(constructor.Attributes, constructor.CallingConvention, parameters);

                constructorBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(typeof(DynamicallyAttribute)));

                //inherit constructor's attribute
                foreach (var customAttributeData in constructor.CustomAttributes)
                {
                    constructorBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(customAttributeData));
                }

                ParameterBuilderUtils.DefineParameters(constructor, constructorBuilder);

                var ilGen = constructorBuilder.GetILGenerator();

                ilGen.EmitThis();
                ilGen.EmitLoadArgument(1);
                ilGen.Emit(OpCodes.Stfld, typeDesc.Fields[FieldBuilderUtils.ExecutorFactory]);

                ilGen.EmitThis();
                ilGen.EmitThis();
                ilGen.Emit(OpCodes.Stfld, typeDesc.Fields[FieldBuilderUtils.Target]);

                ilGen.EmitThis();
                for (var i = 2; i <= parameters.Length; i++)
                {
                    ilGen.EmitLoadArgument(i);
                }

                ilGen.Emit(OpCodes.Call, constructor);

                ilGen.Emit(OpCodes.Ret);
            }
        }
    }

    private static class MethodBuilderUtils
    {
        private const MethodAttributes _explicitMethodAttributes = MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
        internal const MethodAttributes _interfaceMethodAttributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
        private const MethodAttributes _overrideMethodAttributes = MethodAttributes.HideBySig | MethodAttributes.Virtual;
        private static readonly HashSet<string> _ignores = new() { "Finalize" };

        internal static void DefineInterfaceImplMethods(Type[] interfaceTypes, TypeBuilder implTypeBuilder)
        {
            foreach (var item in interfaceTypes)
            {
                foreach (var method in item.GetTypeInfo().DeclaredMethods.Where(x => !x.IsPropertyBinding()))
                {
                    DefineInterfaceImplMethod(method, implTypeBuilder);
                }
            }
        }

        private static MethodBuilder DefineInterfaceImplMethod(MethodInfo method, TypeBuilder implTypeBuilder)
        {
            var methodBuilder = implTypeBuilder.DefineMethod(method.Name, _interfaceMethodAttributes, method.CallingConvention, method.ReturnType, method.GetParameterTypes());
            var ilGen = methodBuilder.GetILGenerator();
            if (method.ReturnType != typeof(void))
            {
                ilGen.EmitDefault(method.ReturnType);
            }

            ilGen.Emit(OpCodes.Ret);
            implTypeBuilder.DefineMethodOverride(methodBuilder, method);
            return methodBuilder;
        }

        internal static void DefineInterfaceProxyMethods(Type interfaceType, Type targetType, Type[] additionalInterfaces, TypeDesc typeDesc)
        {
            foreach (var method in interfaceType.GetTypeInfo().DeclaredMethods.Where(x => !x.IsPropertyBinding()))
            {
                DefineInterfaceMethod(method, targetType, typeDesc);
            }

            foreach (var item in additionalInterfaces)
            {
                foreach (var method in item.GetTypeInfo().DeclaredMethods.Where(x => !x.IsPropertyBinding()))
                {
                    DefineExplicitMethod(method, targetType, typeDesc);
                }
            }
        }

        internal static void DefineClassProxyMethods(Type serviceType, Type implType, Type[] additionalInterfaces, TypeDesc typeDesc)
        {
            foreach (var method in serviceType.GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => !x.IsPropertyBinding()))
            {
                if (method.IsVisibleAndVirtual() && !_ignores.Contains(method.Name))
                    DefineClassMethod(method, implType, typeDesc);
            }

            foreach (var item in additionalInterfaces)
            {
                foreach (var method in item.GetTypeInfo().DeclaredMethods.Where(x => !x.IsPropertyBinding()))
                {
                    DefineExplicitMethod(method, implType, typeDesc);
                }
            }
        }

        internal static MethodBuilder DefineInterfaceMethod(MethodInfo method, Type implType, TypeDesc typeDesc)
        {
            var methodBuilder = DefineMethod(method, method.Name, _interfaceMethodAttributes, implType, typeDesc);
            typeDesc.Builder.DefineMethodOverride(methodBuilder, method);
            return methodBuilder;
        }

        internal static MethodBuilder DefineExplicitMethod(MethodInfo method, Type implType, TypeDesc typeDesc)
        {
            var methodBuilder = DefineMethod(method, method.GetName(), _explicitMethodAttributes, implType, typeDesc);
            typeDesc.Builder.DefineMethodOverride(methodBuilder, method);
            return methodBuilder;
        }

        internal static MethodBuilder DefineClassMethod(MethodInfo method, Type implType, TypeDesc typeDesc)
        {
            var attributes = _overrideMethodAttributes;

            if (method.Attributes.HasFlag(MethodAttributes.Public))
            {
                attributes |= MethodAttributes.Public;
            }

            if (method.Attributes.HasFlag(MethodAttributes.Family))
            {
                attributes |= MethodAttributes.Family;
            }

            if (method.Attributes.HasFlag(MethodAttributes.FamORAssem))
            {
                attributes |= MethodAttributes.FamORAssem;
            }

            var methodBuilder = DefineMethod(method, method.Name, attributes, implType, typeDesc);
            return methodBuilder;
        }

        private static MethodBuilder DefineMethod(MethodInfo method, string name, MethodAttributes attributes, Type implType, TypeDesc typeDesc)
        {
            var methodBuilder = typeDesc.Builder.DefineMethod(name, attributes, method.CallingConvention, method.ReturnType, method.GetParameterTypes());

            GenericParameterUtils.DefineGenericParameter(method, methodBuilder);

            //define method attributes
            methodBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(typeof(DynamicallyAttribute)));

            //inherit targetMethod's attribute
            foreach (var customAttributeData in method.CustomAttributes)
            {
                methodBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(customAttributeData));
            }

            //define paramters
            ParameterBuilderUtils.DefineParameters(method, methodBuilder);

            var implementationMethod = implType.GetTypeInfo().GetMethodBySignature(method);
            if (implementationMethod == null)
            {
                var interfaces = implType.GetInterfaces();
                if (interfaces == null || interfaces.Length <= 0)
                {
                    throw new MissingMethodException($"Type '{implType}' does not contain a method '{method}'.");
                }

                var @interface = interfaces.Where(f => f.GetCustomAttribute(typeof(AbstractInterceptorAttribute)) != null).ToArray();
                if (@interface.Length > 0)
                {
                    foreach (var item in @interface)
                    {
                        implementationMethod = item.GetTypeInfo().GetMethodBySignature(method);
                        if (implementationMethod != null) break;
                    }
                }
                else
                {
                    foreach (var item in interfaces)
                    {
                        implementationMethod = item.GetTypeInfo().GetMethodBySignature(method);
                        if (implementationMethod != null) break;
                    }
                }

                if (implementationMethod == null)
                {
                    throw new MissingMethodException($"Type '{implType}' does not contain a method '{method}'.");
                }
            }

            if (method.IsNonAspect())
            {
                EmitMethodBody();
            }
            // else if (typeDesc.GetProperty<IAspectValidator>().Validate(method, true) || typeDesc.GetProperty<IAspectValidator>().Validate(implementationMethod, false))
            // {
            //     EmitProxyMethodBody();
            // }
            // else
            // {
            //     EmitMethodBody();
            // }
            else
            {
                EmitProxyMethodBody();
            }

            return methodBuilder;

            void EmitMethodBody()
            {
                var ilGen = methodBuilder.GetILGenerator();
                var parameters = method.GetParameterTypes();
                if (typeDesc.ServiceType.GetTypeInfo().IsInterface || !implementationMethod.IsExplicit())
                {
                    ilGen.EmitThis();
                    ilGen.Emit(OpCodes.Ldfld, typeDesc.Fields[FieldBuilderUtils.Target]);
                    for (int i = 1; i <= parameters.Length; i++)
                    {
                        ilGen.EmitLoadArgument(i);
                    }

                    var callOpCode = implementationMethod.IsCallvirt() ? OpCodes.Callvirt : OpCodes.Call;
                    ilGen.Emit(callOpCode, implementationMethod.IsExplicit() ? method : implementationMethod);
                }
                else
                {
                    var reflectorLocal = ilGen.DeclareLocal(typeof(MethodReflector));
                    var argsLocal = ilGen.DeclareLocal(typeof(object[]));
                    var returnLocal = ilGen.DeclareLocal(typeof(object));
                    ilGen.EmitMethod(implementationMethod);
                    ilGen.Emit(OpCodes.Call, MethodUtils.GetMethodReflector);
                    ilGen.Emit(OpCodes.Stloc, reflectorLocal);
                    ilGen.EmitInt(parameters.Length);
                    ilGen.Emit(OpCodes.Newarr, typeof(object));
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        ilGen.Emit(OpCodes.Dup);
                        ilGen.EmitInt(i);
                        ilGen.EmitLoadArgument(i + 1);
                        if (parameters[i].IsByRef)
                        {
                            ilGen.EmitLdRef(parameters[i]);
                            ilGen.EmitConvertToObject(parameters[i].GetElementType());
                        }
                        else
                        {
                            ilGen.EmitConvertToObject(parameters[i]);
                        }

                        ilGen.Emit(OpCodes.Stelem_Ref);
                    }

                    ilGen.Emit(OpCodes.Stloc, argsLocal);
                    ilGen.Emit(OpCodes.Ldloc, reflectorLocal);
                    ilGen.EmitThis();
                    ilGen.Emit(OpCodes.Ldloc, argsLocal);
                    ilGen.Emit(OpCodes.Callvirt, MethodUtils.ReflectorInvoke);
                    ilGen.Emit(OpCodes.Stloc, returnLocal);
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].IsByRef)
                        {
                            Type byrefToType = parameters[i].GetElementType();

                            ilGen.EmitLoadArgument(i + 1);
                            ilGen.Emit(OpCodes.Ldloc, argsLocal);
                            ilGen.EmitInt(i);
                            ilGen.Emit(OpCodes.Ldelem_Ref);
                            ilGen.EmitConvertFromObject(parameters[i].GetElementType());
                            ilGen.EmitStRef(byrefToType);
                        }
                    }

                    if (!method.IsVoid())
                    {
                        ilGen.Emit(OpCodes.Ldloc, returnLocal);
                        ilGen.EmitConvertFromObject(method.ReturnType);
                    }
                }

                ilGen.Emit(OpCodes.Ret);
            }

            void EmitProxyMethodBody()
            {
                var ilGen = methodBuilder.GetILGenerator();

                var activatorContext = ilGen.DeclareLocal(typeof(AspectExecutorContext));

                EmitInitializeMetaData(ilGen);

                ilGen.EmitNew(MethodUtils.AspectExecutorContextCtor);
                ilGen.Emit(OpCodes.Stloc, activatorContext);

                ilGen.EmitThis();
                ilGen.Emit(OpCodes.Ldfld, typeDesc.Fields[FieldBuilderUtils.ExecutorFactory]);
                ilGen.Emit(OpCodes.Callvirt, MethodUtils.CreateAspectExecutor);
                ilGen.Emit(OpCodes.Ldloc, activatorContext);

                EmitReturnValue(ilGen);
                var returnValue = default(LocalBuilder);
                if (method.ReturnType != typeof(void))
                {
                    returnValue = ilGen.DeclareLocal(method.ReturnType);
                    ilGen.Emit(OpCodes.Stloc, returnValue);
                }

                var parameterTypes = method.GetParameterTypes();

                if (parameterTypes.Any(x => x.IsByRef))
                {
                    var parameters = ilGen.DeclareLocal(typeof(object[]));
                    ilGen.Emit(OpCodes.Ldloca, activatorContext);
                    ilGen.Emit(OpCodes.Call, MethodUtils.GetParameters);
                    ilGen.Emit(OpCodes.Stloc, parameters);
                    for (var i = 0; i < parameterTypes.Length; i++)
                    {
                        if (parameterTypes[i].IsByRef)
                        {
                            Type byrefToType = parameterTypes[i].GetElementType();

                            ilGen.EmitLoadArgument(i + 1);
                            ilGen.Emit(OpCodes.Ldloc, parameters);
                            ilGen.EmitInt(i);
                            ilGen.Emit(OpCodes.Ldelem_Ref);
                            ilGen.EmitConvertFromObject(byrefToType);
                            ilGen.EmitStRef(byrefToType);
                        }
                    }
                }

                if (returnValue != null)
                {
                    ilGen.Emit(OpCodes.Ldloc, returnValue);
                }

                ilGen.Emit(OpCodes.Ret);
            }

            void EmitInitializeMetaData(ILGenerator ilGen)
            {
                var methodConstants = typeDesc.MethodConstants;

                if (method.IsGenericMethodDefinition)
                {
                    ilGen.EmitMethod(method.MakeGenericMethod(methodBuilder.GetGenericArguments()));
                    ilGen.EmitMethod(implementationMethod.MakeGenericMethod(methodBuilder.GetGenericArguments()));
                    ilGen.EmitMethod(methodBuilder.MakeGenericMethod(methodBuilder.GetGenericArguments()));
                }
                else
                {
                    methodConstants.AddMethod($"service{method.GetDisplayName()}", method);
                    methodConstants.AddMethod($"impl{implementationMethod.GetDisplayName()}", implementationMethod);
                    methodConstants.AddMethod($"proxy{method.GetDisplayName()}", methodBuilder);

                    methodConstants.LoadMethod(ilGen, $"service{method.GetDisplayName()}");
                    methodConstants.LoadMethod(ilGen, $"impl{implementationMethod.GetDisplayName()}");
                    methodConstants.LoadMethod(ilGen, $"proxy{method.GetDisplayName()}");
                }

                ilGen.EmitThis();
                ilGen.Emit(OpCodes.Ldfld, typeDesc.Fields[FieldBuilderUtils.Target]);
                ilGen.EmitThis();
                var parameterTypes = method.GetParameterTypes();
                if (parameterTypes.Length == 0)
                {
                    ilGen.Emit(OpCodes.Ldnull);
                    return;
                }

                ilGen.EmitInt(parameterTypes.Length);
                ilGen.Emit(OpCodes.Newarr, typeof(object));
                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    ilGen.Emit(OpCodes.Dup);
                    ilGen.EmitInt(i);
                    ilGen.EmitLoadArgument(i + 1);
                    if (parameterTypes[i].IsByRef)
                    {
                        ilGen.EmitLdRef(parameterTypes[i]);
                        ilGen.EmitConvertToObject(parameterTypes[i].GetElementType());
                    }
                    else
                    {
                        ilGen.EmitConvertToObject(parameterTypes[i]);
                    }

                    ilGen.Emit(OpCodes.Stelem_Ref);
                }
            }

            void EmitReturnValue(ILGenerator ilGen)
            {
                if (method.ReturnType == typeof(void))
                {
                    ilGen.Emit(OpCodes.Callvirt, MethodUtils.AspectActivatorInvoke.MakeGenericMethod(typeof(object)));
                    ilGen.Emit(OpCodes.Pop);
                }
                else if (method.ReturnType == typeof(Task))
                {
                    ilGen.Emit(OpCodes.Callvirt, MethodUtils.AspectActivatorInvokeTask.MakeGenericMethod(typeof(object)));
                }
                else if (method.IsReturnTask())
                {
                    var returnType = method.ReturnType.GetTypeInfo().GetGenericArguments().Single();
                    ilGen.Emit(OpCodes.Callvirt, MethodUtils.AspectActivatorInvokeTask.MakeGenericMethod(returnType));
                }
                else if (method.IsReturnValueTask())
                {
                    var returnType = method.ReturnType.GetTypeInfo().GetGenericArguments().Single();
                    ilGen.Emit(OpCodes.Callvirt, MethodUtils.AspectActivatorInvokeValueTask.MakeGenericMethod(returnType));
                }
                else if (method.ReturnType == typeof(ValueTask))
                {
                    ilGen.Emit(OpCodes.Callvirt, MethodUtils.AspectActivatorInvokeValueTask.MakeGenericMethod(typeof(object)));
                }
                else
                {
                    ilGen.Emit(OpCodes.Callvirt, MethodUtils.AspectActivatorInvoke.MakeGenericMethod(method.ReturnType));
                }
            }
        }
    }

    private static class PropertyBuilderUtils
    {
        public static void DefineInterfaceProxyProperties(Type interfaceType, Type implType, Type[] additionalInterfaces, TypeDesc typeDesc)
        {
            foreach (var property in interfaceType.GetTypeInfo().DeclaredProperties)
            {
                var builder = DefineInterfaceProxyProperty(property, property.Name, implType, typeDesc);
                DefineInterfacePropertyMethod(builder, property, implType, typeDesc);
            }

            foreach (var item in additionalInterfaces)
            {
                foreach (var property in item.GetTypeInfo().DeclaredProperties)
                {
                    var builder = DefineInterfaceProxyProperty(property, property.GetDisplayName(), implType, typeDesc);
                    DefineExplicitPropertyMethod(builder, property, implType, typeDesc);
                }
            }
        }

        internal static void DefineClassProxyProperties(Type serviceType, Type implType, Type[] additionalInterfaces, TypeDesc typeDesc)
        {
            foreach (var property in serviceType.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (property.IsVisibleAndVirtual())
                {
                    var builder = DefineInterfaceProxyProperty(property, property.Name, implType, typeDesc);
                    DefineClassPropertyMethod(builder, property, implType, typeDesc);
                }
            }

            foreach (var item in additionalInterfaces)
            {
                foreach (var property in item.GetTypeInfo().DeclaredProperties)
                {
                    var builder = DefineInterfaceProxyProperty(property, property.GetDisplayName(), implType, typeDesc);
                    DefineExplicitPropertyMethod(builder, property, implType, typeDesc);
                }
            }
        }

        private static void DefineClassPropertyMethod(PropertyBuilder propertyBuilder, PropertyInfo property, Type implType, TypeDesc typeDesc)
        {
            if (property.CanRead)
            {
                var method = MethodBuilderUtils.DefineClassMethod(property.GetMethod, implType, typeDesc);
                propertyBuilder.SetGetMethod(method);
            }

            if (property.CanWrite)
            {
                var method = MethodBuilderUtils.DefineClassMethod(property.SetMethod, implType, typeDesc);
                propertyBuilder.SetSetMethod(method);
            }
        }

        private static void DefineInterfacePropertyMethod(PropertyBuilder propertyBuilder, PropertyInfo property, Type implType, TypeDesc typeDesc)
        {
            if (property.CanRead)
            {
                var method = MethodBuilderUtils.DefineInterfaceMethod(property.GetMethod, implType, typeDesc);
                propertyBuilder.SetGetMethod(method);
            }

            if (property.CanWrite)
            {
                var method = MethodBuilderUtils.DefineInterfaceMethod(property.SetMethod, implType, typeDesc);
                propertyBuilder.SetSetMethod(method);
            }
        }

        private static void DefineExplicitPropertyMethod(PropertyBuilder propertyBuilder, PropertyInfo property, Type implType, TypeDesc typeDesc)
        {
            if (property.CanRead)
            {
                var method = MethodBuilderUtils.DefineExplicitMethod(property.GetMethod, implType, typeDesc);
                propertyBuilder.SetGetMethod(method);
            }

            // ReSharper disable once InvertIf
            if (property.CanWrite)
            {
                var method = MethodBuilderUtils.DefineExplicitMethod(property.SetMethod, implType, typeDesc);
                propertyBuilder.SetSetMethod(method);
            }
        }

        private static PropertyBuilder DefineInterfaceProxyProperty(PropertyInfo property, string name, Type implType, TypeDesc typeDesc)
        {
            var propertyBuilder = typeDesc.Builder.DefineProperty(name, property.Attributes, property.PropertyType, Type.EmptyTypes);

            propertyBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(typeof(DynamicallyAttribute)));

            //inherit targetMethod's attribute
            foreach (var customAttributeData in property.CustomAttributes)
            {
                propertyBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(customAttributeData));
            }

            return propertyBuilder;
        }

        internal static void DefineInterfaceImplProperties(Type[] interfaceTypes, TypeBuilder implTypeBuilder)
        {
            foreach (var item in interfaceTypes)
            {
                foreach (var property in item.GetTypeInfo().DeclaredProperties)
                {
                    DefineInterfaceImplProperty(property, implTypeBuilder);
                }
            }
        }

        private static void DefineInterfaceImplProperty(PropertyInfo property, TypeBuilder implTypeBuilder)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            var propertyBuilder = implTypeBuilder.DefineProperty(property.Name, property.Attributes, property.PropertyType, Type.EmptyTypes);
            var field = implTypeBuilder.DefineField($"<{property.Name}>k__BackingField", property.PropertyType, FieldAttributes.Private);
            if (property.CanRead)
            {
                var methodBuilder = implTypeBuilder.DefineMethod(property.GetMethod!.Name, MethodBuilderUtils._interfaceMethodAttributes, property.GetMethod.CallingConvention, property.GetMethod.ReturnType, property.GetMethod.GetParameterTypes());
                var ilGen = methodBuilder.GetILGenerator();
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, field);
                ilGen.Emit(OpCodes.Ret);
                implTypeBuilder.DefineMethodOverride(methodBuilder, property.GetMethod);
                propertyBuilder.SetGetMethod(methodBuilder);
            }

            if (property.CanWrite)
            {
                var methodBuilder = implTypeBuilder.DefineMethod(property.SetMethod!.Name, MethodBuilderUtils._interfaceMethodAttributes, property.SetMethod.CallingConvention, property.SetMethod.ReturnType, property.SetMethod.GetParameterTypes());
                var ilGen = methodBuilder.GetILGenerator();
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldarg_1);
                ilGen.Emit(OpCodes.Stfld, field);
                ilGen.Emit(OpCodes.Ret);
                implTypeBuilder.DefineMethodOverride(methodBuilder, property.SetMethod);
                propertyBuilder.SetSetMethod(methodBuilder);
            }

            foreach (var customAttributeData in property.CustomAttributes)
            {
                propertyBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(customAttributeData));
            }
        }
    }

    private static class ParameterBuilderUtils
    {
        public static void DefineParameters(MethodInfo targetMethod, MethodBuilder methodBuilder)
        {
            var parameters = targetMethod.GetParameters();
            if (parameters.Length > 0)
            {
                const int paramOffset = 1; // 1
                foreach (var parameter in parameters)
                {
                    var parameterBuilder = methodBuilder.DefineParameter(parameter.Position + paramOffset, parameter.Attributes, parameter.Name);
                    // if (parameter.HasDefaultValue) // parameter.HasDefaultValue will throw a FormatException when parameter is DateTime type with default value
                    if (parameter.HasDefaultValueByAttributes())
                    {
                        // if (!(parameter.ParameterType.GetTypeInfo().IsValueType && parameter.DefaultValue == null))          // we can comment above line safely, and CopyDefaultValueConstant will handle this case.
                        // parameter.DefaultValue will throw a FormatException when parameter is DateTime type with default value
                        {
                            // parameterBuilder.SetConstant(parameter.DefaultValue);
                            try
                            {
                                CopyDefaultValueConstant(from: parameter, to: parameterBuilder);
                            }
                            catch
                            {
                                // Default value replication is a nice-to-have feature but not essential,
                                // so if it goes wrong for one parameter, just continue.
                            }
                        }
                    }

                    parameterBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(typeof(DynamicallyAttribute)));
                    foreach (var attribute in parameter.CustomAttributes)
                    {
                        parameterBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(attribute));
                    }
                }
            }

            var returnParameter = targetMethod.ReturnParameter;
            var returnParameterBuilder = methodBuilder.DefineParameter(0, returnParameter!.Attributes, returnParameter.Name);
            returnParameterBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(typeof(DynamicallyAttribute)));
            foreach (var attribute in returnParameter.CustomAttributes)
            {
                returnParameterBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(attribute));
            }
        }

        // Code from https://github.com/castleproject/Core/blob/master/src/Castle.Core/DynamicProxy/Generators/Emitters/MethodEmitter.cs
        private static void CopyDefaultValueConstant(ParameterInfo from, ParameterBuilder to)
        {
            object defaultValue;
            try
            {
                defaultValue = from.DefaultValue;
            }
            catch (FormatException) when (from.ParameterType == typeof(DateTime))
            {
                // This catch clause guards against a CLR bug that makes it impossible to query
                // the default value of an optional DateTime parameter. For the CoreCLR, see
                // https://github.com/dotnet/corefx/issues/26164.

                // If this bug is present, it is caused by a `null` default value:
                defaultValue = null;
            }
            catch (FormatException) when (from.ParameterType.IsEnum)
            {
                // This catch clause guards against a CLR bug that makes it impossible to query
                // the default value of a (closed generic) enum parameter. For the CoreCLR, see
                // https://github.com/dotnet/corefx/issues/29570.

                // If this bug is present, it is caused by a `null` default value:
                defaultValue = null;
            }

            if (defaultValue is Missing)
            {
                // It is likely that we are reflecting over invalid metadata if we end up here.
                // At this point, `to.Attributes` will have the `HasDefault` flag set. If we do
                // not call `to.SetConstant`, that flag will be reset when creating the dynamic
                // type, so `to` will at least end up having valid metadata. It is quite likely
                // that the `Missing.Value` will still be reproduced because the `Parameter-
                // Builder`'s `ParameterAttributes.Optional` is likely set. (If it isn't set,
                // we'll be causing a default value of `DBNull.Value`, but there's nothing that
                // can be done about that, short of recreating a new `ParameterBuilder`.)
                return;
            }

            try
            {
                to.SetConstant(defaultValue);
            }
            catch (ArgumentException)
            {
                var parameterType = from.ParameterType;
                var parameterNonNullableType = parameterType;
                var isNullableType = parameterType.IsNullableType();

                if (defaultValue == null)
                {
                    if (isNullableType)
                    {
                        // This guards against a Mono bug that prohibits setting default value `null`
                        // for a `Nullable<T>` parameter. See https://github.com/mono/mono/issues/8504.
                        //
                        // If this bug is present, luckily we still get `null` as the default value if
                        // we do nothing more (which is probably itself yet another bug, as the CLR
                        // would "produce" a default value of `Missing.Value` in this situation).
                        return;
                    }
                    else if (parameterType.IsValueType)
                    {
                        // This guards against a CLR bug that prohibits replicating `null` default
                        // values for non-nullable value types (which, despite the apparent type
                        // mismatch, is perfectly legal and something that the Roslyn compilers do).
                        // For the CoreCLR, see https://github.com/dotnet/corefx/issues/26184.

                        // If this bug is present, the best we can do is to not set the default value.
                        // This will cause a default value of `Missing.Value` (if `ParameterAttributes-
                        // .Optional` is set) or `DBNull.Value` (otherwise, unlikely).
                        return;
                    }
                }
                else if (isNullableType)
                {
                    parameterNonNullableType = from.ParameterType.GetGenericArguments()[0];
                    if (parameterNonNullableType.IsEnum || parameterNonNullableType.IsInstanceOfType(defaultValue))
                    {
                        // This guards against two bugs:
                        //
                        // * On the CLR and CoreCLR, a bug that makes it impossible to use `ParameterBuilder-
                        //   .SetConstant` on parameters of a nullable enum type. For CoreCLR, see
                        //   https://github.com/dotnet/coreclr/issues/17893.
                        //
                        //   If this bug is present, there is no way to faithfully reproduce the default
                        //   value. This will most likely cause a default value of `Missing.Value` or
                        //   `DBNull.Value`. (To better understand which of these, see comment above).
                        //
                        // * On Mono, a bug that performs a too-strict type check for nullable types. The
                        //   value passed to `ParameterBuilder.SetConstant` must have a type matching that
                        //   of the parameter precisely. See https://github.com/mono/mono/issues/8597.
                        //
                        //   If this bug is present, there's no way to reproduce the default value because
                        //   we cannot actually create a value of type `Nullable<>`.
                        return;
                    }
                }

                // Finally, we might have got here because the metadata constant simply doesn't match
                // the parameter type exactly. Some code generators other than the .NET compilers
                // might produce such metadata. Make a final attempt to coerce it to the required type:
                try
                {
                    var coercedDefaultValue = Convert.ChangeType(defaultValue, parameterNonNullableType, CultureInfo.InvariantCulture);
                    to.SetConstant(coercedDefaultValue);

                    return;
                }
                catch
                {
                    // We don't care about the error thrown by an unsuccessful type coercion.
                }

                throw;
            }
        }

        internal static void DefineParameters(ConstructorInfo constructor, ConstructorBuilder constructorBuilder)
        {
            constructorBuilder.DefineParameter(1, ParameterAttributes.None, "aspectContextFactory");
            var parameters = constructor.GetParameters();
            if (parameters.Length > 0)
            {
                var paramOffset = 2; //ParameterTypes.Length - parameters.Length + 1
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var parameterBuilder = constructorBuilder.DefineParameter(i + paramOffset, parameter.Attributes, parameter.Name);
                    if (parameter.HasDefaultValue)
                    {
                        parameterBuilder.SetConstant(parameter.DefaultValue);
                    }

                    parameterBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(typeof(DynamicallyAttribute)));
                    foreach (var attribute in parameter.CustomAttributes)
                    {
                        parameterBuilder.SetCustomAttribute(CustomAttributeBuilderUtils.DefineCustomAttribute(attribute));
                    }
                }
            }
        }
    }

    private static class GenericParameterUtils
    {
        internal static void DefineGenericParameter(Type targetType, TypeBuilder typeBuilder)
        {
            if (!targetType.GetTypeInfo().IsGenericTypeDefinition)
            {
                return;
            }

            var genericArguments = targetType.GetTypeInfo().GetGenericArguments().Select(t => t.GetTypeInfo()).ToArray();
            var genericArgumentsBuilders = typeBuilder.DefineGenericParameters(genericArguments.Select(a => a.Name).ToArray());
            for (var index = 0; index < genericArguments.Length; index++)
            {
                genericArgumentsBuilders[index].SetGenericParameterAttributes(ToClassGenericParameterAttributes(genericArguments[index].GenericParameterAttributes));
                foreach (var constraint in genericArguments[index].GetGenericParameterConstraints().Select(t => t.GetTypeInfo()))
                {
                    if (constraint.IsClass) genericArgumentsBuilders[index].SetBaseTypeConstraint(constraint.AsType());
                    if (constraint.IsInterface) genericArgumentsBuilders[index].SetInterfaceConstraints(constraint.AsType());
                }
            }
        }

        internal static void DefineGenericParameter(MethodInfo tergetMethod, MethodBuilder methodBuilder)
        {
            if (!tergetMethod.IsGenericMethod)
            {
                return;
            }

            var genericArguments = tergetMethod.GetGenericArguments().Select(t => t.GetTypeInfo()).ToArray();
            var genericArgumentsBuilders = methodBuilder.DefineGenericParameters(genericArguments.Select(a => a.Name).ToArray());
            for (var index = 0; index < genericArguments.Length; index++)
            {
                genericArgumentsBuilders[index].SetGenericParameterAttributes(genericArguments[index].GenericParameterAttributes);
                foreach (var constraint in genericArguments[index].GetGenericParameterConstraints().Select(t => t.GetTypeInfo()))
                {
                    if (constraint.IsClass) genericArgumentsBuilders[index].SetBaseTypeConstraint(constraint.AsType());
                    if (constraint.IsInterface) genericArgumentsBuilders[index].SetInterfaceConstraints(constraint.AsType());
                }
            }
        }

        private static GenericParameterAttributes ToClassGenericParameterAttributes(GenericParameterAttributes attributes)
        {
            if (attributes == GenericParameterAttributes.None)
            {
                return GenericParameterAttributes.None;
            }

            if (attributes.HasFlag(GenericParameterAttributes.SpecialConstraintMask))
            {
                return GenericParameterAttributes.SpecialConstraintMask;
            }

            if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            {
                return GenericParameterAttributes.NotNullableValueTypeConstraint;
            }

            if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) && attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
            {
                return GenericParameterAttributes.ReferenceTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint;
            }

            if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
            {
                return GenericParameterAttributes.ReferenceTypeConstraint;
            }

            return attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)
                ? GenericParameterAttributes.DefaultConstructorConstraint
                : GenericParameterAttributes.None;
        }
    }

    private static class CustomAttributeBuilderUtils
    {
        public static CustomAttributeBuilder DefineCustomAttribute(Type attributeType)
        {
            return new CustomAttributeBuilder(attributeType.GetTypeInfo()!.GetConstructor(Type.EmptyTypes)!, Array.Empty<object>());
        }

        public static CustomAttributeBuilder DefineCustomAttribute(CustomAttributeData customAttributeData)
        {
            if (customAttributeData.NamedArguments != null)
            {
                var attributeTypeInfo = customAttributeData.AttributeType.GetTypeInfo();
                //var constructor = customAttributeData.Constructor;
                //var constructorArgs = customAttributeData.ConstructorArguments.Select(c => c.Value).ToArray();
                var constructorArgs = customAttributeData.ConstructorArguments
                    .Select(ReadAttributeValue)
                    .ToArray();
                var namedProperties = customAttributeData.NamedArguments
                    .Where(n => !n.IsField)
                    .Select(n => attributeTypeInfo.GetProperty(n.MemberName))
                    .ToArray();
                var propertyValues = customAttributeData.NamedArguments
                    .Where(n => !n.IsField)
                    .Select(n => ReadAttributeValue(n.TypedValue))
                    .ToArray();
                var namedFields = customAttributeData.NamedArguments.Where(n => n.IsField)
                    .Select(n => attributeTypeInfo.GetField(n.MemberName))
                    .ToArray();
                var fieldValues = customAttributeData.NamedArguments.Where(n => n.IsField)
                    .Select(n => ReadAttributeValue(n.TypedValue))
                    .ToArray();
                return new CustomAttributeBuilder(customAttributeData.Constructor, constructorArgs
                    , namedProperties
                    , propertyValues, namedFields, fieldValues);
            }
            else
            {
                return new CustomAttributeBuilder(customAttributeData.Constructor,
                    customAttributeData.ConstructorArguments.Select(c => c.Value).ToArray());
            }
        }

        private static object ReadAttributeValue(CustomAttributeTypedArgument argument)
        {
            var value = argument.Value;
            if (argument.ArgumentType.GetTypeInfo().IsArray == false)
            {
                return value;
            }

            //special case for handling arrays in attributes
            //the actual type of "value" is ReadOnlyCollection<CustomAttributeTypedArgument>.
            var arguments = ((IEnumerable<CustomAttributeTypedArgument>)value!)
                .Select(m => m.Value)
                .ToArray();

            return arguments;
        }
    }

    private static class FieldBuilderUtils
    {
        public const string ExecutorFactory = "_executorFactory";
        public const string Target = "_implementation";

        public static FieldTable DefineFields(Type targetType, TypeBuilder typeBuilder)
        {
            var fieldTable = new FieldTable
            {
                [ExecutorFactory] = typeBuilder.DefineField(ExecutorFactory, typeof(IAspectExecutorFactory), FieldAttributes.Private),
                [Target] = typeBuilder.DefineField(Target, targetType, FieldAttributes.Private)
            };
            return fieldTable;
        }
    }

    private class FieldTable
    {
        private readonly Dictionary<string, FieldBuilder> _table = new();

        public FieldBuilder this[string fieldName]
        {
            get { return _table[fieldName]; }
            init { _table[value.Name] = value; }
        }
    }

    private class MethodConstantTable
    {
        private readonly TypeBuilder _nestedTypeBuilder;
        private readonly ILGenerator _ilGen;
        private readonly Dictionary<string, FieldBuilder> _fields;

        public MethodConstantTable(TypeBuilder typeBuilder)
        {
            _fields = new Dictionary<string, FieldBuilder>();
            _nestedTypeBuilder = typeBuilder.DefineNestedType("MethodConstant", TypeAttributes.NestedPrivate);
            _ilGen = _nestedTypeBuilder.DefineTypeInitializer().GetILGenerator();
        }

        public void AddMethod(string name, MethodInfo method)
        {
            if (_fields.ContainsKey(name))
            {
                return;
            }

            var field = _nestedTypeBuilder.DefineField(name, typeof(MethodInfo), FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.Assembly);
            _fields.Add(name, field);
            if (method == null)
            {
                return;
            }

            _ilGen.EmitMethod(method);
            _ilGen.Emit(OpCodes.Stsfld, field);
        }

        public void LoadMethod(ILGenerator ilGen, string name)
        {
            if (!_fields.TryGetValue(name, out FieldBuilder field))
            {
                throw new InvalidOperationException($"Failed to find the method associated with the specified key {name}.");
            }

            ilGen.Emit(OpCodes.Ldsfld, field);
        }

        public void Compile()
        {
            _ilGen.Emit(OpCodes.Ret);
            _nestedTypeBuilder.CreateTypeInfo();
        }
    }

    private class TypeDesc
    {
        public TypeBuilder Builder { get; }

        public FieldTable Fields { get; }

        public MethodConstantTable MethodConstants { get; }

        public Type ServiceType { get; }

        public TypeDesc(Type serviceType, TypeBuilder typeBuilder, FieldTable fields, MethodConstantTable methodConstants)
        {
            ServiceType = serviceType;
            Builder = typeBuilder ?? throw new ArgumentNullException(nameof(typeBuilder));
            Fields = fields;
            MethodConstants = methodConstants;
        }

        public Type Compile()
        {
            MethodConstants.Compile();
            return Builder.CreateTypeInfo()!.AsType();
        }
    }
}