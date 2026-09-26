namespace FinGrow.Application.UnitTests.Validations.Goals;

using FinGrow.Application.Features.Goals.CreateGoal;
using Fakes;
using FinGrow.Application.Validations.Goals;
using Domain.Entities;
using Domain.Enums;

public class CreateGoalValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 22);

    private readonly CreateGoalValidator _validator = new(new FakeDateTimeProvider(Now));

    private static CreateGoalCommand ValidCommand() => new(
        Guid.CreateVersion7(),
        Name: "Vacaciones",
        TargetAmount: 500000m,
        Currency.ARS,
        Deadline: Today.AddMonths(4));

    [Fact]
    public void A_valid_command_passes()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_deadline_of_today_passes()
    {
        var result = _validator.Validate(ValidCommand() with { Deadline = Today });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Late_at_night_today_is_the_argentinian_date_not_the_utc_one()
    {
        var lateNightInBuenosAires = new DateTimeOffset(2026, 9, 23, 2, 30, 0, TimeSpan.Zero);
        var validator = new CreateGoalValidator(new FakeDateTimeProvider(lateNightInBuenosAires));

        var result = validator.Validate(ValidCommand() with { Deadline = new DateOnly(2026, 9, 22) });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_deadline_in_the_past_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Deadline = Today.AddDays(-1) });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateGoalCommand.Deadline));
    }

    [Fact]
    public void A_zero_target_amount_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { TargetAmount = 0m });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateGoalCommand.TargetAmount));
    }

    [Fact]
    public void A_negative_target_amount_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { TargetAmount = -100m });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateGoalCommand.TargetAmount));
    }

    [Fact]
    public void An_empty_name_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Name = "   " });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateGoalCommand.Name));
    }

    [Fact]
    public void A_name_longer_than_the_maximum_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Name = new string('a', Goal.MaxNameLength + 1) });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateGoalCommand.Name));
    }

    [Fact]
    public void An_unknown_currency_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Currency = (Currency)999 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateGoalCommand.Currency));
    }
}
