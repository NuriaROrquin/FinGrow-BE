namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public class EmployeeIntegrationTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Linking_stores_the_trimmed_external_account()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.WhatsApp, " +5491112345678 ", Now);

        integration.ExternalAccountId.ShouldBe("+5491112345678");
        integration.Provider.ShouldBe(IntegrationProvider.WhatsApp);
        integration.LinkedAt.ShouldBe(Now);
    }

    [Fact]
    public void An_integration_always_belongs_to_an_employee()
    {
        Should.Throw<DomainException>(() =>
            EmployeeIntegration.Create(Guid.Empty, IntegrationProvider.WhatsApp, "+5491112345678", Now));
    }

    [Fact]
    public void The_external_account_is_required()
    {
        Should.Throw<DomainException>(() =>
            EmployeeIntegration.Create(EmployeeId, IntegrationProvider.WhatsApp, "  ", Now));
    }

    [Fact]
    public void Relinking_replaces_the_phone_number_instead_of_adding_a_second_one()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.WhatsApp, "+5491112345678", Now);

        integration.Relink("+5491199999999", Now.AddDays(1));

        integration.ExternalAccountId.ShouldBe("+5491199999999");
        integration.LinkedAt.ShouldBe(Now.AddDays(1));
        integration.UpdatedAt.ShouldBe(Now.AddDays(1));
    }
}
