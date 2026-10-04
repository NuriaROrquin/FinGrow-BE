namespace FinGrow.Application.UnitTests.Validations.Reports;

using FinGrow.Application.Features.Reports.SavingsVsGoals;
using FinGrow.Application.Validations.Reports;
using Domain.Enums;

public class GetSavingsVsGoalsValidatorTests
{
    private readonly GetSavingsVsGoalsValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(GetSavingsVsGoalsQuery.MaxMonths)]
    [InlineData(null)]
    public void A_period_within_the_limit_or_no_period_passes(int? months)
    {
        _validator.Validate(new GetSavingsVsGoalsQuery(Guid.CreateVersion7(), Currency.ARS, months)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetSavingsVsGoalsQuery.MaxMonths + 1)]
    public void A_period_out_of_range_is_rejected(int months)
    {
        var result = _validator.Validate(new GetSavingsVsGoalsQuery(Guid.CreateVersion7(), Currency.ARS, months));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(GetSavingsVsGoalsQuery.Months));
    }
}
