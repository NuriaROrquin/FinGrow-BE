namespace FinGrow.Application.UnitTests.Validations.Budgets;

using FinGrow.Application.Features.Budgets.CreateBudget;
using FinGrow.Application.Validations.Budgets;
using Domain.Enums;

public class CreateBudgetValidatorTests
{
    private readonly CreateBudgetValidator _validator = new();

    private static CreateBudgetCommand ValidCommand() => new(
        Guid.CreateVersion7(),
        Year: 2026,
        Month: 9,
        Currency.ARS,
        new[] { new CategoryLimitInput(ExpenseCategory.Alimentos, 150000m) });

    [Fact]
    public void A_valid_command_passes()
    {
        _validator.Validate(ValidCommand()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void A_month_outside_one_to_twelve_is_rejected(int month)
    {
        var result = _validator.Validate(ValidCommand() with { Month = month });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateBudgetCommand.Month));
    }

    [Fact]
    public void An_absurd_year_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Year = 1 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateBudgetCommand.Year));
    }

    [Fact]
    public void A_budget_without_limits_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Limits = Array.Empty<CategoryLimitInput>() });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateBudgetCommand.Limits));
    }

    [Fact]
    public void A_repeated_category_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with
        {
            Limits = new[]
            {
                new CategoryLimitInput(ExpenseCategory.Alimentos, 100m),
                new CategoryLimitInput(ExpenseCategory.Alimentos, 200m),
            },
        });

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void A_limit_that_is_not_positive_is_rejected(decimal amount)
    {
        var result = _validator.Validate(ValidCommand() with
        {
            Limits = new[] { new CategoryLimitInput(ExpenseCategory.Alimentos, amount) },
        });

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_unknown_category_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with
        {
            Limits = new[] { new CategoryLimitInput((ExpenseCategory)999, 100m) },
        });

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_unknown_currency_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Currency = (Currency)999 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateBudgetCommand.Currency));
    }
}
