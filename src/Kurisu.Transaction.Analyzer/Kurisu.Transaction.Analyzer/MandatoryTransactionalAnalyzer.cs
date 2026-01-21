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
        private static readonly LocalizableString MessageFormat = "调用具有 Propagation.Mandatory 的事务方法 '{0}' 必须在调用链上存在标注 [Transactional] 的方法。";
        private static readonly LocalizableString Description = "Methods annotated with Transactional(Propagation = Propagation.Mandatory) require that the caller chain contains a method annotated with Transactional.";
        private const string Category = "Correctness";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        // 编译级别的缓存类，避免静态缓存跨编译会话泄漏
        private sealed class AnalyzerCache
        {
            public ConcurrentDictionary<IMethodSymbol, bool?> HasMandatoryCache { get; } 
                = new(SymbolEqualityComparer.Default);
            
            public ConcurrentDictionary<IMethodSymbol, bool?> HasTransactionalCache { get; } 
                = new(SymbolEqualityComparer.Default);
            
            // 缓存接口实现查找结果，避免重复查找
            public ConcurrentDictionary<(INamedTypeSymbol, INamedTypeSymbol, IMethodSymbol), IMethodSymbol> InterfaceImplementationCache { get; }
                = new(new InterfaceImplementationComparer());
            
            // 缓存接口方法的所有实现类方法（反向查找）
            public ConcurrentDictionary<IMethodSymbol, ImmutableArray<IMethodSymbol>> InterfaceToImplementationsCache { get; }
                = new(SymbolEqualityComparer.Default);
            
            // 存储当前编译的所有类型（延迟初始化）
            private ImmutableArray<INamedTypeSymbol>? _allTypes;
            public ImmutableArray<INamedTypeSymbol> AllTypes
            {
                get => _allTypes ?? ImmutableArray<INamedTypeSymbol>.Empty;
                set => _allTypes = value;
            }
        }

        // 用于缓存的相等比较器
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
                
                // 延迟初始化所有类型（仅在需要时收集）
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

            // 仅通过 Operation 树向上查找，不进行全局搜索
            if (EnclosingChainHasTransactional(invocation, cache))
                return;

            // 报告错误
            var diagnostic = Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), targetMethod.Name);
            context.ReportDiagnostic(diagnostic);
        }

        // 优化：添加缓存，简化接口检查，一直往上检查接口层次（带深度限制）
        private static bool HasTransactionalWithMandatory(IMethodSymbol method, AnalyzerCache cache)
        {
            if (method == null) return false;

            // 检查缓存
            if (cache.HasMandatoryCache.TryGetValue(method, out var cached) && cached.HasValue)
                return cached.Value;

            bool result = HasTransactionalWithMandatoryIncludingInterfaces(method, cache, depth: 0);
            cache.HasMandatoryCache[method] = result;
            return result;
        }

        // 递归检查方法及其接口实现/基类是否有 Mandatory（一直往上，带深度限制）
        private static bool HasTransactionalWithMandatoryIncludingInterfaces(IMethodSymbol method, AnalyzerCache cache, int depth = 0)
        {
            const int MaxRecursionDepth = 10; // 限制最大递归深度
            
            if (method == null || depth > MaxRecursionDepth) 
                return false;

            // 1) 先检查方法自身的 attribute
            if (HasTransactionalWithMandatory_Self(method))
                return true;

            // 2) 检查显式接口实现
            foreach (var ei in method.ExplicitInterfaceImplementations)
            {
                if (HasTransactionalWithMandatory_Self(ei))
                    return true;
                
                // 递归检查接口的父接口
                if (HasTransactionalWithMandatoryIncludingInterfaces(ei, cache, depth + 1))
                    return true;
            }

            // 3) 检查隐式接口实现（仅对类方法）
            if (method.ContainingType != null && method.ContainingType.TypeKind == TypeKind.Class)
            {
                var containingType = method.ContainingType;
                foreach (var iface in containingType.AllInterfaces)
                {
                    var interfaceMethod = SafeFindImplementation(containingType, iface, method, cache);
                    if (interfaceMethod != null)
                    {
                        if (HasTransactionalWithMandatory_Self(interfaceMethod))
                            return true;
                        
                        // 递归检查接口方法的父接口
                        if (HasTransactionalWithMandatoryIncludingInterfaces(interfaceMethod, cache, depth + 1))
                            return true;
                    }
                }
            }

            // 4) ? 新增：如果方法是接口方法，检查其在当前编译单元中的所有实现类
            if (method.ContainingType?.TypeKind == TypeKind.Interface)
            {
                var implementations = FindImplementationsOfInterfaceMethod(method, cache);
                foreach (var impl in implementations)
                {
                    if (HasTransactionalWithMandatory_Self(impl))
                        return true;
                    
                    // 递归检查实现类方法（可能还实现了其他接口）
                    if (HasTransactionalWithMandatoryIncludingInterfaces(impl, cache, depth + 1))
                        return true;
                }
            }

            // 5) 检查重写的基类方法
            if (method.OverriddenMethod != null)
            {
                if (HasTransactionalWithMandatoryIncludingInterfaces(method.OverriddenMethod, cache, depth + 1))
                    return true;
            }

            return false;
        }

        // 安全地查找接口实现，避免过度遍历，添加缓存
        private static IMethodSymbol SafeFindImplementation(INamedTypeSymbol type, INamedTypeSymbol iface, IMethodSymbol method, AnalyzerCache cache)
        {
            if (type == null || iface == null || method == null)
                return null;

            var cacheKey = (type, iface, method);
            
            // 检查缓存
            if (cache.InterfaceImplementationCache.TryGetValue(cacheKey, out var cachedResult))
                return cachedResult;

            IMethodSymbol result = null;
            
            try
            {
                // 只遍历当前接口的成员（不包括父接口）
                foreach (var ifaceMember in iface.GetMembers().OfType<IMethodSymbol>())
                {
                    // 跳过不匹配的方法名（早期优化）
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

            // 缓存结果（包括 null）
            cache.InterfaceImplementationCache[cacheKey] = result;
            return result;
        }

        // 辅助：仅检查方法自身是否带有 Propagation=Mandatory 的 Transactional 特性
        private static bool HasTransactionalWithMandatory_Self(IMethodSymbol method)
        {
            if (method == null) return false;

            foreach (var attr in method.GetAttributes())
            {
                var attrClass = attr.AttributeClass;
                if (attrClass == null || !attrClass.Name.Contains("Transactional")) 
                    continue;

                // 检查 constructor args 和命名参数是否包含 Propagation.Mandatory
                if (attr.ConstructorArguments.Any(IsMandatoryPropagationTypedConstant))
                    return true;

                foreach (var na in attr.NamedArguments)
                {
                    if (na.Key.Equals("Propagation", StringComparison.OrdinalIgnoreCase) 
                        && IsMandatoryPropagationTypedConstant(na.Value))
                        return true;
                }
            }

            return false;
        }

        // 检查方法是否有合格的 Transactional（包括接口实现的检查）
        private static bool HasTransactionalWithoutPropagation(IMethodSymbol method, AnalyzerCache cache)
        {
            if (method == null) return false;

            // 检查缓存
            if (cache.HasTransactionalCache.TryGetValue(method, out var cached) && cached.HasValue)
                return cached.Value;

            bool result = HasTransactionalWithoutPropagationIncludingInterfaces(method, cache, depth: 0);
            cache.HasTransactionalCache[method] = result;
            return result;
        }

        // 检查方法及其接口实现是否有合格的 Transactional（一直往上检查，带深度限制）
        private static bool HasTransactionalWithoutPropagationIncludingInterfaces(IMethodSymbol method, AnalyzerCache cache, int depth = 0)
        {
            const int MaxRecursionDepth = 10; // 限制最大递归深度
            
            if (method == null || depth > MaxRecursionDepth) 
                return false;

            // 1) 检查方法自身
            if (HasTransactionalWithoutPropagation_Self(method))
                return true;

            // 2) 检查显式接口实现
            foreach (var ei in method.ExplicitInterfaceImplementations)
            {
                if (HasTransactionalWithoutPropagation_Self(ei))
                    return true;
                
                // 递归检查接口的父接口
                if (HasTransactionalWithoutPropagationIncludingInterfaces(ei, cache, depth + 1))
                    return true;
            }

            // 3) 检查隐式接口实现（仅对类方法）
            var containingType = method.ContainingType;
            if (containingType != null && containingType.TypeKind == TypeKind.Class)
            {
                foreach (var iface in containingType.AllInterfaces)
                {
                    var interfaceMethod = SafeFindImplementation(containingType, iface, method, cache);
                    if (interfaceMethod != null)
                    {
                        if (HasTransactionalWithoutPropagation_Self(interfaceMethod))
                            return true;
                        
                        // 递归检查接口方法的父接口
                        if (HasTransactionalWithoutPropagationIncludingInterfaces(interfaceMethod, cache, depth + 1))
                            return true;
                    }
                }
            }

            // 4) ? 新增：如果方法是接口方法，检查其在当前编译单元中的所有实现类
            if (method.ContainingType?.TypeKind == TypeKind.Interface)
            {
                var implementations = FindImplementationsOfInterfaceMethod(method, cache);
                foreach (var impl in implementations)
                {
                    if (HasTransactionalWithoutPropagation_Self(impl))
                        return true;
                    
                    // 递归检查实现类方法
                    if (HasTransactionalWithoutPropagationIncludingInterfaces(impl, cache, depth + 1))
                        return true;
                }
            }

            // 5) 检查重写的基类方法
            if (method.OverriddenMethod != null)
            {
                if (HasTransactionalWithoutPropagationIncludingInterfaces(method.OverriddenMethod, cache, depth + 1))
                    return true;
            }

            return false;
        }

        // 仅检查方法自身的 Transactional 属性
        private static bool HasTransactionalWithoutPropagation_Self(IMethodSymbol method)
        {
            if (method == null) return false;

            foreach (var attr in method.GetAttributes())
            {
                var attrClass = attr.AttributeClass;
                if (attrClass == null || !attrClass.Name.Contains("Transactional")) 
                    continue;

                // 检查命名参数 Propagation
                foreach (var na in attr.NamedArguments)
                {
                    if (na.Key.Equals("Propagation", StringComparison.OrdinalIgnoreCase))
                    {
                        if (IsMandatoryPropagationTypedConstant(na.Value))
                            return false;
                        return true;
                    }
                }

                // 检查构造参数
                if (attr.ConstructorArguments.Any(IsMandatoryPropagationTypedConstant))
                    return false;

                // 未显式指定 Propagation，默认认为不是 Mandatory
                return true;
            }

            return false;
        }

        private static bool IsMandatoryPropagationTypedConstant(TypedConstant tc)
        {
            return tc.ToCSharpString().Contains("Propagation.Mandatory");
        }

        // 优化：仅通过 Operation 树向上查找，不进行全局搜索
        private static bool EnclosingChainHasTransactional(IOperation invocationOperation, AnalyzerCache cache)
        {
            var op = invocationOperation?.Parent;
            var visitedMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

            while (op != null)
            {
                // 1) 检查是否在另一个方法调用内（嵌套调用）
                if (op is IInvocationOperation parentInvocation)
                {
                    var parentMethod = parentInvocation.TargetMethod;
                    if (parentMethod != null && visitedMethods.Add(parentMethod))
                    {
                        // 使用统一的接口检查方法，会一直往上检查
                        if (HasTransactionalWithoutPropagation(parentMethod, cache))
                            return true;
                    }
                }

                // 2) 检查包含方法（通过 SemanticModel）
                var semanticModel = op.SemanticModel;
                if (semanticModel != null && op.Syntax != null)
                {
                    var enclosingSymbol = semanticModel.GetEnclosingSymbol(op.Syntax.SpanStart);
                    if (enclosingSymbol is IMethodSymbol enclosingMethod && visitedMethods.Add(enclosingMethod))
                    {
                        // 使用统一的接口检查方法，会一直往上检查
                        if (HasTransactionalWithoutPropagation(enclosingMethod, cache))
                            return true;
                    }
                }


                op = op.Parent;
            }

            return false;
        }

        // 获取编译单元中的所有命名类型（仅限当前项目，不包括引用的程序集）
        private static ImmutableArray<INamedTypeSymbol> GetAllTypesInCompilation(Compilation compilation)
        {
            var types = new List<INamedTypeSymbol>();
            
            // 只遍历当前编译单元的语法树（不包括引用的程序集）
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                
                var typeDeclarations = root.DescendantNodes()
                    .Where(n => n is ClassDeclarationSyntax || n is StructDeclarationSyntax || n is RecordDeclarationSyntax);
                
                foreach (var typeDecl in typeDeclarations)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(typeDecl);
                    if (symbol is INamedTypeSymbol namedType)
                    {
                        types.Add(namedType);
                    }
                }
            }
            
            return types.ToImmutableArray();
        }

        // 查找接口方法在当前编译单元中的所有实现（反向查找）
        private static ImmutableArray<IMethodSymbol> FindImplementationsOfInterfaceMethod(
            IMethodSymbol interfaceMethod, 
            AnalyzerCache cache)
        {
            if (interfaceMethod == null)
                return ImmutableArray<IMethodSymbol>.Empty;

            // 检查缓存
            if (cache.InterfaceToImplementationsCache.TryGetValue(interfaceMethod, out var cached))
                return cached;

            var implementations = new List<IMethodSymbol>();
            
            // 只有接口方法才需要查找实现
            if (interfaceMethod.ContainingType?.TypeKind != TypeKind.Interface)
            {
                cache.InterfaceToImplementationsCache[interfaceMethod] = ImmutableArray<IMethodSymbol>.Empty;
                return ImmutableArray<IMethodSymbol>.Empty;
            }

            var interfaceType = interfaceMethod.ContainingType;

            // 遍历当前编译单元中的所有类型
            foreach (var type in cache.AllTypes)
            {
                // 只检查类（不检查结构体、接口等）
                if (type.TypeKind != TypeKind.Class)
                    continue;

                // 检查该类型是否实现了目标接口
                if (!type.AllInterfaces.Contains(interfaceType, SymbolEqualityComparer.Default))
                    continue;

                try
                {
                    // 查找该接口方法在此类型中的实现
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

            var result = implementations.ToImmutableArray();
            cache.InterfaceToImplementationsCache[interfaceMethod] = result;
            return result;
        }
    }
}
