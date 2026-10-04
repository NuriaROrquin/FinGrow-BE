namespace FinGrow.Application.UnitTests.Features.Account;

using FinGrow.Application.Features.Account.Profile;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class UpdateProfileCommandHandlerTests
{
    private static readonly DateTimeOffset HiredAt = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private Employee AddLoggedInEmployee()
    {
        var employee = Employee.Create(
            Guid.NewGuid(),
            departmentId: null,
            "Ana Gomez",
            Email.From("ana.gomez@acme.com"),
            "+54 11 1234-5678",
            "hash",
            Currency.USD,
            DateOnly.FromDateTime(HiredAt.Date),
            HiredAt);

        _employees.Employees.Add(employee);
        _currentUser.UserId = employee.Id;
        return employee;
    }

    private UpdateProfileCommandHandler CreateHandler() =>
        new(_currentUser, _employees, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task Saves_the_new_name_and_phone()
    {
        var employee = AddLoggedInEmployee();

        var result = await CreateHandler().Handle(
            new UpdateProfileCommand("Ana Gómez", "+54 11 9999-0000"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.FullName.ShouldBe("Ana Gómez");
        employee.PhoneNumber.ShouldBe("+54 11 9999-0000");
        employee.UpdatedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Keeps_the_email_and_the_preferred_currency()
    {
        var employee = AddLoggedInEmployee();

        await CreateHandler().Handle(new UpdateProfileCommand("Ana Gómez", null), CancellationToken.None);

        employee.Email.Value.ShouldBe("ana.gomez@acme.com");
        employee.PreferredCurrency.ShouldBe(Currency.USD);
    }

    [Fact]
    public async Task An_empty_phone_removes_it()
    {
        var employee = AddLoggedInEmployee();

        await CreateHandler().Handle(new UpdateProfileCommand("Ana Gomez", "  "), CancellationToken.None);

        employee.PhoneNumber.ShouldBeNull();
    }

    [Fact]
    public async Task Requires_an_authenticated_user()
    {
        var result = await CreateHandler().Handle(
            new UpdateProfileCommand("Ana Gómez", null),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.NoAutenticado");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_rejects_an_empty_name(string fullName)
    {
        var validation = new UpdateProfileCommandValidator().Validate(new UpdateProfileCommand(fullName, null));

        validation.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validator_rejects_a_phone_longer_than_the_column()
    {
        var phoneNumber = new string('1', Employee.MaxPhoneNumberLength + 1);

        var validation = new UpdateProfileCommandValidator().Validate(new UpdateProfileCommand("Ana Gómez", phoneNumber));

        validation.IsValid.ShouldBeFalse();
    }
}
