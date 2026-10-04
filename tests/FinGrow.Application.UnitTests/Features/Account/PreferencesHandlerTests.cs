namespace FinGrow.Application.UnitTests.Features.Account;

using FinGrow.Application.Features.Account.Preferences;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class PreferencesHandlerTests
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
            null,
            "hash",
            Currency.ARS,
            DateOnly.FromDateTime(HiredAt.Date),
            HiredAt);

        _employees.Employees.Add(employee);
        _currentUser.UserId = employee.Id;
        return employee;
    }

    private GetPreferencesQueryHandler CreateQueryHandler() => new(_currentUser, _employees);

    private UpdatePreferencesCommandHandler CreateCommandHandler() =>
        new(_currentUser, _employees, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task Returns_the_default_preferences_of_a_new_employee()
    {
        AddLoggedInEmployee();

        var result = await CreateQueryHandler().Handle(new GetPreferencesQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Theme.ShouldBe(Theme.System);
        result.Value.Language.ShouldBe(Language.es);
        result.Value.Currency.ShouldBe(Currency.ARS);
        result.Value.DateFormat.ShouldBe(DateFormat.DayMonthYear);
    }

    [Fact]
    public async Task Saved_preferences_are_returned_by_the_next_query()
    {
        var employee = AddLoggedInEmployee();

        var update = await CreateCommandHandler().Handle(
            new UpdatePreferencesCommand(Theme.Dark, Language.pt, Currency.BRL, DateFormat.MonthDayYear),
            CancellationToken.None);
        var query = await CreateQueryHandler().Handle(new GetPreferencesQuery(), CancellationToken.None);

        update.IsSuccess.ShouldBeTrue();
        _unitOfWork.SaveCount.ShouldBe(1);
        employee.UpdatedAt.ShouldBe(Now);
        query.Value.ShouldBe(new(Theme.Dark, Language.pt, Currency.BRL, DateFormat.MonthDayYear));
    }

    [Fact]
    public async Task Updating_the_preferences_keeps_the_profile_data()
    {
        var employee = AddLoggedInEmployee();

        await CreateCommandHandler().Handle(
            new UpdatePreferencesCommand(Theme.Light, Language.en, Currency.USD, DateFormat.YearMonthDay),
            CancellationToken.None);

        employee.FullName.ShouldBe("Ana Gomez");
        employee.Email.Value.ShouldBe("ana.gomez@acme.com");
    }

    [Fact]
    public async Task Both_handlers_require_an_authenticated_user()
    {
        var query = await CreateQueryHandler().Handle(new GetPreferencesQuery(), CancellationToken.None);
        var update = await CreateCommandHandler().Handle(
            new UpdatePreferencesCommand(Theme.Dark, Language.es, Currency.ARS, DateFormat.DayMonthYear),
            CancellationToken.None);

        query.Error.Code.ShouldBe("Account.NoAutenticado");
        update.Error.Code.ShouldBe("Account.NoAutenticado");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public void Validator_accepts_every_supported_value()
    {
        var validation = new UpdatePreferencesCommandValidator().Validate(
            new UpdatePreferencesCommand(Theme.System, Language.pt, Currency.EUR, DateFormat.YearMonthDay));

        validation.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validator_rejects_values_outside_the_enums()
    {
        var validation = new UpdatePreferencesCommandValidator().Validate(
            new UpdatePreferencesCommand((Theme)99, (Language)0, (Currency)42, (DateFormat)7));

        var invalidProperties = validation.Errors.Select(error => error.PropertyName).ToList();

        invalidProperties.Count.ShouldBe(4);
        invalidProperties.ShouldContain(nameof(UpdatePreferencesCommand.Theme));
        invalidProperties.ShouldContain(nameof(UpdatePreferencesCommand.Language));
        invalidProperties.ShouldContain(nameof(UpdatePreferencesCommand.Currency));
        invalidProperties.ShouldContain(nameof(UpdatePreferencesCommand.DateFormat));
    }
}
