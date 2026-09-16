namespace FinGrow.Application.UnitTests.Features.Login;

using FinGrow.Application.Features.Login;
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

    [Fact]
    public async Task Valid_credentials_log_the_employee_in_and_return_a_token()
    {
        var employee = CreateEmployee(plainPassword: "1234");
        var employeeRepository = new FakeEmployeeRepository();
        employeeRepository.Employees.Add(employee);
        var unitOfWork = new FakeUnitOfWork();

        var handler = new LoginEmployeeCommandHandler(
            employeeRepository,
            new FakePasswordHasher(),
            new FakeTokenService(),
            unitOfWork,
            new FakeDateTimeProvider(Now));

        var result = await handler.Handle(
            new LoginEmployeeCommand("empleado@empresa.com", "1234"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.EmployeeId.ShouldBe(employee.Id);
        result.Value.Token.ShouldBe($"token-for-{employee.Id}");
        result.Value.ExpiresAt.ShouldBe(FakeTokenService.ExpiresAt);
        result.Value.Role.ShouldBe("Empleado");
        result.Value.CompanyId.ShouldBe(employee.CompanyId);
        employee.LastLoginAt.ShouldBe(Now);
        unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Wrong_password_returns_the_generic_error()
    {
        var employee = CreateEmployee(plainPassword: "1234");
        var employeeRepository = new FakeEmployeeRepository();
        employeeRepository.Employees.Add(employee);

        var handler = new LoginEmployeeCommandHandler(
            employeeRepository,
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeUnitOfWork(),
            new FakeDateTimeProvider(Now));

        var result = await handler.Handle(
            new LoginEmployeeCommand("empleado@empresa.com", "123"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }

    [Fact]
    public async Task Nonexistent_email_returns_the_same_generic_error_as_wrong_password()
    {
        var handler = new LoginEmployeeCommandHandler(
            new FakeEmployeeRepository(),
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeUnitOfWork(),
            new FakeDateTimeProvider(Now));

        var result = await handler.Handle(
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

        var handler = new LoginEmployeeCommandHandler(
            employeeRepository,
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeUnitOfWork(),
            new FakeDateTimeProvider(Now));

        var result = await handler.Handle(
            new LoginEmployeeCommand("empleado@empresa.com", "1234"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CredencialesInvalidas");
    }
}
