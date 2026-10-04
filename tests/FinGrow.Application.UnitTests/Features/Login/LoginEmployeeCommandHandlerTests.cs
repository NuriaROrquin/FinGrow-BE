namespace FinGrow.Application.UnitTests.Features.Login;

using FinGrow.Application.Features.Login;
using FinGrow.Application.Features.Session;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class LoginEmployeeCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private static Employee CreateEmployee(string plainPassword = "1234", bool isActive = true)
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

        return employee;
    }

    private static SessionIssuer CreateSessionIssuer() =>
        new(new FakeTokenService(), new FakeRefreshTokenRepository(), new FakeDateTimeProvider(Now));

    private static LoginEmployeeCommandHandler CreateHandler(FakeEmployeeRepository employees, FakeUnitOfWork? unitOfWork = null) =>
        new(
            employees,
            new FakePasswordHasher(),
            CreateSessionIssuer(),
            new FakeTokenService(),
            unitOfWork ?? new FakeUnitOfWork(),
            new FakeDateTimeProvider(Now));

    [Fact]
    public async Task Valid_credentials_log_the_employee_in_and_return_a_token()
    {
        var employee = CreateEmployee(plainPassword: "1234");
        var employeeRepository = new FakeEmployeeRepository();
        employeeRepository.Employees.Add(employee);
        var unitOfWork = new FakeUnitOfWork();

        var handler = CreateHandler(employeeRepository, unitOfWork);

        var result = await handler.Handle(
            new LoginEmployeeCommand("empleado@empresa.com", "1234"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TwoFactorChallenge.ShouldBeNull();
        var session = result.Value.Session.ShouldNotBeNull();
        session.EmployeeId.ShouldBe(employee.Id);
        session.Token.ShouldBe($"token-for-{employee.Id}");
        session.ExpiresAt.ShouldBe(FakeTokenService.ExpiresAt);
        session.Role.ShouldBe("Empleado");
        session.CompanyId.ShouldBe(employee.CompanyId);
        employee.LastLoginAt.ShouldBe(Now);
        unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task With_two_factor_enabled_the_password_only_returns_a_challenge()
    {
        var employee = CreateEmployee(plainPassword: "1234");
        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);
        employee.EnableTwoFactor(Now);
        var employeeRepository = new FakeEmployeeRepository();
        employeeRepository.Employees.Add(employee);
        var unitOfWork = new FakeUnitOfWork();

        var handler = CreateHandler(employeeRepository, unitOfWork);

        var result = await handler.Handle(
            new LoginEmployeeCommand("empleado@empresa.com", "1234"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Session.ShouldBeNull();
        var challenge = result.Value.TwoFactorChallenge.ShouldNotBeNull();
        challenge.Value.ShouldBe($"challenge-for-{employee.Id}");
        employee.LastLoginAt.ShouldBeNull();
        unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_started_but_unconfirmed_enrollment_does_not_ask_for_a_code()
    {
        var employee = CreateEmployee(plainPassword: "1234");
        employee.StartTwoFactorEnrollment("SECRETBASE32", Now);
        var employeeRepository = new FakeEmployeeRepository();
        employeeRepository.Employees.Add(employee);

        var result = await CreateHandler(employeeRepository).Handle(
            new LoginEmployeeCommand("empleado@empresa.com", "1234"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Session.ShouldNotBeNull();
        result.Value.TwoFactorChallenge.ShouldBeNull();
    }

    [Fact]
    public async Task Wrong_password_returns_the_generic_error()
    {
        var employee = CreateEmployee(plainPassword: "1234");
        var employeeRepository = new FakeEmployeeRepository();
        employeeRepository.Employees.Add(employee);

        var result = await CreateHandler(employeeRepository).Handle(
            new LoginEmployeeCommand("empleado@empresa.com", "123"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task Nonexistent_email_returns_the_same_generic_error_as_wrong_password()
    {
        var result = await CreateHandler(new FakeEmployeeRepository()).Handle(
            new LoginEmployeeCommand("nadie@empresa.com", "1234"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task Deactivated_employee_is_rejected_even_with_correct_credentials()
    {
        var employee = CreateEmployee(plainPassword: "1234", isActive: false);
        var employeeRepository = new FakeEmployeeRepository();
        employeeRepository.Employees.Add(employee);

        var result = await CreateHandler(employeeRepository).Handle(
            new LoginEmployeeCommand("empleado@empresa.com", "1234"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }
}
