namespace FinGrow.Application.UnitTests.Features.Investments.GetPortfolioEvolution;

using Common;
using FinGrow.Application.Features.Investments.GetPortfolioEvolution;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class GetPortfolioEvolutionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 14);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeInvestmentRepository _investments = new();

    [Fact]
    public async Task Without_investments_in_the_currency_there_is_no_history()
    {
        Store("SPY", InvestmentType.Etf, 500m, Currency.USD, new DateOnly(2026, 9, 1));

        var evolution = (await EvolutionAsync(Currency.ARS, PortfolioEvolutionRange.All)).Value;

        evolution.Currency.ShouldBe(Currency.ARS);
        evolution.HistoryStartsOn.ShouldBeNull();
        evolution.Points.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_range_ends_today_and_goes_back_that_many_months_one_day_at_a_time()
    {
        Store("AL30", InvestmentType.Bond, 1000m, Currency.ARS, new DateOnly(2026, 1, 1));

        var evolution = (await EvolutionAsync(Currency.ARS, PortfolioEvolutionRange.ThreeMonths)).Value;

        evolution.Interval.ShouldBe(PortfolioEvolutionInterval.Day);
        evolution.HistoryStartsOn.ShouldBe(new DateOnly(2026, 1, 1));
        evolution.Points[0].Date.ShouldBe(new DateOnly(2026, 7, 14));
        evolution.Points[^1].Date.ShouldBe(Today);
        evolution.Points.Count.ShouldBe(93);
    }

    [Fact]
    public async Task A_range_longer_than_the_history_starts_at_the_first_purchase()
    {
        Store("AL30", InvestmentType.Bond, 1000m, Currency.ARS, new DateOnly(2026, 9, 1));

        var evolution = (await EvolutionAsync(Currency.ARS, PortfolioEvolutionRange.OneYear)).Value;

        evolution.HistoryStartsOn.ShouldBe(new DateOnly(2026, 9, 1));
        evolution.Points[0].Date.ShouldBe(new DateOnly(2026, 9, 1));
        evolution.Points[^1].Date.ShouldBe(Today);
    }

    [Fact]
    public async Task A_whole_history_longer_than_a_year_goes_week_by_week_back_from_today()
    {
        Store("AL30", InvestmentType.Bond, 1000m, Currency.ARS, new DateOnly(2024, 10, 1));

        var evolution = (await EvolutionAsync(Currency.ARS, PortfolioEvolutionRange.All)).Value;

        evolution.Interval.ShouldBe(PortfolioEvolutionInterval.Week);
        evolution.Points[0].Date.ShouldBe(new DateOnly(2024, 10, 1));
        evolution.Points[^1].Date.ShouldBe(Today);
        evolution.Points
            .Skip(1)
            .Zip(evolution.Points.Skip(2), (previous, next) => next.Date.DayNumber - previous.Date.DayNumber)
            .ShouldAllBe(days => days == 7);
        (evolution.Points[1].Date.DayNumber - evolution.Points[0].Date.DayNumber).ShouldBeInRange(1, 7);
    }

    [Fact]
    public async Task Each_day_adds_up_the_last_known_value_and_the_capital_of_what_was_already_bought()
    {
        var bond = Store("AL30", InvestmentType.Bond, 1000m, Currency.ARS, new DateOnly(2026, 9, 20));
        bond.RecordValuation(Money.From(1200m, Currency.ARS), new DateOnly(2026, 10, 1), ValuationSource.Feed, Now);
        var fund = Store("FCI Balanz", InvestmentType.MutualFund, 500m, Currency.ARS, new DateOnly(2026, 10, 5));

        var evolution = (await EvolutionAsync(Currency.ARS, PortfolioEvolutionRange.OneMonth)).Value;

        evolution.Points[0].Date.ShouldBe(new DateOnly(2026, 9, 20));
        PointOn(evolution, new DateOnly(2026, 9, 20)).ShouldBe(Point(2026, 9, 20, value: 1000m, invested: 1000m));
        PointOn(evolution, new DateOnly(2026, 9, 30)).ShouldBe(Point(2026, 9, 30, value: 1000m, invested: 1000m));
        PointOn(evolution, new DateOnly(2026, 10, 1)).ShouldBe(Point(2026, 10, 1, value: 1200m, invested: 1000m));
        PointOn(evolution, new DateOnly(2026, 10, 5)).ShouldBe(Point(2026, 10, 5, value: 1700m, invested: 1500m));
        evolution.Points[^1].Value.ShouldBe(bond.CurrentValue.Amount + fund.CurrentValue.Amount);
    }

    [Fact]
    public async Task Each_currency_has_its_own_history_and_is_never_mixed()
    {
        Store("AL30", InvestmentType.Bond, 1000m, Currency.ARS, new DateOnly(2026, 8, 1));
        Store("SPY", InvestmentType.Etf, 500m, Currency.USD, new DateOnly(2026, 10, 1));

        var evolution = (await EvolutionAsync(Currency.USD, PortfolioEvolutionRange.ThreeMonths)).Value;

        evolution.HistoryStartsOn.ShouldBe(new DateOnly(2026, 10, 1));
        evolution.Points.ShouldAllBe(point => point.Value == 500m && point.InvestedAmount == 500m);
    }

    [Fact]
    public async Task A_fixed_term_deposit_grows_with_its_rate_day_by_day()
    {
        var deposit = Store("Plazo fijo", InvestmentType.FixedTermDeposit, 1_000_000m, Currency.ARS, new DateOnly(2026, 10, 4));
        deposit.AgreeRate(30m, 30, Today, Now);

        var evolution = (await EvolutionAsync(Currency.ARS, PortfolioEvolutionRange.OneMonth)).Value;

        PointOn(evolution, new DateOnly(2026, 10, 4)).Value.ShouldBe(1_000_000m);
        PointOn(evolution, new DateOnly(2026, 10, 9)).Value.ShouldBe(1_004_109.59m);
        evolution.Points[^1].Value.ShouldBe(deposit.CurrentValue.Amount);
    }

    private static PortfolioEvolutionPoint Point(int year, int month, int day, decimal value, decimal invested) =>
        new(new DateOnly(year, month, day), value, invested);

    private static PortfolioEvolutionPoint PointOn(PortfolioEvolutionResponse evolution, DateOnly date) =>
        evolution.Points.Single(point => point.Date == date);

    private Task<Result<PortfolioEvolutionResponse>> EvolutionAsync(Currency currency, PortfolioEvolutionRange range) =>
        new GetPortfolioEvolutionHandler(_investments, new FakeDateTimeProvider(Now))
            .Handle(new GetPortfolioEvolutionQuery(_employeeId, currency, range), CancellationToken.None);

    private Investment Store(string assetName, InvestmentType type, decimal invested, Currency currency, DateOnly purchasedOn)
    {
        var investment = Investment.Create(_employeeId, assetName, type, Money.From(invested, currency), purchasedOn, Now);
        _investments.Add(investment);
        return investment;
    }
}
