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

        // 新增：缓存方法是否“需要 Mandatory”（包含自身标注或体内递归调用了标注 Mandatory 的方法）
        private static readonly ConcurrentDictionary<IMethodSymbol, bool> RequiresMandatoryCache = new(SymbolEqualityComparer.Default);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var targetMethod = invocation.TargetMethod;
            if (targetMethod == null) return;

            // 修改：传入 compilation，使 HasTransactionalWithMandatory 同时考虑接口/实现关系
            var requiresMandatory = HasTransactionalWithMandatory(targetMethod, context.Compilation);
            if (!requiresMandatory) return;

            var containingSymbol = context.ContainingSymbol;
            if (containingSymbol == null) return;

            // 传入 invocation 及 compilation 以便在操作树/符号链上向上查找父级调用（收集调用链中的方法）
            if (EnclosingChainHasTransactional(containingSymbol, invocation, context.Compilation, out var methodInfos))
            {
                return;
            }

            // 否则报告错误（针对被调用的方法）
            var diagnostic = Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), targetMethod.Name);
            context.ReportDiagnostic(diagnostic);

            // methodInfos: List<(Location location, string invocationName)>
            foreach (var (loc, invocationName) in methodInfos)
            {
                if (loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, loc, invocationName));
                }
            }
        }

        // 将 HasTransactionalWithMandatory 扩展为同时考虑接口/实现（双向），需传入 compilation
        private static bool HasTransactionalWithMandatory(IMethodSymbol method, Compilation compilation)
        {
            if (method == null) return false;

            // 1) 先检查方法自身的 attribute
            if (HasTransactionalWithMandatory_Self(method))
                return true;

            // 2) 检查显式接口实现（method.ExplicitInterfaceImplementations）
            foreach (var ei in method.ExplicitInterfaceImplementations)
            {
                if (HasTransactionalWithMandatory_Self(ei))
                    return true;
            }

            // 3) 如果 method 是某个实现（类方法），检查它实现的接口成员（隐式实现）
            var containingType = method.ContainingType;
            if (containingType != null)
            {
                foreach (var iface in containingType.AllInterfaces)
                {
                    foreach (var ifaceMember in iface.GetMembers().OfType<IMethodSymbol>())
                    {
                        try
                        {
                            var impl = containingType.FindImplementationForInterfaceMember(ifaceMember) as IMethodSymbol;
                            if (impl != null && SymbolEqualityComparer.Default.Equals(impl.OriginalDefinition, method.OriginalDefinition))
                            {
                                if (HasTransactionalWithMandatory_Self(ifaceMember))
                                    return true;
                            }
                        }
                        catch
                        {
                            // 忽略 FindImplementationForInterfaceMember 可能抛出的异常
                        }
                    }
                }
            }

            // 4) 如果 method 本身是接口成员或抽象/未实现，查找其在编译单元中的实现并检查实现方法
            var impls = FindImplementations(method, compilation);
            foreach (var impl in impls)
            {
                if (HasTransactionalWithMandatory_Self(impl))
                    return true;
            }

            return false;
        }

        // 辅助：仅检查方法自身是否带有 Propagation=Mandatory 的 Transactional 特性
        private static bool HasTransactionalWithMandatory_Self(IMethodSymbol method)
        {
            foreach (var attr in method.GetAttributes())
            {
                var attrClass = attr.AttributeClass;
                if (attrClass == null) continue;

                if (!attrClass.Name.Contains("Transactional")) continue;

                // 检查 constructor args 和命名参数是否包含 Propagation.Mandatory
                foreach (var ca in attr.ConstructorArguments)
                {
                    if (IsMandatoryPropagationTypedConstant(ca)) return true;
                }

                foreach (var na in attr.NamedArguments)
                {
                    if (na.Key.Equals("Propagation", System.StringComparison.OrdinalIgnoreCase) && IsMandatoryPropagationTypedConstant(na.Value))
                        return true;
                }

                // 如果没有显式提供 Propagation，默认可能不是 Mandatory；跳过
            }

            return false;
        }

        private static bool HasTransactionalWithoutPropagation(IMethodSymbol method)
        {
            if (method == null) return false;
            foreach (var attr in method.GetAttributes())
            {
                var attrClass = attr.AttributeClass;
                if (attrClass == null) continue;
                if (!attrClass.Name.Contains("Transactional")) continue;

                // 如果有命名参数 Propagation，且其值为 Mandatory，则视为不合格（返回 false）
                foreach (var na in attr.NamedArguments)
                {
                    if (na.Key.Equals("Propagation", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (IsMandatoryPropagationTypedConstant(na.Value))
                            return false;
                        // 明确指定了非 Mandatory，认为链上存在可接受的 Transactional
                        return true;
                    }
                }

                // 如果构造参数中包含 Propagation 枚举（某些写法可能通过构造参数传递），检查是否为 Mandatory
                foreach (var ca in attr.ConstructorArguments)
                {
                    if (IsMandatoryPropagationTypedConstant(ca))
                    {
                        // 构造参数显式为 Mandatory，视为不合格
                        return false;
                    }
                }

                // 未显式指定 Propagation，默认认为不是 Mandatory，因此视为合格
                return true;
            }

            return false;
        }

        // 新增：考虑实现/接口关联的合格 Transactional（Propagation != Mandatory）
        private static bool HasTransactionalWithoutPropagationIncludingRelated(IMethodSymbol method, Compilation compilation)
        {
            if (method == null) return false;

            // 1) 方法自身或显式接口实现
            if (HasTransactionalWithoutPropagation(method))
                return true;

            foreach (var ei in method.ExplicitInterfaceImplementations)
            {
                if (HasTransactionalWithoutPropagation(ei))
                    return true;
            }

            // 2) 如果该方法属于某个实现类型，检查它所实现的接口成员（隐式实现）
            var containingType = method.ContainingType;
            if (containingType != null)
            {
                foreach (var iface in containingType.AllInterfaces)
                {
                    foreach (var ifaceMember in iface.GetMembers().OfType<IMethodSymbol>())
                    {
                        try
                        {
                            var impl = containingType.FindImplementationForInterfaceMember(ifaceMember) as IMethodSymbol;
                            if (impl != null && SymbolEqualityComparer.Default.Equals(impl.OriginalDefinition, method.OriginalDefinition))
                            {
                                if (HasTransactionalWithoutPropagation(ifaceMember))
                                    return true;
                            }
                        }
                        catch
                        {
                            // 忽略可能的异常
                        }
                    }
                }
            }

            // 3) 如果 method 本身是接口或抽象/未实现，检查编译单元中的实现方法是否带有合格的 Transactional
            var impls = FindImplementations(method, compilation);
            foreach (var impl in impls)
            {
                if (HasTransactionalWithoutPropagation(impl))
                    return true;
            }

            return false;
        }

        private static bool IsMandatoryPropagationTypedConstant(TypedConstant tc)
        {
            return tc.ToCSharpString().Contains("Propagation.Mandatory");
        }

        // 新增：递归检查方法体内的调用，判断方法是否“需要 Mandatory”
        private static bool MethodRequiresMandatory(IMethodSymbol method, Compilation compilation, HashSet<IMethodSymbol>? visiting = null)
        {
            if (method == null) return false;

            // 修改：使用带 compilation 的版本，考虑接口/实现关系
            if (HasTransactionalWithMandatory(method, compilation))
                return true;

            if (RequiresMandatoryCache.TryGetValue(method, out var cached))
                return cached;

            visiting ??= new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            if (!visiting.Add(method))
            {
                // 已在访问链上，避免循环依赖导致无限递归
                return false;
            }

            bool result = false;

            foreach (var decl in method.DeclaringSyntaxReferences)
            {
                var node = decl.GetSyntax();
                var tree = node.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(tree);

                // 收集方法体内所有调用表达式，检查其目标方法
                var invocations = node.DescendantNodes().OfType<InvocationExpressionSyntax>();
                foreach (var inv in invocations)
                {
                    // 改为使用 GetInvokedMethodSymbol 来更准确识别接口调用等情况
                    var sym = GetInvokedMethodSymbol(inv, semanticModel);
                    if (sym == null) continue;

                    // 先检查被解析到的方法本身（及其相关接口/实现）
                    if (HasTransactionalWithMandatory(sym, compilation))
                    {
                        result = true;
                        break;
                    }

                    // 若被解析的方法是接口/抽象，尝试查找其在编译单元中的实现，并一并检查实现
                    var impls = FindImplementations(sym, compilation);
                    foreach (var impl in impls)
                    {
                        if (HasTransactionalWithMandatory(impl, compilation))
                        {
                            result = true;
                            break;
                        }
                    }

                    if (result) break;

                    // 递归检查被调用的方法（及其实现）是否需要 Mandatory
                    if (MethodRequiresMandatory(sym, compilation, visiting))
                    {
                        result = true;
                        break;
                    }

                    foreach (var impl in impls)
                    {
                        if (MethodRequiresMandatory(impl, compilation, visiting))
                        {
                            result = true;
                            break;
                        }
                    }

                    if (result) break;
                }

                if (result) break;
            }

            visiting.Remove(method);
            RequiresMandatoryCache[method] = result;
            return result;
        }

        // 修改：增加 Compilation 参数，链上传播规则：
        // - 遇到 HasTransactional(合格) => 整链合格，返回 true
        // - 遇到 MethodRequiresMandatory => 把该方法视为链上节点并继续向上
        // - 遇到既非合格也不需要 Mandatory 的方法 => 链断，返回 false
        // 修改：EnclosingChainHasTransactional 改为基于队列（BFS/DFS）遍历调用者链（包括间接调用者）
        private static bool EnclosingChainHasTransactional(ISymbol startingSymbol, IOperation? invocationOperation, Compilation compilation, out List<(Location location, string invocationName)> methodInfos)
        {
            methodInfos = new List<(Location, string)>();
            var seen = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            var queue = new Queue<IMethodSymbol>();

            // 1) 从操作树的父级调用开始收集初始方法（例如外层的 IInvocationOperation）
            var op = invocationOperation?.Parent;
            while (op != null)
            {
                if (op is IInvocationOperation parentInvocation)
                {
                    var parentMethod = parentInvocation.TargetMethod;
                    if (parentMethod != null && !seen.Contains(parentMethod))
                    {
                        seen.Add(parentMethod);
                        queue.Enqueue(parentMethod);

                        // 记录调用位置与调用名称（使用被调用方法名作为调用名称）
                        var loc = parentInvocation.Syntax.GetLocation();
                        var name = parentMethod.Name;
                        methodInfos.Add((loc, name));
                    }
                }

                op = op.Parent;
            }

            // 2) 如果起始符号本身是方法（例如直接在方法体内调用），也把它作为起点
            if (startingSymbol is IMethodSymbol startMethod && !seen.Contains(startMethod))
            {
                seen.Add(startMethod);
                queue.Enqueue(startMethod);

                Location? loc = startMethod.Locations.Length > 0
                    ? startMethod.Locations[0]
                    : startMethod.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation();

                methodInfos.Add((loc ?? Location.None, startMethod.Name));
            }

            // 3) 通过队列遍历：对于每个方法，判断是否存在合格的 [Transactional]（Propagation != Mandatory）
            //    若存在则整链合格；否则若该方法“需要 Mandatory”则将它的调用者加入队列继续向上扩展（间接调用）
            while (queue.Count > 0)
            {
                var m = queue.Dequeue();

                // 如果该方法上存在合格的 [Transactional]（Propagation != Mandatory），则整链合格
                // 同时考虑接口/实现关联（任意相关方法只要是合格就认为整链合格）
                if (HasTransactionalWithoutPropagationIncludingRelated(m, compilation))
                    return true;

                // 若该方法需要 Mandatory，则继续查找调用它的方法（callers）
                if (MethodRequiresMandatory(m, compilation))
                {
                    var callers = FindCallers(m, compilation);
                    foreach (var (caller, loc, invocationName) in callers)
                    {
                        if (!seen.Contains(caller))
                        {
                            seen.Add(caller);
                            queue.Enqueue(caller);
                            methodInfos.Add((loc, invocationName));
                        }
                    }

                    // 另外也尝试向上包含符号链（例如局部函数包含在方法内的情况）
                    var container = m.ContainingSymbol;
                    while (container != null)
                    {
                        if (container is IMethodSymbol containerMethod && !seen.Contains(containerMethod))
                        {
                            seen.Add(containerMethod);
                            queue.Enqueue(containerMethod);

                            Location? loc = containerMethod.Locations.Length > 0
                                ? containerMethod.Locations[0]
                                : containerMethod.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation();

                            methodInfos.Add((loc ?? Location.None, containerMethod.Name));
                        }

                        container = container.ContainingSymbol;
                    }
                }
                else
                {
                    // 如果既不是合格的 Transactional，也不需要 Mandatory，则该分支停止扩展
                    continue;
                }
            }

            // 未找到合格的 Transactional
            return false;
        }

        // 新增：判断两个方法是否相同或通过接口/实现关系相关联（用于识别接口方法调用）
        private static bool AreMethodsRelated(IMethodSymbol? a, IMethodSymbol? b, Compilation compilation)
        {
            if (a == null || b == null) return false;

            // 直接比较原始定义
            if (SymbolEqualityComparer.Default.Equals(a.OriginalDefinition, b.OriginalDefinition))
                return true;

            try
            {
                // explicit interface implementations on a
                if (a.ExplicitInterfaceImplementations.Any(e => SymbolEqualityComparer.Default.Equals(e.OriginalDefinition, b.OriginalDefinition)))
                    return true;

                // explicit interface implementations on b
                if (b.ExplicitInterfaceImplementations.Any(e => SymbolEqualityComparer.Default.Equals(e.OriginalDefinition, a.OriginalDefinition)))
                    return true;

                // a is interface member, check if b's type implements it
                if (a.ContainingType?.TypeKind == TypeKind.Interface && b.ContainingType != null)
                {
                    var impl = b.ContainingType.FindImplementationForInterfaceMember(a);
                    if (impl is IMethodSymbol implMethod && SymbolEqualityComparer.Default.Equals(implMethod.OriginalDefinition, b.OriginalDefinition))
                        return true;
                }

                // b is interface member, check if a's type implements it
                if (b.ContainingType?.TypeKind == TypeKind.Interface && a.ContainingType != null)
                {
                    var impl2 = a.ContainingType.FindImplementationForInterfaceMember(b);
                    if (impl2 is IMethodSymbol implMethod2 && SymbolEqualityComparer.Default.Equals(implMethod2.OriginalDefinition, a.OriginalDefinition))
                        return true;
                }
            }
            catch
            {
                // 忽略在某些特殊类型上 FindImplementationForInterfaceMember 可能抛出的异常
            }

            return false;
        }

        private static IEnumerable<(IMethodSymbol caller, Location location, string invocationName)> FindCallers(IMethodSymbol targetMethod, Compilation compilation)
        {
            var callers = new HashSet<(IMethodSymbol, Location, string)>();
            if (targetMethod == null) return callers;

            var targetDef = targetMethod.OriginalDefinition;

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();

                var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
                foreach (var inv in invocations)
                {
                    // 使用 GetInvokedMethodSymbol 提高识别率（包括接口调用）
                    var called = GetInvokedMethodSymbol(inv, semanticModel);
                    if (called == null) continue;

                    // 构造候选列表：被解析到的方法本身 + 它的实现（若是接口/抽象）
                    var calledCandidates = new List<IMethodSymbol> { called };
                    calledCandidates.AddRange(FindImplementations(called, compilation));

                    bool matched = false;
                    foreach (var candidate in calledCandidates)
                    {
                        if (SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, targetDef)
                            || AreMethodsRelated(candidate, targetDef, compilation)
                            || AreMethodsRelated(targetDef, candidate, compilation))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (!matched) continue;

                    var loc = inv.GetLocation();
                    var invocationName = called.Name;

                    var containing = semanticModel.GetEnclosingSymbol(inv.SpanStart);
                    if (containing is IMethodSymbol callerMethod)
                    {
                        callers.Add((callerMethod, loc, invocationName));
                    }
                    else
                    {
                        var ancestorMethodNode = inv.Ancestors().FirstOrDefault(a =>
                            a is MethodDeclarationSyntax
                            || a is LocalFunctionStatementSyntax);
                        if (ancestorMethodNode != null)
                        {
                            var sym = semanticModel.GetDeclaredSymbol(ancestorMethodNode);
                            if (sym is IMethodSymbol ms)
                                callers.Add((ms, loc, invocationName));
                        }
                    }
                }
            }

            return callers;
        }

        // 新增：在 Compilation 中查找某个接口/抽象方法的实现方法（可能有多个）
        private static IEnumerable<IMethodSymbol> FindImplementations(IMethodSymbol? method, Compilation compilation)
        {
            var results = new List<IMethodSymbol>();
            if (method == null) return results;

            // 只对接口成员或抽象成员尝试查找实现
            var container = method.ContainingType;
            if (container == null) return results;
            if (container.TypeKind != TypeKind.Interface && !method.IsAbstract) return results;

            // 遍历全局命名空间的类型（含嵌套类型）
            foreach (var type in GetAllNamedTypes(compilation.GlobalNamespace))
            {
                try
                {
                    // 检查类型是否实现了接口成员或能为抽象成员提供实现
                    var impl = type.FindImplementationForInterfaceMember(method);
                    if (impl is IMethodSymbol implMethod)
                    {
                        results.Add(implMethod);
                    }
                    else
                    {
                        // 对非接口抽象情形，查找重写实现
                        foreach (var m in type.GetMembers().OfType<IMethodSymbol>())
                        {
                            if (SymbolEqualityComparer.Default.Equals(m.OverriddenMethod?.OriginalDefinition, method.OriginalDefinition))
                            {
                                results.Add(m);
                            }
                        }
                    }
                }
                catch
                {
                    // 某些类型上 FindImplementationForInterfaceMember 可能抛异常，忽略
                }
            }

            return results;
        }

        // 新增：递归获取命名空间下所有命名类型（包含嵌套类型）
        private static IEnumerable<INamedTypeSymbol> GetAllNamedTypes(INamespaceSymbol @namespace)
        {
            var stack = new Stack<INamespaceSymbol>();
            stack.Push(@namespace);
            while (stack.Count > 0)
            {
                var ns = stack.Pop();
                foreach (var nested in ns.GetNamespaceMembers())
                    stack.Push(nested);

                foreach (var t in ns.GetTypeMembers())
                {
                    yield return t;
                    foreach (var nestedType in GetNestedTypesRecursive(t))
                        yield return nestedType;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetNestedTypesRecursive(INamedTypeSymbol type)
        {
            foreach (var nested in type.GetTypeMembers())
            {
                yield return nested;
                foreach (var deeper in GetNestedTypesRecursive(nested))
                    yield return deeper;
            }
        }

        private static IMethodSymbol? GetInvokedMethodSymbol(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            if (invocation == null || semanticModel == null) return null;

            // 优先通过 Operation 获取更精确的目标方法
            var op = semanticModel.GetOperation(invocation) as IInvocationOperation;
            if (op?.TargetMethod != null) return op.TargetMethod;

            // 其次通过 SymbolInfo 直接获取
            var symInfo = ModelExtensions.GetSymbolInfo(semanticModel, invocation);
            if (symInfo.Symbol is IMethodSymbol ms) return ms;

            // 候选符号（例如重载解析不唯一时）
            var candidate = symInfo.CandidateSymbols.FirstOrDefault() as IMethodSymbol;
            if (candidate != null) return candidate;

            // 尝试分析 invocation.Expression 的不���语法形态
            var expr = invocation.Expression;
            if (expr != null)
            {
                // 对于 member access / generic name / identifier 等，分别尝试解析其符号
                var exprInfo = ModelExtensions.GetSymbolInfo(semanticModel, expr);
                if (exprInfo.Symbol is IMethodSymbol exprMs) return exprMs;
                var exprCandidate = exprInfo.CandidateSymbols.FirstOrDefault() as IMethodSymbol;
                if (exprCandidate != null) return exprCandidate;

                if (expr is MemberAccessExpressionSyntax memberAccess)
                {
                    var nameInfo = ModelExtensions.GetSymbolInfo(semanticModel, memberAccess.Name);
                    if (nameInfo.Symbol is IMethodSymbol nameMs) return nameMs;
                    var nameCand = nameInfo.CandidateSymbols.FirstOrDefault() as IMethodSymbol;
                    if (nameCand != null) return nameCand;
                }
                else if (expr is IdentifierNameSyntax idName)
                {
                    var idInfo = ModelExtensions.GetSymbolInfo(semanticModel, idName);
                    if (idInfo.Symbol is IMethodSymbol idMs) return idMs;
                    var idCand = idInfo.CandidateSymbols.FirstOrDefault() as IMethodSymbol;
                    if (idCand != null) return idCand;
                }
                else if (expr is GenericNameSyntax genName)
                {
                    var genInfo = ModelExtensions.GetSymbolInfo(semanticModel, genName);
                    if (genInfo.Symbol is IMethodSymbol genMs) return genMs;
                    var genCand = genInfo.CandidateSymbols.FirstOrDefault() as IMethodSymbol;
                    if (genCand != null) return genCand;
                }

                // 作为最后兜底，再尝试取表达式的 Operation（例如委托调用等情况）
                var exprOp = semanticModel.GetOperation(expr) as IInvocationOperation;
                if (exprOp?.TargetMethod != null) return exprOp.TargetMethod;
            }

            return null;
        }
    }
}

