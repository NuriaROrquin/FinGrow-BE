namespace FinGrow.Application.UnitTests.Features.Account;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Account.ChangePassword;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class ChangePasswordCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private Employee AddLoggedInEmployee(string password = "actual-1234")
    {
        var employee = Employee.Create(
            Guid.NewGuid(),
            departmentId: null,
            "Empleado Test",
            Email.From("empleado@empresa.com"),
            phoneNumber: null,
            password,
            Currency.ARS,
            DateOnly.FromDateTime(Now.Date),
            Now);

        _employees.Employees.Add(employee);
        _currentUser.UserId = employee.Id;
        _currentUser.Role = Rol.Empleado;
        return employee;
    }

    private RefreshToken AddOpenSession(Guid userId, Guid companyId)
    {
        var token = RefreshToken.Issue(userId, companyId, Rol.Empleado, RefreshToken.GenerateValue(), Now);
        _refreshTokens.Add(token);
        return token;
    }

    private ChangePasswordCommandHandler CreateHandler() =>
        new(
            _currentUser,
            _employees,
            new FakeCompanyRepository(),
            new FakePasswordHasher(),
            _refreshTokens,
            _unitOfWork,
            new FakeDateTimeProvider(Now));

    [Fact]
    public async Task Changing_the_password_stores_the_new_hash_and_closes_every_open_session()
    {
        var employee = AddLoggedInEmployee();
        var laptop = AddOpenSession(employee.Id, employee.CompanyId);
        var phone = AddOpenSession(employee.Id, employee.CompanyId);

        var result = await CreateHandler().Handle(
            new ChangePasswordCommand("actual-1234", "nueva-5678"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.PasswordHash.ShouldBe("nueva-5678");
        laptop.RevokedAt.ShouldBe(Now);
        phone.RevokedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Sessions_of_other_users_stay_open()
    {
        var employee = AddLoggedInEmployee();
        var otherUserSession = AddOpenSession(Guid.NewGuid(), employee.CompanyId);

        await CreateHandler().Handle(new ChangePasswordCommand("actual-1234", "nueva-5678"), CancellationToken.None);

        otherUserSession.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task A_wrong_current_password_changes_nothing()
    {
        var employee = AddLoggedInEmployee();
        var session = AddOpenSession(employee.Id, employee.CompanyId);

        var result = await CreateHandler().Handle(
            new ChangePasswordCommand("incorrecta", "nueva-5678"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.ContrasenaActualIncorrecta");
        employee.PasswordHash.ShouldBe("actual-1234");
        session.RevokedAt.ShouldBeNull();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_anonymous_request_is_rejected()
    {
        var result = await CreateHandler().Handle(
            new ChangePasswordCommand("actual-1234", "nueva-5678"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.NoAutenticado");
    }

    [Theory]
    [InlineData("actual-1234", "corta")]
    [InlineData("actual-1234", "actual-1234")]
    [InlineData("", "nueva-5678")]
    public void The_validator_rejects_invalid_requests(string currentPassword, string newPassword)
    {
        var validation = new ChangePasswordCommandValidator().Validate(new ChangePasswordCommand(currentPassword, newPassword));

        validation.IsValid.ShouldBeFalse();
    }
}
