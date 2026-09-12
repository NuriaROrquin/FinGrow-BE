namespace FinGrow.ArchitectureTests;

using NetArchTest.Rules;

public class NamingRules
{
    [Fact]
    public void Interfaces_must_start_with_I()
    {
        var result = Types.InAssembly(AssemblyReference.Application)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Infrastructure_implementations_must_be_internal()
    {
        var result = Types.InAssembly(AssemblyReference.Infrastructure)
            .That()
            .ResideInNamespaceStartingWith("FinGrow.Infrastructure.Services")
            .Should()
            .NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Handlers_must_be_internal()
    {
        var result = Types.InAssembly(AssemblyReference.Application)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Handlers_must_live_in_Features()
    {
        var result = Types.InAssembly(AssemblyReference.Application)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .ResideInNamespaceStartingWith("FinGrow.Application.Features")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}
