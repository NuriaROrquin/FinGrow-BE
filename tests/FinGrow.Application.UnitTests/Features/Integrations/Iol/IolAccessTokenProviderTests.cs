namespace FinGrow.Application.UnitTests.Features.Integrations.Iol;

using System.Net;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Iol;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class IolAccessTokenProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeIolAuthClient _iol = new() { Tokens = new("new-access", "new-refresh", TimeSpan.FromMinutes(15)) };
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task A_bearer_with_time_left_is_used_as_is()
    {
        var integration = LinkedIntegration(expiresAt: Now.AddMinutes(10));

        var result = await GetAccessToken(integration);

        result.Value.ShouldBe("access");
        _iol.Refreshes.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_bearer_about_to_expire_is_renewed_and_the_new_pair_is_stored()
    {
        var integration = LinkedIntegration(expiresAt: Now.AddMinutes(1));

        var result = await GetAccessToken(integration);

        result.Value.ShouldBe("new-access");
        _iol.Refreshes.ShouldHaveSingleItem().ShouldBe("refresh");
        integration.Grant!.AccessToken.ShouldBe("new-access");
        integration.Grant.RefreshToken.ShouldBe("new-refresh");
        integration.Grant.ExpiresAt.ShouldBe(Now.AddMinutes(15));
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_expired_bearer_is_renewed()
    {
        var integration = LinkedIntegration(expiresAt: Now.AddHours(-3));

        var result = await GetAccessToken(integration);

        result.Value.ShouldBe("new-access");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task A_rejected_refresh_token_leaves_the_integration_in_error_with_its_reason(HttpStatusCode status)
    {
        var integration = LinkedIntegration(expiresAt: Now.AddMinutes(-1));
        _iol.Failure = new HttpRequestException("invalid_grant", null, status);

        var result = await GetAccessToken(integration);

        result.Error.ShouldBe(IolErrors.GrantExpired);
        integration.Status.ShouldBe(IntegrationStatus.Error);
        integration.ErrorReason.ShouldBe(IolErrors.GrantExpired.Description);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_integration_in_error_does_not_call_iol_again()
    {
        var integration = LinkedIntegration(expiresAt: Now.AddMinutes(-1));
        integration.MarkFailed(IolErrors.GrantExpired.Description, Now.AddHours(-1));

        var result = await GetAccessToken(integration);

        result.Error.ShouldBe(IolErrors.GrantExpired);
        _iol.Refreshes.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_outage_while_renewing_keeps_using_a_bearer_that_has_not_expired_yet()
    {
        var integration = LinkedIntegration(expiresAt: Now.AddMinutes(1));
        _iol.Failure = new HttpRequestException("bad gateway", null, HttpStatusCode.BadGateway);

        var result = await GetAccessToken(integration);

        result.Value.ShouldBe("access");
        integration.Status.ShouldBe(IntegrationStatus.Active);
    }

    [Fact]
    public async Task An_outage_with_an_expired_bearer_is_unavailable_but_not_an_error_of_the_integration()
    {
        var integration = LinkedIntegration(expiresAt: Now.AddMinutes(-1));
        _iol.Failure = new TaskCanceledException("timeout");

        var result = await GetAccessToken(integration);

        result.Error.ShouldBe(IolErrors.Unavailable);
        integration.Status.ShouldBe(IntegrationStatus.Active);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Another_provider_is_not_an_iol_link()
    {
        var integration = EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.MercadoPago, "228085066", Now);
        integration.Authorize(OAuthGrant.From("access", "refresh", Now.AddDays(1)), Now);

        var result = await GetAccessToken(integration);

        result.Error.ShouldBe(IolErrors.NotLinked);
    }

    private static EmployeeIntegration LinkedIntegration(DateTimeOffset expiresAt)
    {
        var integration = EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.Iol, "juan.perez", Now.AddDays(-1));
        integration.Authorize(OAuthGrant.From("access", "refresh", expiresAt), Now.AddDays(-1));

        return integration;
    }

    private Task<Result<string>> GetAccessToken(EmployeeIntegration integration) =>
        new IolAccessTokenProvider(_iol, _unitOfWork, _clock, NullLogger<IolAccessTokenProvider>.Instance)
            .GetAccessTokenAsync(integration, CancellationToken.None);
}
