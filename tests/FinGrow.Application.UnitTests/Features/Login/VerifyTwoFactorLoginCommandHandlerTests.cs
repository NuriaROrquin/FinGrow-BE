namespace FinGrow.Application.UnitTests.Features.Login;

using FinGrow.Application.Features.Login;
using FinGrow.Application.Features.Session;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class VerifyTwoFactorLoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTokenService _tokenService = new();

    private static Employee CreateEmployee(bool twoFactorEnabled = true)
    {
        var employee = Employee.Create(
            Guid.NewGuid(),
            departmentId: null,
            "Empleado Test",
            Email.From("empleado@empresa.com"),
            phoneNumber: null,
            "1234",
            Currency.ARS,
            DateOnly.FromDateTime(Now.Date),
            Now);

        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);

        if (twoFactorEnabled)
        {
            employee.EnableTwoFactor(Now);
        }

        return employee;
    }

    private VerifyTwoFactorLoginCommandHandler CreateHandler() =>
        new(
            _tokenService,
            new FakeTotpService(),
            _employees,
            new SessionIssuer(_tokenService, _refreshTokens, new FakeDateTimeProvider(Now)),
            _unitOfWork,
            new FakeDateTimeProvider(Now));

    [Fact]
    public async Task A_valid_code_completes_the_login_and_issues_the_session()
    {
        var employee = CreateEmployee();
        _employees.Employees.Add(employee);
        var challenge = _tokenService.GenerateTwoFactorChallenge(employee.Id).Value;

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(challenge, FakeTotpService.ValidCode),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.EmployeeId.ShouldBe(employee.Id);
        result.Value.Token.ShouldBe($"token-for-{employee.Id}");
        employee.LastLoginAt.ShouldBe(Now);
        _refreshTokens.Tokens.Count.ShouldBe(1);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_invalid_code_rejects_the_login()
    {
        var employee = CreateEmployee();
        _employees.Employees.Add(employee);
        var challenge = _tokenService.GenerateTwoFactorChallenge(employee.Id).Value;

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(challenge, "000000"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CodigoInvalido");
        employee.LastLoginAt.ShouldBeNull();
        _refreshTokens.Tokens.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_invalid_challenge_is_rejected_even_with_a_valid_code()
    {
        _employees.Employees.Add(CreateEmployee());

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand("not-a-challenge", FakeTotpService.ValidCode),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.DesafioInvalido");
    }

    [Fact]
    public async Task A_challenge_for_an_employee_without_two_factor_is_rejected()
    {
        var employee = CreateEmployee(twoFactorEnabled: false);
        _employees.Employees.Add(employee);
        var challenge = _tokenService.GenerateTwoFactorChallenge(employee.Id).Value;

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(challenge, FakeTotpService.ValidCode),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.DesafioInvalido");
    }

    [Fact]
    public async Task A_deactivated_employee_cannot_complete_the_login()
    {
        var employee = CreateEmployee();
        employee.Deactivate(Now);
        _employees.Employees.Add(employee);
        var challenge = _tokenService.GenerateTwoFactorChallenge(employee.Id).Value;

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(challenge, FakeTotpService.ValidCode),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.DesafioInvalido");
    }
}
