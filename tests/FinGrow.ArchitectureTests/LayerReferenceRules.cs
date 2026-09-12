namespace FinGrow.ArchitectureTests;

using NetArchTest.Rules;

public class LayerReferenceRules
{
    private const string DomainNamespace = "FinGrow.Domain";
    private const string ApplicationNamespace = "FinGrow.Application";
    private const string InfrastructureNamespace = "FinGrow.Infrastructure";
    private const string ApiNamespace = "FinGrow.Api";

    [Fact]
    public void Domain_must_not_depend_on_any_other_layer()
    {
        var result = Types.InAssembly(AssemblyReference.Domain)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"tipos en falta: {FormatFailures(result)}");
    }

    [Fact]
    public void Application_must_not_depend_on_Infrastructure_or_the_Api()
    {
        var result = Types.InAssembly(AssemblyReference.Application)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"tipos en falta: {FormatFailures(result)}");
    }

    [Fact]
    public void Infrastructure_must_not_depend_on_the_Api()
    {
        var result = Types.InAssembly(AssemblyReference.Infrastructure)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"tipos en falta: {FormatFailures(result)}");
    }

    [Fact]
    public void Domain_must_not_depend_on_EntityFramework()
    {
        var result = Types.InAssembly(AssemblyReference.Domain)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"tipos en falta: {FormatFailures(result)}");
    }

    [Fact]
    public void Controllers_must_not_use_Infrastructure_types()
    {
        var result = Types.InAssembly(AssemblyReference.Api)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"tipos en falta: {FormatFailures(result)}");
    }

    private static string FormatFailures(TestResult result) =>
        result.FailingTypeNames is null ? "(ninguno)" : string.Join(", ", result.FailingTypeNames);
}
