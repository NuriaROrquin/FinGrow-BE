namespace FinGrow.Application.UnitTests.Features.CompanyPreferences;

using FinGrow.Application.Features.CompanyPreferences.UpdateCompanyPreferences;
using FinGrow.Domain.Enums;

public sealed class UpdateCompanyPreferencesCommandValidatorTests
{
    private readonly UpdateCompanyPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validator_accepts_a_valid_fiscal_year_start_month()
    {
        var result = _validator.Validate(new UpdateCompanyPreferencesCommand(
            Theme.Dark,
            Language.es,
            Currency.ARS,
            4));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validator_rejects_a_fiscal_year_start_month_outside_the_calendar(int month)
    {
        var result = _validator.Validate(new UpdateCompanyPreferencesCommand(
            Theme.Dark,
            Language.es,
            Currency.ARS,
            month));

        result.IsValid.ShouldBeFalse();
    }
}
