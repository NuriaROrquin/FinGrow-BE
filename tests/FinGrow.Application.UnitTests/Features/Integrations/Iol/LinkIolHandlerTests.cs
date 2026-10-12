namespace FinGrow.Application.UnitTests.Features.Integrations.Iol;

using System.Net;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.GetIntegration;
using FinGrow.Application.Features.Integrations.Iol;
using FinGrow.Application.Features.Integrations.Iol.LinkIol;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class LinkIolHandlerTests
{
    private const string Password = "s3creta-IOL";
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeCurrentUser _currentUser = new() { UserId = EmployeeId };
    private readonly FakeIolAuthClient _iol = new();
    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task Valid_credentials_link_the_account_with_the_grant_and_no_password()
    {
        var result = await Handle(" Juan.Perez ");

        result.IsSuccess.ShouldBeTrue();
        _iol.Logins.ShouldBe(new[] { ("Juan.Perez", Password) });
        var integration = _integrations.Integrations.ShouldHaveSingleItem();
        integration.Provider.ShouldBe(IntegrationProvider.Iol);
        integration.EmployeeId.ShouldBe(EmployeeId);
        integration.ExternalAccountId.ShouldBe("juan.perez");
        integration.Status.ShouldBe(IntegrationStatus.Active);
        integration.Grant.ShouldNotBeNull();
        integration.Grant.AccessToken.ShouldBe("iol-access");
        integration.Grant.RefreshToken.ShouldBe("iol-refresh");
        integration.Grant.ExpiresAt.ShouldBe(Now.AddMinutes(15));
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_response_reports_the_link_without_echoing_the_credentials()
    {
        var result = await Handle("juan.perez");

        result.Value.ShouldBe(new IntegrationResponse(true, "juan.perez", Now, IntegrationStatus.Active));
        result.Value.ToString().ShouldNotContain(Password);
    }

    [Fact]
    public async Task Relinking_replaces_the_grant_and_clears_a_previous_failure()
    {
        var existing = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Iol, "juan.perez", Now.AddDays(-30));
        existing.Authorize(OAuthGrant.From("old-access", "old-refresh", Now.AddDays(-29)), Now.AddDays(-30));
        existing.MarkFailed("La autorizacion de IOL vencio.", Now.AddDays(-1));
        _integrations.Integrations.Add(existing);

        var result = await Handle("juan.perez");

        result.IsSuccess.ShouldBeTrue();
        _integrations.Integrations.ShouldHaveSingleItem().ShouldBeSameAs(existing);
        existing.Grant!.AccessToken.ShouldBe("iol-access");
        existing.Status.ShouldBe(IntegrationStatus.Active);
        existing.ErrorReason.ShouldBeNull();
        existing.LinkedAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task Rejected_credentials_return_a_validation_error_and_create_nothing(HttpStatusCode status)
    {
        _iol.Failure = new HttpRequestException("rejected", null, status);

        var result = await Handle("juan.perez");

        result.Error.ShouldBe(IolErrors.InvalidCredentials);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        _integrations.Integrations.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_unreachable_iol_is_reported_as_unavailable()
    {
        _iol.Failure = new HttpRequestException("connection refused", null, HttpStatusCode.ServiceUnavailable);

        var result = await Handle("juan.perez");

        result.Error.ShouldBe(IolErrors.Unavailable);
        _integrations.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_timeout_is_reported_as_unavailable()
    {
        _iol.Failure = new TaskCanceledException("timeout");

        var result = await Handle("juan.perez");

        result.Error.ShouldBe(IolErrors.Unavailable);
    }

    [Fact]
    public async Task An_account_linked_by_another_employee_is_rejected_before_talking_to_iol()
    {
        var other = EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.Iol, "juan.perez", Now);
        _integrations.Integrations.Add(other);

        var result = await Handle("Juan.Perez");

        result.Error.ShouldBe(IolErrors.AlreadyLinkedToAnotherEmployee);
        _iol.Logins.ShouldBeEmpty();
    }

    [Fact]
    public async Task Linking_requires_a_session()
    {
        _currentUser.UserId = null;

        var result = await Handle("juan.perez");

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        _iol.Logins.ShouldBeEmpty();
    }

    [Fact]
    public void The_command_never_prints_the_password()
    {
        var command = new LinkIolCommand("juan.perez", Password);

        command.ToString().ShouldNotContain(Password);
        command.ToString().ShouldContain("juan.perez");
    }

    [Theory]
    [InlineData("", Password)]
    [InlineData("juan.perez", "")]
    public void Username_and_password_are_required(string username, string password)
    {
        var result = new LinkIolCommandValidator().Validate(new LinkIolCommand(username, password));

        result.IsValid.ShouldBeFalse();
    }

    private Task<Result<IntegrationResponse>> Handle(string username) =>
        new LinkIolHandler(_currentUser, _iol, _integrations, _unitOfWork, _clock, NullLogger<LinkIolHandler>.Instance)
            .Handle(new LinkIolCommand(username, Password), CancellationToken.None);
}
