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
    private readonly FakeCompanyRepository _companies = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTokenService _tokenService = new();

    private Employee AddEmployee(bool twoFactorEnabled = true)
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

        employee.User.StartTwoFactorEnrollment("SECRETBASE32", Now);

        if (twoFactorEnabled)
        {
            employee.User.EnableTwoFactor(Now);
        }

        _employees.Employees.Add(employee);
        return employee;
    }

    private Company AddCompany()
    {
        var company = Company.Create(
            "Empresa Demo S.A.",
            TaxId.From("20123456786"),
            Email.From("empresa@empresa.com"),
            "1234",
            Currency.ARS,
            Now);

        company.User.StartTwoFactorEnrollment("SECRETBASE32", Now);
        company.User.EnableTwoFactor(Now);
        _companies.Companies.Add(company);
        return company;
    }

    private VerifyTwoFactorLoginCommandHandler CreateHandler() =>
        new(
            _tokenService,
            new FakeTotpService(),
            new FakeUserRepository(_employees, _companies),
            new SessionAccountResolver(_employees, _companies),
            new SessionIssuer(_tokenService, _refreshTokens, new FakeDateTimeProvider(Now)),
            _unitOfWork,
            new FakeDateTimeProvider(Now));

    private string ChallengeFor(Guid userId) => _tokenService.GenerateTwoFactorChallenge(userId).Value;

    [Fact]
    public async Task A_valid_code_completes_the_login_and_issues_the_session()
    {
        var employee = AddEmployee();

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(ChallengeFor(employee.Id), FakeTotpService.ValidCode, UserRole.Employee),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.EmployeeId.ShouldBe(employee.Id);
        result.Value.Token.ShouldBe($"token-for-{employee.Id}");
        employee.User.LastLoginAt.ShouldBe(Now);
        _refreshTokens.Tokens.Count.ShouldBe(1);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_valid_code_completes_the_login_of_a_company()
    {
        var company = AddCompany();

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(ChallengeFor(company.Id), FakeTotpService.ValidCode, UserRole.Company),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Role.ShouldBe("Empresa");
        result.Value.CompanyId.ShouldBe(company.Id);
        company.User.LastLoginAt.ShouldBe(Now);
    }

    [Fact]
    public async Task An_invalid_code_rejects_the_login()
    {
        var employee = AddEmployee();

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(ChallengeFor(employee.Id), "000000", UserRole.Employee),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CodigoInvalido");
        employee.User.LastLoginAt.ShouldBeNull();
        _refreshTokens.Tokens.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_invalid_challenge_is_rejected_even_with_a_valid_code()
    {
        AddEmployee();

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand("not-a-challenge", FakeTotpService.ValidCode, UserRole.Employee),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.DesafioInvalido");
    }

    [Fact]
    public async Task A_challenge_issued_for_someone_else_is_rejected()
    {
        AddCompany();

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(ChallengeFor(Guid.NewGuid()), FakeTotpService.ValidCode, UserRole.Company),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Auth.DesafioInvalido");
    }

    [Fact]
    public async Task A_challenge_of_an_employee_is_rejected_by_the_company_login()
    {
        var employee = AddEmployee();

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(ChallengeFor(employee.Id), FakeTotpService.ValidCode, UserRole.Company),
            CancellationToken.None);

        result.Error.Code.ShouldBe("Auth.DesafioInvalido");
        _refreshTokens.Tokens.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_challenge_for_an_employee_without_two_factor_is_rejected()
    {
        var employee = AddEmployee(twoFactorEnabled: false);

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(ChallengeFor(employee.Id), FakeTotpService.ValidCode, UserRole.Employee),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.DesafioInvalido");
    }

    [Fact]
    public async Task A_deactivated_employee_cannot_complete_the_login()
    {
        var employee = AddEmployee();
        employee.Deactivate(Now);

        var result = await CreateHandler().Handle(
            new VerifyTwoFactorLoginCommand(ChallengeFor(employee.Id), FakeTotpService.ValidCode, UserRole.Employee),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.DesafioInvalido");
    }
}
