namespace FinGrow.Application.UnitTests.Validations.Investments;

using FinGrow.Application.Features.Investments.GetPortfolioEvolution;
using FinGrow.Application.Validations.Investments;
using Domain.Enums;

public class GetPortfolioEvolutionValidatorTests
{
    private readonly GetPortfolioEvolutionValidator _validator = new();

    [Fact]
    public void A_supported_currency_and_range_pass()
    {
        var result = _validator.Validate(
            new GetPortfolioEvolutionQuery(Guid.CreateVersion7(), Currency.USD, PortfolioEvolutionRange.All));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void An_unknown_range_is_rejected()
    {
        var result = _validator.Validate(
            new GetPortfolioEvolutionQuery(Guid.CreateVersion7(), Currency.ARS, (PortfolioEvolutionRange)99));

        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(GetPortfolioEvolutionQuery.Range));
    }

    [Fact]
    public void An_unknown_currency_is_rejected()
    {
        var result = _validator.Validate(
            new GetPortfolioEvolutionQuery(Guid.CreateVersion7(), (Currency)99, PortfolioEvolutionRange.OneMonth));

        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(GetPortfolioEvolutionQuery.Currency));
    }
}
