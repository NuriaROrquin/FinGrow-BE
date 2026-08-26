namespace FinGrow.ArchitectureTests;

using FluentAssertions;
using NetArchTest.Rules;

public class NamingRules
{
    [Fact]
    public void Las_interfaces_deben_empezar_con_I()
    {
        var result = Types.InAssembly(AssemblyReference.Application)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Las_implementaciones_de_Infrastructure_deben_ser_internal()
    {
        var result = Types.InAssembly(AssemblyReference.Infrastructure)
            .That()
            .ResideInNamespaceStartingWith("FinGrow.Infrastructure.Services")
            .Should()
            .NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
