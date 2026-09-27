namespace FinGrow.Application.UnitTests.Validations.Investments;

using Common;
using FinGrow.Application.Features.Investments.ListInvestments;
using FinGrow.Application.Validations.Investments;
using Domain.Entities;
using Domain.Enums;

public class ListInvestmentsValidatorTests
{
    private readonly ListInvestmentsValidator _validator = new();

    [Fact]
    public void The_default_filters_pass()
    {
        var result = Validate(new InvestmentFilters());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Every_filter_combined_passes_when_it_makes_sense()
    {
        var result = Validate(new InvestmentFilters(
            PageNumber: 3,
            PageSize: 50,
            Search: "AL30",
            Types: new[] { InvestmentType.Bond, InvestmentType.Etf },
            Currencies: new[] { Currency.ARS, Currency.USD },
            PurchasedFrom: new DateOnly(2026, 1, 1),
            PurchasedTo: new DateOnly(2026, 9, 30),
            MinInvested: 0m,
            MaxInvested: 1000m,
            Quoted: false,
            Performance: InvestmentPerformance.Gain,
            SortBy: InvestmentSortField.CurrentValue,
            SortDirection: SortDirection.Ascending));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_page_before_the_first_is_rejected(int pageNumber)
    {
        var result = Validate(new InvestmentFilters(PageNumber: pageNumber));

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void A_page_size_out_of_range_is_rejected(int pageSize)
    {
        var result = Validate(new InvestmentFilters(PageSize: pageSize));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void A_search_longer_than_an_asset_name_is_rejected()
    {
        var result = Validate(new InvestmentFilters(Search: new string('a', Investment.MaxAssetNameLength + 1)));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_asset_type_outside_the_supported_ones_is_rejected()
    {
        var result = Validate(new InvestmentFilters(Types: new[] { InvestmentType.Bond, (InvestmentType)99 }));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_unknown_currency_is_rejected()
    {
        var result = Validate(new InvestmentFilters(Currencies: new[] { (Currency)999 }));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void A_purchase_range_that_ends_before_it_starts_is_rejected()
    {
        var result = Validate(new InvestmentFilters(
            PurchasedFrom: new DateOnly(2026, 9, 30),
            PurchasedTo: new DateOnly(2026, 1, 1)));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void A_single_day_purchase_range_passes()
    {
        var result = Validate(new InvestmentFilters(
            PurchasedFrom: new DateOnly(2026, 9, 1),
            PurchasedTo: new DateOnly(2026, 9, 1)));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_minimum_capital_above_the_maximum_is_rejected()
    {
        var result = Validate(new InvestmentFilters(MinInvested: 5000m, MaxInvested: 100m));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void A_negative_capital_bound_is_rejected()
    {
        var result = Validate(new InvestmentFilters(MinInvested: -1m));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_unknown_performance_sort_or_direction_is_rejected()
    {
        Validate(new InvestmentFilters(Performance: (InvestmentPerformance)9)).IsValid.ShouldBeFalse();
        Validate(new InvestmentFilters(SortBy: (InvestmentSortField)9)).IsValid.ShouldBeFalse();
        Validate(new InvestmentFilters(SortDirection: (SortDirection)9)).IsValid.ShouldBeFalse();
    }

    private FluentValidation.Results.ValidationResult Validate(InvestmentFilters filters) =>
        _validator.Validate(new ListInvestmentsQuery(Guid.CreateVersion7(), filters));
}
