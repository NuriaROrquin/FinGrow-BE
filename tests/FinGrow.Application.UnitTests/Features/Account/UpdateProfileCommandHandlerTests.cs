namespace FinGrow.Application.UnitTests.Features.Account;

using FinGrow.Application.Features.Account.Profile;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class UpdateProfileCommandHandlerTests
{
    private static readonly DateTimeOffset HiredAt = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 4);

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

    private static UpdateProfileCommand Command(
        string fullName = "Ana Gómez",
        string? phoneNumber = null,
        string? nationalId = null,
        DateOnly? birthDate = null,
        string? address = null) =>
        new(fullName, phoneNumber, nationalId, birthDate, address);

    private static bool IsValid(UpdateProfileCommand command) =>
        new UpdateProfileCommandValidator(new FakeDateTimeProvider(Now)).Validate(command).IsValid;

    [Fact]
    public async Task Saves_every_editable_field()
    {
        var employee = AddLoggedInEmployee();

        var result = await CreateHandler().Handle(
            Command(
                phoneNumber: "+54 11 9999-0000",
                nationalId: "30123456",
                birthDate: new DateOnly(1990, 5, 20),
                address: "Av. Corrientes 1234, CABA"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.FullName.ShouldBe("Ana Gómez");
        employee.PhoneNumber.ShouldBe("+54 11 9999-0000");
        employee.NationalId.ShouldBe("30123456");
        employee.BirthDate.ShouldBe(new DateOnly(1990, 5, 20));
        employee.Address.ShouldBe("Av. Corrientes 1234, CABA");
        employee.UpdatedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Keeps_the_email_and_the_preferred_currency()
    {
        var employee = AddLoggedInEmployee();

        await CreateHandler().Handle(Command(), CancellationToken.None);

        employee.Email.Value.ShouldBe("ana.gomez@acme.com");
        employee.PreferredCurrency.ShouldBe(Currency.USD);
    }

    [Fact]
    public async Task Empty_optional_fields_are_removed()
    {
        var employee = AddLoggedInEmployee();
        await CreateHandler().Handle(
            Command(nationalId: "30123456", birthDate: new DateOnly(1990, 5, 20), address: "Av. Corrientes 1234"),
            CancellationToken.None);

        await CreateHandler().Handle(Command(phoneNumber: "  ", nationalId: "", address: " "), CancellationToken.None);

        employee.PhoneNumber.ShouldBeNull();
        employee.NationalId.ShouldBeNull();
        employee.BirthDate.ShouldBeNull();
        employee.Address.ShouldBeNull();
    }

    [Fact]
    public async Task Requires_an_authenticated_user()
    {
        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Account.NoAutenticado");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public void The_employee_rejects_an_invalid_national_id_even_without_the_validator()
    {
        var employee = AddLoggedInEmployee();

        Should.Throw<DomainException>(() =>
            employee.UpdateProfile("Ana Gómez", null, "12.345.678", null, null, Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Validator_rejects_a_name_that_is_empty_or_too_short(string fullName) =>
        IsValid(Command(fullName: fullName)).ShouldBeFalse();

    [Theory]
    [InlineData("+54 11 1234-5678")]
    [InlineData("(011) 4000-0000")]
    [InlineData("1150000004")]
    [InlineData("+54 9 11 1234 5678")]
    public void Validator_accepts_usual_phone_formats(string phoneNumber) =>
        IsValid(Command(phoneNumber: phoneNumber)).ShouldBeTrue();

    [Theory]
    [InlineData("hola")]
    [InlineData("1234")]
    [InlineData("+54 11 1234-5678 int. 22")]
    [InlineData("1234567890123456")]
    public void Validator_rejects_invalid_phones(string phoneNumber) =>
        IsValid(Command(phoneNumber: phoneNumber)).ShouldBeFalse();

    [Theory]
    [InlineData("1234567")]
    [InlineData("30123456")]
    public void Validator_accepts_a_national_id_of_7_or_8_digits(string nationalId) =>
        IsValid(Command(nationalId: nationalId)).ShouldBeTrue();

    [Theory]
    [InlineData("123456")]
    [InlineData("123456789")]
    [InlineData("30.123.456")]
    [InlineData("3012345A")]
    public void Validator_rejects_an_invalid_national_id(string nationalId) =>
        IsValid(Command(nationalId: nationalId)).ShouldBeFalse();

    [Fact]
    public void Validator_accepts_a_birth_date_between_16_and_100_years_ago()
    {
        IsValid(Command(birthDate: Today.AddYears(-Employee.MinAge))).ShouldBeTrue();
        IsValid(Command(birthDate: Today.AddYears(-(Employee.MaxAge + 1)).AddDays(1))).ShouldBeTrue();
    }

    [Fact]
    public void Validator_rejects_a_birth_date_in_the_future_or_out_of_the_age_range()
    {
        IsValid(Command(birthDate: Today.AddDays(1))).ShouldBeFalse();
        IsValid(Command(birthDate: Today.AddYears(-Employee.MinAge).AddDays(1))).ShouldBeFalse();
        IsValid(Command(birthDate: Today.AddYears(-(Employee.MaxAge + 1)))).ShouldBeFalse();
    }

    [Fact]
    public void Validator_rejects_an_address_longer_than_the_column() =>
        IsValid(Command(address: new string('a', Employee.MaxAddressLength + 1))).ShouldBeFalse();
}
