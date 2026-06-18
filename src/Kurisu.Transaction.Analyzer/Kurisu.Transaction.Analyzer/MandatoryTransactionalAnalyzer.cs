using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kurisu.Transaction.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MandatoryTransactionalAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "KS1001";
        private static readonly LocalizableString Title = "Mandatory transaction propagation requires an ambient transactional method on the call chain";
        private static readonly LocalizableString MessageFormat = "使用了 Propagation.Mandatory 标记的方法 '{0}' 必须在调用链上存在标注 [Transactional] 的方法";
        private static readonly LocalizableString Description = "Methods annotated with Transactional(Propagation = Propagation.Mandatory) require that the caller chain contains a method annotated with Transactional.";
        private const string Category = "Correctness";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        // 编译级别的缓存类，避免静态字典跨会话泄漏
        private sealed class AnalyzerCache
        {
            public ConcurrentDictionary<IMethodSymbol, bool?> HasMandatoryCache { get; }
                = new(SymbolEqualityComparer.Default);

            public ConcurrentDictionary<IMethodSymbol, bool?> HasTransactionalCache { get; }
                = new(SymbolEqualityComparer.Default);

            // 缓存接口实现查找结果（包括 null，表示已查找但无结果），避免重复查找
            public ConcurrentDictionary<(INamedTypeSymbol, INamedTypeSymbol, IMethodSymbol), IMethodSymbol?> InterfaceImplementationCache { get; }
                = new(new InterfaceImplementationComparer());

            // 缓存接口方法到实现类方法的映射查找
            public ConcurrentDictionary<IMethodSymbol, ImmutableArray<IMethodSymbol>> InterfaceToImplementationsCache { get; }
                = new(SymbolEqualityComparer.Default);

            // 存储当前编译的所有类型，延迟初始化
            private ImmutableArray<INamedTypeSymbol>? _allTypes;
            public ImmutableArray<INamedTypeSymbol> AllTypes
            {
                get => _allTypes ?? ImmutableArray<INamedTypeSymbol>.Empty;
                set => _allTypes = value;
            }

            // 接口 → 实现类索引，加速 FindImplementationsOfInterfaceMethod 查找（延迟初始化）
            private Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>? _interfaceToTypesIndex;
            public Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> InterfaceToTypesIndex
            {
                get
                {
                    if (_interfaceToTypesIndex == null)
                    {
                        _interfaceToTypesIndex = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
                        foreach (var type in AllTypes)
                        {
                            if (type.TypeKind != TypeKind.Class) continue;
                            foreach (var iface in type.AllInterfaces)
                            {
                                if (!_interfaceToTypesIndex.TryGetValue(iface, out var list))
                                {
                                    list = new List<INamedTypeSymbol>();
                                    _interfaceToTypesIndex[iface] = list;
                                }
                                list.Add(type);
                            }
                        }
                    }
                    return _interfaceToTypesIndex;
                }
            }
        }

        // 用于缓存键的相等比较器
        private sealed class InterfaceImplementationComparer : IEqualityComparer<(INamedTypeSymbol type, INamedTypeSymbol iface, IMethodSymbol method)>
        {
            public bool Equals((INamedTypeSymbol type, INamedTypeSymbol iface, IMethodSymbol method) x,
                             (INamedTypeSymbol type, INamedTypeSymbol iface, IMethodSymbol method) y)
            {
                return SymbolEqualityComparer.Default.Equals(x.type, y.type)
                    && SymbolEqualityComparer.Default.Equals(x.iface, y.iface)
                    && SymbolEqualityComparer.Default.Equals(x.method, y.method);
            }

            public int GetHashCode((INamedTypeSymbol type, INamedTypeSymbol iface, IMethodSymbol method) obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(obj.type);
                    hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(obj.iface);
                    hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(obj.method);
                    return hash;
                }
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // 使用 CompilationStartAction 创建编译级别缓存
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var cache = new AnalyzerCache();

                // 延迟初始化所有类型，仅在需要时收集
                var allTypesInitialized = false;
                void EnsureAllTypesInitialized()
                {
                    if (!allTypesInitialized)
                    {
                        var types = GetAllTypesInCompilation(compilationContext.Compilation);
                        cache.AllTypes = types;
                        allTypesInitialized = true;
                    }
                }

                compilationContext.RegisterOperationAction(
                    operationContext =>
                    {
                        // 仅在需要时初始化所有类型
                        EnsureAllTypesInitialized();
                        AnalyzeInvocation(operationContext, cache);
                    },
                    OperationKind.Invocation);
            });
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context, AnalyzerCache cache)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var targetMethod = invocation.TargetMethod;
            if (targetMethod == null) return;

            // 检查被调用方法是否要求 Mandatory
            if (!HasTransactionalWithMandatory(targetMethod, cache))
                return;

            // 通过 Operation 向上查找，检查整个调用链
            if (EnclosingChainHasTransactional(invocation, cache))
                return;

            // 报告诊断
            var diagnostic = Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), targetMethod.Name);
            context.ReportDiagnostic(diagnostic);
        }

        // 优化的链式缓存，简化接口检查，一直向上检查接口层次（有深度限制）
        private static bool HasTransactionalWithMandatory(IMethodSymbol method, AnalyzerCache cache)
        {
            if (method == null) return false;

            if (cache.HasMandatoryCache.TryGetValue(method, out var cached) && cached.HasValue)
                return cached.Value;

            bool result = HasTransactionalAttributeIncludingInterfaces(method, cache, HasTransactionalWithMandatory_Self, depth: 0);
            cache.HasMandatoryCache[method] = result;
            return result;
        }

        // 检查方法是否有合格的 Transactional（包括接口实现的检查）
        private static bool HasTransactionalWithoutPropagation(IMethodSymbol method, AnalyzerCache cache)
        {
            if (method == null) return false;

            if (cache.HasTransactionalCache.TryGetValue(method, out var cached) && cached.HasValue)
                return cached.Value;

            bool result = HasTransactionalAttributeIncludingInterfaces(method, cache, HasTransactionalWithoutPropagation_Self, depth: 0);
            cache.HasTransactionalCache[method] = result;
            return result;
        }

        // 统一的递归遍历：检查方法及其接口实现/重写链是否满足 selfCheck，一直向上（有深度限制）
        private static bool HasTransactionalAttributeIncludingInterfaces(
            IMethodSymbol method,
            AnalyzerCache cache,
            Func<IMethodSymbol, bool> selfCheck,
            int depth)
        {
            const int MaxRecursionDepth = 10; // 防止无限递归深度

            if (method == null || depth > MaxRecursionDepth)
                return false;

            // 1) 检查方法本身的 attribute
            if (selfCheck(method))
                return true;

            // 2) 检查显式接口实现
            foreach (var ei in method.ExplicitInterfaceImplementations)
            {
                if (selfCheck(ei))
                    return true;

                // 递归查接口的父接口
                if (HasTransactionalAttributeIncludingInterfaces(ei, cache, selfCheck, depth + 1))
                    return true;
            }

            // 3) 检查隐式接口实现（通过类方法查找）
            if (method.ContainingType != null && method.ContainingType.TypeKind == TypeKind.Class)
            {
                var containingType = method.ContainingType;
                foreach (var iface in containingType.AllInterfaces)
                {
                    var interfaceMethod = SafeFindImplementation(containingType, iface, method, cache);
                    if (interfaceMethod != null)
                    {
                        if (selfCheck(interfaceMethod))
                            return true;

                        // 递归查接口方法的父接口
                        if (HasTransactionalAttributeIncludingInterfaces(interfaceMethod, cache, selfCheck, depth + 1))
                            return true;
                    }
                }
            }

            // 4) 如果当前方法本身是接口方法，查找当前编译单元中的所有实现类
            if (method.ContainingType?.TypeKind == TypeKind.Interface)
            {
                var implementations = FindImplementationsOfInterfaceMethod(method, cache);
                foreach (var impl in implementations)
                {
                    if (selfCheck(impl))
                        return true;

                    // 递归查实现类方法（可能会实现多个接口）
                    if (HasTransactionalAttributeIncludingInterfaces(impl, cache, selfCheck, depth + 1))
                        return true;
                }
            }

            // 5) 检查被重写的基类方法
            if (method.OverriddenMethod != null)
            {
                if (HasTransactionalAttributeIncludingInterfaces(method.OverriddenMethod, cache, selfCheck, depth + 1))
                    return true;
            }

            return false;
        }

        // 安全地查找接口实现，包含缓存以避免重复计算
        private static IMethodSymbol? SafeFindImplementation(INamedTypeSymbol type, INamedTypeSymbol iface, IMethodSymbol method, AnalyzerCache cache)
        {
            if (type == null || iface == null || method == null)
                return null;

            var cacheKey = (type, iface, method);

            if (cache.InterfaceImplementationCache.TryGetValue(cacheKey, out var cachedResult))
                return cachedResult;

            IMethodSymbol? result = null;

            try
            {
                // 只检查当前接口的成员（不包括父接口）
                foreach (var ifaceMember in iface.GetMembers().OfType<IMethodSymbol>())
                {
                    // 先按方法名匹配，再做深度检查
                    if (ifaceMember.Name != method.Name)
                        continue;

                    var impl = type.FindImplementationForInterfaceMember(ifaceMember) as IMethodSymbol;
                    if (impl != null && SymbolEqualityComparer.Default.Equals(impl.OriginalDefinition, method.OriginalDefinition))
                    {
                        result = ifaceMember;
                        break;
                    }
                }
            }
            catch
            {
                // 忽略异常
            }

            // 缓存查找结果（包括 null）
            cache.InterfaceImplementationCache[cacheKey] = result;
            return result;
        }

        // 仅在目标方法上检查是否存在 Propagation=Mandatory 的 Transactional 特性
        private static bool HasTransactionalWithMandatory_Self(IMethodSymbol method)
        {
            return CheckTransactionalAttribute(method, checkMandatory: true);
        }

        // 仅检查方法本身的 Transactional 特性（不含 Propagation=Mandatory）
        private static bool HasTransactionalWithoutPropagation_Self(IMethodSymbol method)
        {
            return CheckTransactionalAttribute(method, checkMandatory: false);
        }

        // 统一的 Transactional 特性检查，合并了 Mandatory 和非 Mandatory 两种场景
        private static bool CheckTransactionalAttribute(IMethodSymbol method, bool checkMandatory)
        {
            if (method == null) return false;

            foreach (var attr in method.GetAttributes())
            {
                var attrClass = attr.AttributeClass;
                if (attrClass == null || !attrClass.Name.Contains("Transactional"))
                    continue;

                if (checkMandatory)
                {
                    // 检查是否存在 Propagation.Mandatory（构造函数参数或命名参数）
                    if (attr.ConstructorArguments.Any(IsMandatoryPropagationTypedConstant))
                        return true;

                    foreach (var na in attr.NamedArguments)
                    {
                        if (na.Key.Equals("Propagation", StringComparison.OrdinalIgnoreCase)
                            && IsMandatoryPropagationTypedConstant(na.Value))
                            return true;
                    }
                }
                else
                {
                    // 检查命名参数 Propagation — 非 Mandatory 即为合格
                    foreach (var na in attr.NamedArguments)
                    {
                        if (na.Key.Equals("Propagation", StringComparison.OrdinalIgnoreCase))
                            return !IsMandatoryPropagationTypedConstant(na.Value);
                    }

                    // 检查构造函数参数
                    if (attr.ConstructorArguments.Any(IsMandatoryPropagationTypedConstant))
                        return false;

                    // 未显式指定 Propagation，默认行为非 Mandatory
                    return true;
                }
            }

            return false;
        }

        private static bool IsMandatoryPropagationTypedConstant(TypedConstant tc)
        {
            // 源代码场景：C# 字符串表示通常包含 "Propagation.Mandatory"
            if (tc.ToCSharpString().Contains("Propagation.Mandatory"))
                return true;

            // 编译后引用程序集（NuGet）场景：enum 值以整数存储，无法从 ToCSharpString 获取枚举名称
            // Propagation.Mandatory == 2
            if (tc.Kind == TypedConstantKind.Enum && tc.Value != null)
            {
                try
                {
                    return Convert.ToInt32(tc.Value) == 2;
                }
                catch
                {
                    // 忽略转换异常
                }
            }

            return false;
        }

        // 优化版：通过 Operation 向上查找，检查整个调用链
        // 性能优化：避免在每一层都调用 GetEnclosingSymbol，仅在必要时调用
        private static bool EnclosingChainHasTransactional(IOperation invocationOperation, AnalyzerCache cache)
        {
            var op = invocationOperation?.Parent;
            if (op == null) return false;

            var visitedMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

            // 缓存最近一次 GetEnclosingSymbol 的结果及其语法树，避免同树内重复调用
            IMethodSymbol? lastEnclosingMethod = null;
            SyntaxTree? lastSyntaxTree = null;

            while (op != null)
            {
                // 1) 检查是否被另一个方法调用（嵌套调用）
                if (op is IInvocationOperation parentInvocation)
                {
                    var parentMethod = parentInvocation.TargetMethod;
                    if (parentMethod != null && visitedMethods.Add(parentMethod))
                    {
                        if (HasTransactionalWithoutPropagation(parentMethod, cache))
                            return true;
                    }
                }

                // 2) 检查符号层级（通过 SemanticModel），但仅在跨越语法树边界或首次时调用
                var semanticModel = op.SemanticModel;
                if (semanticModel != null && op.Syntax != null)
                {
                    var currentTree = op.Syntax.SyntaxTree;
                    // 仅当语法树发生变化或尚未缓存时才调用 GetEnclosingSymbol
                    if (lastSyntaxTree != currentTree || lastEnclosingMethod == null)
                    {
                        lastSyntaxTree = currentTree;
                        var symbol = semanticModel.GetEnclosingSymbol(op.Syntax.SpanStart);
                        lastEnclosingMethod = symbol as IMethodSymbol;
                    }

                    if (lastEnclosingMethod != null && visitedMethods.Add(lastEnclosingMethod))
                    {
                        if (HasTransactionalWithoutPropagation(lastEnclosingMethod, cache))
                            return true;
                    }
                }

                op = op.Parent;
            }

            return false;
        }

        // 获取编译单元中的所有命名类型（仅限于当前项目的语法树，避免加载引用的程序集）
        // 仅在 CompilationStartAction 中调用一次，在此上下文中调用 GetSemanticModel 是安全的
#pragma warning disable RS1030
        private static ImmutableArray<INamedTypeSymbol> GetAllTypesInCompilation(Compilation compilation)
        {
            var types = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

            // 只遍历当前编译单元的语法树（避免加载引用的程序集）
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();

                // 使用显式 foreach 遍历 DescendantNodes，避免 LINQ Where 的委托分配
                foreach (var node in root.DescendantNodes())
                {
                    if (node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax)
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(node);
                        if (symbol is INamedTypeSymbol namedType)
                        {
                            types.Add(namedType);
                        }
                    }
                }
            }

            return types.ToImmutable();
        }
#pragma warning restore RS1030

        // 查找接口方法在当前编译单元中的所有实现（带缓存查找，利用预建索引加速）
        private static ImmutableArray<IMethodSymbol> FindImplementationsOfInterfaceMethod(
            IMethodSymbol interfaceMethod,
            AnalyzerCache cache)
        {
            if (interfaceMethod == null)
                return ImmutableArray<IMethodSymbol>.Empty;

            if (cache.InterfaceToImplementationsCache.TryGetValue(interfaceMethod, out var cached))
                return cached;

            // 只有接口方法才需要查找实现
            if (interfaceMethod.ContainingType?.TypeKind != TypeKind.Interface)
            {
                cache.InterfaceToImplementationsCache[interfaceMethod] = ImmutableArray<IMethodSymbol>.Empty;
                return ImmutableArray<IMethodSymbol>.Empty;
            }

            var interfaceType = interfaceMethod.ContainingType;
            var implementations = ImmutableArray.CreateBuilder<IMethodSymbol>();

            // 使用预建索引：接口→实现类列表，避免 O(n) 全类型扫描
            var index = cache.InterfaceToTypesIndex;
            if (index.TryGetValue(interfaceType, out var implementingTypes))
            {
                foreach (var type in implementingTypes)
                {
                    try
                    {
                        var implementation = type.FindImplementationForInterfaceMember(interfaceMethod) as IMethodSymbol;
                        if (implementation != null)
                        {
                            implementations.Add(implementation);
                        }
                    }
                    catch
                    {
                        // 忽略异常
                    }
                }
            }

            var result = implementations.ToImmutable();
            cache.InterfaceToImplementationsCache[interfaceMethod] = result;
            return result;
        }
    }
}
