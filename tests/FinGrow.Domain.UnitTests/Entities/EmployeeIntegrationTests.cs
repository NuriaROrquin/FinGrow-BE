namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;

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

    [Fact]
    public void An_oauth_provider_can_store_its_grant()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.MercadoPago, "228085066", Now);
        var grant = OAuthGrant.From("access", "refresh", Now.AddDays(180));

        integration.Authorize(grant, Now.AddMinutes(1));

        integration.Grant.ShouldBe(grant);
        integration.UpdatedAt.ShouldBe(Now.AddMinutes(1));
    }

    [Fact]
    public void A_new_integration_starts_active()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Gmail, "juan.perez@gmail.com", Now);

        integration.Status.ShouldBe(IntegrationStatus.Active);
    }

    [Fact]
    public void A_failed_integration_becomes_active_again_when_reauthorized()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Gmail, "juan.perez@gmail.com", Now);
        integration.MarkFailed("Sin acceso", Now.AddDays(1));

        integration.Status.ShouldBe(IntegrationStatus.Error);
        integration.UpdatedAt.ShouldBe(Now.AddDays(1));

        integration.Authorize(OAuthGrant.From("access", "refresh", Now.AddDays(2)), Now.AddDays(2));

        integration.Status.ShouldBe(IntegrationStatus.Active);
    }

    [Fact]
    public void Losing_access_is_announced_once_until_the_employee_reauthorizes()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Gmail, "juan.perez@gmail.com", Now);
        integration.DequeueDomainEvents();

        integration.MarkFailed("Sin acceso", Now.AddDays(1));
        integration.MarkFailed("Sin acceso", Now.AddDays(2));

        var lost = integration.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<IntegrationAccessLost>();
        lost.IntegrationId.ShouldBe(integration.Id);
        lost.EmployeeId.ShouldBe(EmployeeId);
        lost.Provider.ShouldBe(IntegrationProvider.Gmail);
        lost.OccurredAt.ShouldBe(Now.AddDays(1));

        integration.Authorize(OAuthGrant.From("access", "refresh", Now.AddDays(3)), Now.AddDays(3));
        integration.MarkFailed("Sin acceso", Now.AddDays(4));

        integration.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<IntegrationAccessLost>();
    }

    [Fact]
    public void A_chat_provider_never_holds_a_grant()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Telegram, "12345", Now);

        Should.Throw<DomainException>(() =>
            integration.Authorize(OAuthGrant.From("access", "refresh", Now.AddDays(180)), Now));
    }

    [Fact]
    public void A_grant_needs_an_access_token_but_the_refresh_token_is_optional()
    {
        Should.Throw<DomainException>(() => OAuthGrant.From(" ", "refresh", Now));

        var grant = OAuthGrant.From("access", " ", Now);

        grant.RefreshToken.ShouldBeNull();
        grant.CanRefresh.ShouldBeFalse();
    }

    [Fact]
    public void A_grant_knows_when_it_is_about_to_expire()
    {
        var grant = OAuthGrant.From("access", "refresh", Now.AddDays(2));

        grant.ExpiresWithin(TimeSpan.FromDays(1), Now).ShouldBeFalse();
        grant.ExpiresWithin(TimeSpan.FromDays(3), Now).ShouldBeTrue();
    }

    [Fact]
    public void Marking_a_sync_records_when_it_happened()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.MercadoPago, "228085066", Now);

        integration.MarkSynced(Now.AddHours(1));

        integration.LastSyncedAt.ShouldBe(Now.AddHours(1));
        integration.UpdatedAt.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void Iol_links_with_tokens_like_an_oauth_provider()
    {
        IntegrationProvider.Iol.LinksWithOAuth().ShouldBeTrue();
        IntegrationProvider.Iol.LinksWithCode().ShouldBeFalse();
    }

    [Fact]
    public void A_new_integration_starts_active_without_an_error()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Iol, "juan.perez", Now);

        integration.Status.ShouldBe(IntegrationStatus.Active);
        integration.ErrorReason.ShouldBeNull();
    }

    [Fact]
    public void Marking_a_failure_keeps_the_reason_to_show_the_employee()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Iol, "juan.perez", Now);

        integration.MarkFailed(" La autorizacion de IOL vencio. ", Now.AddHours(1));

        integration.Status.ShouldBe(IntegrationStatus.Error);
        integration.ErrorReason.ShouldBe("La autorizacion de IOL vencio.");
        integration.UpdatedAt.ShouldBe(Now.AddHours(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_failure_always_has_a_reason(string reason)
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Iol, "juan.perez", Now);

        Should.Throw<DomainException>(() => integration.MarkFailed(reason, Now));

        integration.Status.ShouldBe(IntegrationStatus.Active);
        integration.ErrorReason.ShouldBeNull();
    }

    [Fact]
    public void A_failure_reason_has_a_maximum_length()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Iol, "juan.perez", Now);

        Should.Throw<DomainException>(() =>
            integration.MarkFailed(new string('x', EmployeeIntegration.MaxErrorReasonLength + 1), Now));
    }

    [Fact]
    public void Authorizing_again_clears_a_previous_failure()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Iol, "juan.perez", Now);
        integration.MarkFailed("La autorizacion de IOL vencio.", Now);

        integration.Authorize(OAuthGrant.From("access", "refresh", Now.AddMinutes(15)), Now.AddDays(1));

        integration.Status.ShouldBe(IntegrationStatus.Active);
        integration.ErrorReason.ShouldBeNull();
    }
}
