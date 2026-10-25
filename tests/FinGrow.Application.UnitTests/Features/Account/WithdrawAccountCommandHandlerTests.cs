namespace FinGrow.Application.UnitTests.Features.Account;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Account.Withdraw;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class WithdrawAccountCommandHandlerTests
{
    private const string Password = "actual-1234";

    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeGmailOAuthClient _gmail = new();
    private readonly FakeEmployeeDataEraser _eraser = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeCurrentUser _currentUser = new();

    private Employee AddLoggedInEmployee()
    {
        var employee = Employee.Create(
            Guid.NewGuid(),
            departmentId: null,
            "Ana Gómez",
            Email.From("ana.gomez@acme.com"),
            "+541150000001",
            Password,
            Currency.ARS,
            new DateOnly(2025, 3, 1),
            Now.AddYears(-1));
        employee.UpdateProfile(
            employee.FullName,
            employee.PhoneNumber,
            "30123456",
            new DateOnly(1990, 5, 4),
            "Av. Siempre Viva 742",
            Now.AddDays(-30));

        _employees.Employees.Add(employee);
        _currentUser.UserId = employee.Id;
        _currentUser.CompanyId = employee.CompanyId;
        _currentUser.Role = Rol.Empleado;
        return employee;
    }

    private WithdrawAccountCommandHandler CreateHandler() =>
        new(
            _currentUser,
            _employees,
            _integrations,
            _gmail,
            new FakePasswordHasher(),
            _eraser,
            _refreshTokens,
            new FakeDateTimeProvider(Now),
            NullLogger<WithdrawAccountCommandHandler>.Instance);

    [Fact]
    public async Task Withdrawing_erases_the_financial_data_closes_every_session_and_keeps_the_employee_as_withdrawn()
    {
        var employee = AddLoggedInEmployee();
        var laptop = RefreshToken.Issue(employee.Id, employee.CompanyId, Rol.Empleado, RefreshToken.GenerateValue(), Now.AddDays(-1));
        var phone = RefreshToken.Issue(employee.Id, employee.CompanyId, Rol.Empleado, RefreshToken.GenerateValue(), Now.AddHours(-2));
        _refreshTokens.Add(laptop);
        _refreshTokens.Add(phone);

        var result = await CreateHandler().Handle(new WithdrawAccountCommand(Password), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _eraser.ErasedEmployees.ShouldBe(new[] { employee.Id });
        laptop.RevokedAt.ShouldBe(Now);
        phone.RevokedAt.ShouldBe(Now);

        _employees.Employees.ShouldContain(employee);
        employee.IsWithdrawn.ShouldBeTrue();
        employee.WithdrawnAt.ShouldBe(Now);
        employee.User.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task A_wrong_password_changes_nothing()
    {
        var employee = AddLoggedInEmployee();

        var result = await CreateHandler().Handle(new WithdrawAccountCommand("otra-clave"), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("Account.ContrasenaIncorrecta");
        employee.IsWithdrawn.ShouldBeFalse();
        employee.User.IsActive.ShouldBeTrue();
        _eraser.ErasedEmployees.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_gmail_permission_is_revoked_in_google()
    {
        var employee = AddLoggedInEmployee();
        var gmail = EmployeeIntegration.Create(employee.Id, IntegrationProvider.Gmail, "ana@gmail.com", Now.AddDays(-10));
        gmail.Authorize(OAuthGrant.From("access", "refresh", Now.AddHours(1)), Now.AddDays(-10));
        _integrations.Integrations.Add(gmail);

        await CreateHandler().Handle(new WithdrawAccountCommand(Password), CancellationToken.None);

        _gmail.RevokedTokens.ShouldHaveSingleItem().ShouldBe("refresh");
    }

    [Fact]
    public async Task Google_not_answering_does_not_stop_the_withdrawal()
    {
        var employee = AddLoggedInEmployee();
        var gmail = EmployeeIntegration.Create(employee.Id, IntegrationProvider.Gmail, "ana@gmail.com", Now.AddDays(-10));
        gmail.Authorize(OAuthGrant.From("access", "refresh", Now.AddHours(1)), Now.AddDays(-10));
        _integrations.Integrations.Add(gmail);
        _gmail.Unreachable = true;

        var result = await CreateHandler().Handle(new WithdrawAccountCommand(Password), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.IsWithdrawn.ShouldBeTrue();
        _eraser.ErasedEmployees.ShouldBe(new[] { employee.Id });
    }

    [Fact]
    public async Task Google_timing_out_does_not_stop_the_withdrawal()
    {
        var employee = AddLoggedInEmployee();
        var gmail = EmployeeIntegration.Create(employee.Id, IntegrationProvider.Gmail, "ana@gmail.com", Now.AddDays(-10));
        gmail.Authorize(OAuthGrant.From("access", "refresh", Now.AddHours(1)), Now.AddDays(-10));
        _integrations.Integrations.Add(gmail);
        _gmail.RevokeFailure = new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.");

        var result = await CreateHandler().Handle(new WithdrawAccountCommand(Password), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.IsWithdrawn.ShouldBeTrue();
        _eraser.ErasedEmployees.ShouldBe(new[] { employee.Id });
    }

    [Fact]
    public async Task Without_a_session_nothing_happens()
    {
        var result = await CreateHandler().Handle(new WithdrawAccountCommand(Password), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
        _eraser.ErasedEmployees.ShouldBeEmpty();
    }

    [Fact]
    public void The_password_is_not_printed_when_the_command_is_logged()
    {
        new WithdrawAccountCommand(Password).ToString().ShouldNotContain(Password);
    }
}
