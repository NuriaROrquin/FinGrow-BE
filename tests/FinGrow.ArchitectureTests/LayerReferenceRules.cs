namespace FinGrow.ArchitectureTests;

using NetArchTest.Rules;

public class LayerReferenceRules
{
    private const string DomainNamespace = "FinGrow.Domain";
    private const string ApplicationNamespace = "FinGrow.Application";
    private const string InfrastructureNamespace = "FinGrow.Infrastructure";
    private const string ApiNamespace = "FinGrow.Api";

    [Fact]
    public void Domain_no_debe_depender_de_ninguna_otra_capa()
    {
        var result = Types.InAssembly(AssemblyReference.Domain)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"tipos en falta: {FormatFailures(result)}");
    }

    [Fact]
    public void Application_no_debe_depender_de_Infrastructure_ni_de_la_Api()
    {
        var result = Types.InAssembly(AssemblyReference.Application)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"tipos en falta: {FormatFailures(result)}");
    }

    [Fact]
    public void Infrastructure_no_debe_depender_de_la_Api()
    {
        var result = Types.InAssembly(AssemblyReference.Infrastructure)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"tipos en falta: {FormatFailures(result)}");
    }

    [Fact]
    public void Domain_no_debe_depender_de_EntityFramework()
    {
        var result = Types.InAssembly(AssemblyReference.Domain)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"tipos en falta: {FormatFailures(result)}");
    }

    [Fact]
    public void Los_controllers_no_deben_usar_tipos_de_Infrastructure()
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
