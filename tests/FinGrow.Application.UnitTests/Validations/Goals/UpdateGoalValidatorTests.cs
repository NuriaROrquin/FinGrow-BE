namespace FinGrow.Application.UnitTests.Validations.Goals;

using FinGrow.Application.Features.Goals.UpdateGoal;
using Fakes;
using FinGrow.Application.Validations.Goals;
using Domain.Entities;

public class UpdateGoalValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 22);

    private readonly UpdateGoalValidator _validator = new(new FakeDateTimeProvider(Now));

    private static UpdateGoalCommand ValidCommand() => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Name: "Vacaciones",
        TargetAmount: 500000m,
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
    public void A_deadline_in_the_past_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Deadline = Today.AddDays(-1) });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateGoalCommand.Deadline));
    }

    [Fact]
    public void A_zero_target_amount_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { TargetAmount = 0m });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateGoalCommand.TargetAmount));
    }

    [Fact]
    public void An_empty_name_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Name = "   " });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateGoalCommand.Name));
    }

    [Fact]
    public void A_name_longer_than_the_maximum_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Name = new string('a', Goal.MaxNameLength + 1) });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateGoalCommand.Name));
    }
}
