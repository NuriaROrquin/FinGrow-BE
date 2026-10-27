namespace FinGrow.Application.UnitTests.Features.Session;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Session;
using FinGrow.Application.Features.Session.CloseOtherSessions;
using FinGrow.Application.Features.Session.CloseSession;
using FinGrow.Application.Features.Session.ListSessions;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class SessionManagementTests
{
    private const string ChromeOnWindows =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";

    private const string SafariOnIphone =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Mobile/15E148 Safari/604.1";

    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();

    public SessionManagementTests()
    {
        _currentUser.UserId = _userId;
        _currentUser.CompanyId = _companyId;
        _currentUser.Role = Rol.Empleado;
    }

    private (RefreshToken Token, string Value) OpenSession(
        string? userAgent,
        DateTimeOffset? at = null,
        Guid? userId = null,
        string role = Rol.Empleado)
    {
        var value = RefreshToken.GenerateValue();
        var token = RefreshToken.Issue(userId ?? _userId, _companyId, role, value, at ?? Now, userAgent);
        _refreshTokens.Add(token);
        return (token, value);
    }

    // La sesion actual es la del access token del pedido (HU-73).
    private Task<Result<IReadOnlyList<ActiveSessionResponse>>> ListAsync(Guid? currentSessionId)
    {
        _currentUser.SessionId = currentSessionId;
        return new ListSessionsQueryHandler(_currentUser, _refreshTokens, _clock)
            .Handle(new ListSessionsQuery(), CancellationToken.None);
    }

    private Task<Result> CloseAsync(Guid sessionId, Guid? currentSessionId)
    {
        _currentUser.SessionId = currentSessionId;
        return new CloseSessionCommandHandler(_currentUser, _refreshTokens, _unitOfWork, _clock)
            .Handle(new CloseSessionCommand(sessionId), CancellationToken.None);
    }

    private Task<Result> CloseOthersAsync(Guid? currentSessionId)
    {
        _currentUser.SessionId = currentSessionId;
        return new CloseOtherSessionsCommandHandler(_currentUser, _refreshTokens, _clock)
            .Handle(new CloseOtherSessionsCommand(), CancellationToken.None);
    }

    [Fact]
    public async Task Listing_shows_one_row_per_open_session_with_device_and_dates_and_marks_the_current_one()
    {
        var (laptop, _) = OpenSession(ChromeOnWindows, Now.AddDays(-3));
        var (phone, _) = OpenSession(SafariOnIphone, Now.AddDays(-1));

        var result = await ListAsync(laptop.SessionId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);

        var current = result.Value[0];
        current.Id.ShouldBe(laptop.SessionId);
        current.IsCurrent.ShouldBeTrue();
        current.Browser.ShouldBe("Chrome");
        current.OperatingSystem.ShouldBe("Windows");
        current.StartedAt.ShouldBe(Now.AddDays(-3));

        var other = result.Value[1];
        other.Id.ShouldBe(phone.SessionId);
        other.IsCurrent.ShouldBeFalse();
        other.Browser.ShouldBe("Safari");
        other.OperatingSystem.ShouldBe("iOS");
    }

    [Fact]
    public async Task A_renewed_session_keeps_its_start_and_moves_its_last_activity()
    {
        var (token, _) = OpenSession(ChromeOnWindows, Now.AddDays(-2));
        var renewed = token.Rotate(RefreshToken.GenerateValue(), Now.AddHours(-1));
        _refreshTokens.Add(renewed);

        var result = await ListAsync(currentSessionId: null);

        var session = result.Value.ShouldHaveSingleItem();
        session.Id.ShouldBe(token.SessionId);
        session.StartedAt.ShouldBe(Now.AddDays(-2));
        session.LastActiveAt.ShouldBe(Now.AddHours(-1));
    }

    [Fact]
    public async Task Expired_and_revoked_sessions_are_not_listed()
    {
        OpenSession(ChromeOnWindows, Now.Subtract(RefreshToken.Lifetime).AddMinutes(-1));
        var (revoked, _) = OpenSession(ChromeOnWindows);
        revoked.Revoke(Now);
        var (open, _) = OpenSession(SafariOnIphone);

        var result = await ListAsync(currentSessionId: null);

        result.Value.ShouldHaveSingleItem().Id.ShouldBe(open.SessionId);
    }

    [Fact]
    public async Task Sessions_of_other_users_are_not_listed()
    {
        OpenSession(ChromeOnWindows, userId: Guid.NewGuid());

        var result = await ListAsync(currentSessionId: null);

        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_unknown_user_agent_is_listed_without_inventing_a_device()
    {
        OpenSession(userAgent: null);

        var session = (await ListAsync(currentSessionId: null)).Value.ShouldHaveSingleItem();

        session.Browser.ShouldBeNull();
        session.OperatingSystem.ShouldBeNull();
    }

    [Fact]
    public async Task A_company_lists_its_own_sessions_the_same_way()
    {
        _currentUser.Role = Rol.Empresa;
        var (token, _) = OpenSession(ChromeOnWindows, role: Rol.Empresa);

        var session = (await ListAsync(token.SessionId)).Value.ShouldHaveSingleItem();

        session.Id.ShouldBe(token.SessionId);
        session.IsCurrent.ShouldBeTrue();
    }

    [Fact]
    public async Task Closing_another_session_revokes_its_token()
    {
        var (current, _) = OpenSession(ChromeOnWindows);
        var (phone, _) = OpenSession(SafariOnIphone);

        var result = await CloseAsync(phone.SessionId, current.SessionId);

        result.IsSuccess.ShouldBeTrue();
        phone.RevokedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Closing_a_renewed_session_revokes_the_token_it_is_using_now()
    {
        var (current, _) = OpenSession(ChromeOnWindows);
        var (phone, _) = OpenSession(SafariOnIphone, Now.AddDays(-1));
        var renewed = phone.Rotate(RefreshToken.GenerateValue(), Now.AddHours(-1));
        _refreshTokens.Add(renewed);

        var result = await CloseAsync(phone.SessionId, current.SessionId);

        result.IsSuccess.ShouldBeTrue();
        renewed.RevokedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task The_current_session_is_not_closed_from_the_list()
    {
        var (current, _) = OpenSession(ChromeOnWindows);

        var result = await CloseAsync(current.SessionId, current.SessionId);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        current.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Closing_a_session_of_another_user_is_forbidden()
    {
        var (current, _) = OpenSession(ChromeOnWindows);
        var (foreign, _) = OpenSession(SafariOnIphone, userId: Guid.NewGuid());

        var result = await CloseAsync(foreign.SessionId, current.SessionId);

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        foreign.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Closing_an_already_closed_session_is_not_found()
    {
        var (current, _) = OpenSession(ChromeOnWindows);
        var (phone, _) = OpenSession(SafariOnIphone);
        phone.Revoke(Now.AddMinutes(-5));

        var result = await CloseAsync(phone.SessionId, current.SessionId);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        phone.RevokedAt.ShouldBe(Now.AddMinutes(-5));
    }

    [Fact]
    public async Task Closing_the_other_sessions_keeps_only_the_current_one()
    {
        var (current, _) = OpenSession(ChromeOnWindows);
        var (phone, _) = OpenSession(SafariOnIphone);
        var (tablet, _) = OpenSession(userAgent: null);
        var (foreign, _) = OpenSession(ChromeOnWindows, userId: Guid.NewGuid());

        var result = await CloseOthersAsync(current.SessionId);

        result.IsSuccess.ShouldBeTrue();
        current.RevokedAt.ShouldBeNull();
        phone.RevokedAt.ShouldBe(Now);
        tablet.RevokedAt.ShouldBe(Now);
        foreign.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Without_a_current_session_nothing_is_closed()
    {
        var (phone, _) = OpenSession(SafariOnIphone);

        var result = await CloseOthersAsync(currentSessionId: null);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        phone.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Refreshing_keeps_the_session_and_records_the_device()
    {
        var employees = new FakeEmployeeRepository();
        var employee = Employee.Create(
            _companyId,
            departmentId: null,
            "Ana Gómez",
            Email.From("ana.gomez@acme.com"),
            phoneNumber: null,
            "hash",
            Currency.ARS,
            DateOnly.FromDateTime(Now.Date),
            Now);
        employees.Employees.Add(employee);

        var value = RefreshToken.GenerateValue();
        var token = RefreshToken.Issue(employee.Id, _companyId, Rol.Empleado, value, Now.AddDays(-1), userAgent: null);
        _refreshTokens.Add(token);

        var device = new FakeClientDevice { UserAgent = ChromeOnWindows };
        var companies = new FakeCompanyRepository();
        var handler = new RefreshSessionCommandHandler(
            _refreshTokens,
            new FakeUserRepository(employees, companies),
            new SessionAccountResolver(employees, companies),
            new SessionIssuer(new FakeTokenService(), _refreshTokens, _clock, device),
            _unitOfWork,
            _clock);

        var result = await handler.Handle(new RefreshSessionCommand(value), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        token.RevokedAt.ShouldBe(Now);
        var renewed = _refreshTokens.Tokens.Single(t => t.TokenHash == RefreshToken.Hash(result.Value.RefreshToken));
        renewed.SessionId.ShouldBe(token.SessionId);
        renewed.StartedAt.ShouldBe(Now.AddDays(-1));
        renewed.UserAgent.ShouldBe(ChromeOnWindows);
    }
}
