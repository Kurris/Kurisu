using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Kurisu.Transaction.Analyzer.MandatoryTransactionalAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

public class MandatoryTransactionalAnalyzerTests
{
    private const string TransactionalSupport = @"
using System;
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class TransactionalAttribute : Attribute
{
    public TransactionalAttribute() { }
    public Propagation Propagation { get; set; } = Propagation.Required;
}
public enum Propagation { Required = 0, Mandatory = 1 }
";

    [Fact]
    public async Task ReportsDiagnosticWhenNoAmbientTransactional()
    {
        var test = new VerifyCS
        {
            TestCode = TransactionalSupport + @"
public class Service
{
    [Transactional(Propagation = Propagation.Mandatory)]
    public void DoMandatory()
    {
    }
}

public class TriggerCaller
{
    public void Run()
    {
        var svc = new Service();
        svc.DoMandatory();
    }
}

public class NestedCaller
{
    [Transactional]
    public void Run()
    {
        var caller = new TriggerCaller();
        caller.Run();
    }
}

public class NestedCaller2
{
    //[Transactional]
    public void Run()
    {
        var caller = new NestedCaller();
        caller.Run();
    }
}
"
        };
        await test.RunAsync();
    }
}