namespace FinGrow.Application.UnitTests.Validations.Budgets;

using FinGrow.Application.Features.Budgets.DuplicatePreviousBudget;
using FinGrow.Application.Validations.Budgets;

public class DuplicatePreviousBudgetValidatorTests
{
    private readonly DuplicatePreviousBudgetValidator _validator = new();

    [Fact]
    public void A_valid_month_passes()
    {
        _validator.Validate(new DuplicatePreviousBudgetCommand(Guid.CreateVersion7(), 2026, 10)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(1, 5)]
    public void An_invalid_period_is_rejected(int year, int month)
    {
        _validator.Validate(new DuplicatePreviousBudgetCommand(Guid.CreateVersion7(), year, month)).IsValid.ShouldBeFalse();
    }
}
