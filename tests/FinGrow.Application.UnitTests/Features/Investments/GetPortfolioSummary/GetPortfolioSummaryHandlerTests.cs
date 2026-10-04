namespace FinGrow.Application.UnitTests.Features.Investments.GetPortfolioSummary;

using Common;
using DTOs;
using FinGrow.Application.Features.Investments.GetPortfolioSummary;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class GetPortfolioSummaryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 15, 0, 0, TimeSpan.Zero);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeInvestmentRepository _investments = new();

    [Fact]
    public async Task An_empty_portfolio_has_no_totals_no_quotes_and_no_last_purchase()
    {
        var summary = (await SummarizeAsync()).Value;

        summary.InvestmentCount.ShouldBe(0);
        summary.UnquotedCount.ShouldBe(0);
        summary.OldestQuotedOn.ShouldBeNull();
        summary.LastPurchase.ShouldBeNull();
        summary.Currencies.ShouldBeEmpty();
        summary.Allocation.ShouldBeEmpty();
    }

    [Fact]
    public async Task Investments_that_were_never_quoted_count_at_cost_and_are_left_out_of_the_return()
    {
        var quoted = Store("AL30", InvestmentType.Bond, 1000m, Currency.ARS, new DateOnly(2026, 8, 1));
        quoted.RecordValuation(Money.From(1200m, Currency.ARS), new DateOnly(2026, 9, 20), ValuationSource.Feed, Now);
        Store("FCI Balanz", InvestmentType.MutualFund, 3000m, Currency.ARS, new DateOnly(2026, 9, 1));

        var summary = (await SummarizeAsync()).Value;

        summary.InvestmentCount.ShouldBe(2);
        summary.UnquotedCount.ShouldBe(1);
        var pesos = summary.Currencies.ShouldHaveSingleItem();
        pesos.Currency.ShouldBe(Currency.ARS);
        pesos.InvestedAmount.ShouldBe(4000m);
        pesos.CurrentValue.ShouldBe(4200m);
        pesos.QuotedCount.ShouldBe(1);
        pesos.QuotedInvestedAmount.ShouldBe(1000m);
        pesos.QuotedReturnAmount.ShouldBe(200m);
        pesos.QuotedReturnPercentage.ShouldBe(20m);
    }

    [Fact]
    public async Task Each_currency_is_totalled_on_its_own_and_never_mixed()
    {
        Store("AL30", InvestmentType.Bond, 1000m, Currency.ARS, new DateOnly(2026, 8, 1));
        Store("SPY", InvestmentType.Etf, 500m, Currency.USD, new DateOnly(2026, 9, 1));

        var summary = (await SummarizeAsync()).Value;

        summary.Currencies.Count.ShouldBe(2);
        summary.Currencies[0].Currency.ShouldBe(Currency.ARS);
        summary.Currencies[0].InvestedAmount.ShouldBe(1000m);
        summary.Currencies[1].Currency.ShouldBe(Currency.USD);
        summary.Currencies[1].InvestedAmount.ShouldBe(500m);
    }

    [Fact]
    public async Task The_oldest_quote_date_only_looks_at_quoted_investments()
    {
        var older = Store("AL30", InvestmentType.Bond, 1000m, Currency.ARS, new DateOnly(2026, 5, 1));
        older.RecordValuation(Money.From(1100m, Currency.ARS), new DateOnly(2026, 9, 10), ValuationSource.Feed, Now);
        var newer = Store("YPF", InvestmentType.Stock, 1000m, Currency.ARS, new DateOnly(2026, 6, 1));
        newer.RecordValuation(Money.From(900m, Currency.ARS), new DateOnly(2026, 9, 25), ValuationSource.Feed, Now);
        Store("FCI Balanz", InvestmentType.MutualFund, 3000m, Currency.ARS, new DateOnly(2026, 1, 1));

        var summary = (await SummarizeAsync()).Value;

        summary.OldestQuotedOn.ShouldBe(new DateOnly(2026, 9, 10));
    }

    [Fact]
    public async Task The_allocation_groups_current_value_by_type_and_currency()
    {
        Store("AL30", InvestmentType.Bond, 1000m, Currency.ARS, new DateOnly(2026, 8, 1));
        Store("GD30", InvestmentType.Bond, 2000m, Currency.ARS, new DateOnly(2026, 8, 2));
        Store("GD35", InvestmentType.Bond, 300m, Currency.USD, new DateOnly(2026, 8, 3));
        Store("SPY", InvestmentType.Etf, 500m, Currency.USD, new DateOnly(2026, 9, 1));

        var summary = (await SummarizeAsync()).Value;

        summary.Allocation.Count.ShouldBe(3);
        summary.Allocation.ShouldContain(group => group.Type == InvestmentType.Bond && group.Currency == Currency.ARS && group.CurrentValue == 3000m);
        summary.Allocation.ShouldContain(group => group.Type == InvestmentType.Bond && group.Currency == Currency.USD && group.CurrentValue == 300m);
        summary.Allocation.ShouldContain(group => group.Type == InvestmentType.Etf && group.Currency == Currency.USD && group.CurrentValue == 500m);
    }

    [Fact]
    public async Task The_last_purchase_is_the_most_recent_one_of_the_employee_only()
    {
        Store("AL30", InvestmentType.Bond, 1000m, Currency.ARS, new DateOnly(2026, 8, 1));
        Store("Bitcoin", InvestmentType.Crypto, 200m, Currency.USD, new DateOnly(2026, 9, 20));
        _investments.Add(Investment.Create(Guid.CreateVersion7(), "Ajena", InvestmentType.Stock, Money.From(10m, Currency.ARS), new DateOnly(2026, 9, 26), Now));

        var summary = (await SummarizeAsync()).Value;

        summary.InvestmentCount.ShouldBe(2);
        var last = summary.LastPurchase.ShouldNotBeNull();
        last.AssetName.ShouldBe("Bitcoin");
        last.Type.ShouldBe(InvestmentType.Crypto);
        last.PurchasedOn.ShouldBe(new DateOnly(2026, 9, 20));
    }

    private Task<Result<PortfolioSummaryResponse>> SummarizeAsync() =>
        new GetPortfolioSummaryHandler(_investments).Handle(new GetPortfolioSummaryQuery(_employeeId), CancellationToken.None);

    private Investment Store(string assetName, InvestmentType type, decimal invested, Currency currency, DateOnly purchasedOn)
    {
        var investment = Investment.Create(_employeeId, assetName, type, Money.From(invested, currency), purchasedOn, Now);
        _investments.Add(investment);
        return investment;
    }
}
