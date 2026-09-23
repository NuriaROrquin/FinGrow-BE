namespace FinGrow.Application.UnitTests.Validations.Goals;

using FinGrow.Application.Features.Goals.AddContribution;
using Fakes;
using FinGrow.Application.Validations.Goals;
using Domain.Entities;
using Domain.Enums;

public class AddGoalContributionValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 22);

    private readonly AddGoalContributionValidator _validator = new(new FakeDateTimeProvider(Now));

    private static AddGoalContributionCommand ValidCommand() => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Amount: 5000m,
        Currency.ARS,
        ContributedOn: Today,
        Note: null);

    [Fact]
    public void A_valid_command_passes()
    {
        _validator.Validate(ValidCommand()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_contribution_dated_in_the_past_passes()
    {
        _validator.Validate(ValidCommand() with { ContributedOn = Today.AddMonths(-2) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_contribution_dated_in_the_future_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { ContributedOn = Today.AddDays(1) });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddGoalContributionCommand.ContributedOn));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void A_non_positive_amount_is_rejected(decimal amount)
    {
        var result = _validator.Validate(ValidCommand() with { Amount = amount });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddGoalContributionCommand.Amount));
    }

    [Fact]
    public void A_note_longer_than_the_limit_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Note = new string('x', GoalContribution.MaxNoteLength + 1) });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddGoalContributionCommand.Note));
    }

    [Fact]
    public void An_unknown_currency_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Currency = (Currency)99 });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddGoalContributionCommand.Currency));
    }
}
