namespace FinGrow.Application.UnitTests.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Login;
using FinGrow.Application.Features.Session;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class LoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeCompanyRepository _companies = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private Employee AddEmployee(string plainPassword = "1234", bool isActive = true)
    {
        var employee = Employee.Create(
            companyId: Guid.NewGuid(),
            departmentId: null,
            fullName: "Empleado Test",
            email: Email.From("empleado@empresa.com"),
            phoneNumber: null,
            passwordHash: plainPassword,
            preferredCurrency: Currency.ARS,
            hiredOn: DateOnly.FromDateTime(Now.Date),
            createdAt: Now);

        if (!isActive)
        {
            employee.Deactivate(Now);
        }

        _employees.Employees.Add(employee);
        return employee;
    }

    private Company AddCompany(string plainPassword = "1234", bool isActive = true)
    {
        var company = Company.Create(
            "Empresa Demo S.A.",
            TaxId.From("20123456786"),
            Email.From("empresa@empresa.com"),
            plainPassword,
            Currency.ARS,
            Now);

        if (!isActive)
        {
            company.Deactivate(Now);
        }

        _companies.Companies.Add(company);
        return company;
    }

    private LoginCommandHandler CreateHandler()
    {
        var tokenService = new FakeTokenService();

        return new LoginCommandHandler(
            new FakeUserRepository(_employees, _companies),
            new SessionAccountResolver(_employees, _companies),
            new FakePasswordHasher(),
            new SessionIssuer(tokenService, new FakeRefreshTokenRepository(), new FakeDateTimeProvider(Now)),
            tokenService,
            _unitOfWork,
            new FakeDateTimeProvider(Now));
    }

    private Task<Result<FinGrow.Application.DTOs.LoginResult>> LoginAsync(string email, string password, UserRole role) =>
        CreateHandler().Handle(new LoginCommand(email, password, role), CancellationToken.None);

    [Fact]
    public async Task Valid_credentials_log_the_employee_in_and_return_a_token()
    {
        var employee = AddEmployee(plainPassword: "1234");

        var result = await LoginAsync("empleado@empresa.com", "1234", UserRole.Employee);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TwoFactorChallenge.ShouldBeNull();
        var session = result.Value.Session.ShouldNotBeNull();
        session.EmployeeId.ShouldBe(employee.Id);
        session.Token.ShouldBe($"token-for-{employee.Id}");
        session.ExpiresAt.ShouldBe(FakeTokenService.ExpiresAt);
        session.Role.ShouldBe("Empleado");
        session.CompanyId.ShouldBe(employee.CompanyId);
        employee.User.LastLoginAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Valid_credentials_log_the_company_in_and_return_a_token()
    {
        var company = AddCompany(plainPassword: "1234");

        var result = await LoginAsync("empresa@empresa.com", "1234", UserRole.Company);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TwoFactorChallenge.ShouldBeNull();

        var session = result.Value.Session.ShouldNotBeNull();
        session.EmployeeId.ShouldBe(company.Id);
        session.CompanyId.ShouldBe(company.Id);
        session.FullName.ShouldBe(company.Name);
        session.Role.ShouldBe("Empresa");
        session.Token.ShouldBe($"token-for-{company.Id}");
        session.ExpiresAt.ShouldBe(FakeTokenService.ExpiresAt);
        company.User.LastLoginAt.ShouldBe(Now);
    }

    [Fact]
    public async Task With_two_factor_enabled_the_password_only_returns_a_challenge()
    {
        var employee = AddEmployee(plainPassword: "1234");
        employee.User.StartTwoFactorEnrollment("SECRETBASE32", Now);
        employee.User.EnableTwoFactor(Now);

        var result = await LoginAsync("empleado@empresa.com", "1234", UserRole.Employee);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Session.ShouldBeNull();
        var challenge = result.Value.TwoFactorChallenge.ShouldNotBeNull();
        challenge.Value.ShouldBe($"challenge-for-{employee.Id}");
        employee.User.LastLoginAt.ShouldBeNull();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Company_with_two_factor_gets_a_challenge_instead_of_a_session()
    {
        var company = AddCompany(plainPassword: "1234");
        company.User.StartTwoFactorEnrollment("SECRETBASE32", Now);
        company.User.EnableTwoFactor(Now);

        var result = await LoginAsync("empresa@empresa.com", "1234", UserRole.Company);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Session.ShouldBeNull();
        result.Value.TwoFactorChallenge.ShouldNotBeNull().Value.ShouldBe($"challenge-for-{company.Id}");
    }

    [Theory]
    [InlineData(UserRole.Employee)]
    [InlineData(UserRole.Company)]
    public async Task A_started_but_unconfirmed_enrollment_does_not_ask_for_a_code(UserRole role)
    {
        var user = role == UserRole.Company ? AddCompany().User : AddEmployee().User;
        user.StartTwoFactorEnrollment("SECRETBASE32", Now);

        var result = await LoginAsync(user.Email.Value, "1234", role);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Session.ShouldNotBeNull();
        result.Value.TwoFactorChallenge.ShouldBeNull();
    }

    [Theory]
    [InlineData(UserRole.Employee)]
    [InlineData(UserRole.Company)]
    public async Task Wrong_password_returns_the_generic_error(UserRole role)
    {
        var user = role == UserRole.Company ? AddCompany().User : AddEmployee().User;

        var result = await LoginAsync(user.Email.Value, "wrong", role);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task Wrong_password_never_reveals_that_two_factor_is_enabled()
    {
        var company = AddCompany(plainPassword: "1234");
        company.User.StartTwoFactorEnrollment("SECRETBASE32", Now);
        company.User.EnableTwoFactor(Now);

        var result = await LoginAsync("empresa@empresa.com", "wrong", UserRole.Company);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Theory]
    [InlineData(UserRole.Employee)]
    [InlineData(UserRole.Company)]
    public async Task Nonexistent_email_returns_the_same_generic_error_as_wrong_password(UserRole role)
    {
        var result = await LoginAsync("nadie@empresa.com", "1234", role);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task An_employee_cannot_log_in_through_the_company_login()
    {
        AddEmployee(plainPassword: "1234");

        var result = await LoginAsync("empleado@empresa.com", "1234", UserRole.Company);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task A_company_cannot_log_in_through_the_employee_login()
    {
        AddCompany(plainPassword: "1234");

        var result = await LoginAsync("empresa@empresa.com", "1234", UserRole.Employee);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Theory]
    [InlineData(UserRole.Employee)]
    [InlineData(UserRole.Company)]
    public async Task A_deactivated_account_is_told_that_the_access_is_disabled_after_the_password_matches(UserRole role)
    {
        var user = role == UserRole.Company ? AddCompany(isActive: false).User : AddEmployee(isActive: false).User;

        var result = await LoginAsync(user.Email.Value, "1234", role);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.AccesoDesactivado");
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Deactivated_employee_with_a_wrong_password_gets_the_generic_error()
    {
        AddEmployee(plainPassword: "1234", isActive: false);

        var result = await LoginAsync("empleado@empresa.com", "otra", UserRole.Employee);

        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }
}
