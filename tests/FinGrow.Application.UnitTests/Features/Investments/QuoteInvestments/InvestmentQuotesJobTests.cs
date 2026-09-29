namespace FinGrow.Application.UnitTests.Features.Investments.QuoteInvestments;

using FinGrow.Application.Features.Investments.QuoteInvestments;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class InvestmentQuotesJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 21, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 28);

    private readonly FakeInvestmentRepository _investments = new();
    private readonly FakeMarketPriceProvider _prices = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly ServiceProvider _provider;

    public InvestmentQuotesJobTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IInvestmentRepository>(_investments);
        services.AddSingleton<IUnitOfWork>(_unitOfWork);

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Each_tracked_investment_is_valued_at_the_closing_price_times_its_quantity()
    {
        var bond = Tracked("AL30", InvestmentType.Bond, 800000m, Currency.ARS, "AL30", 1000m);
        var stock = Tracked("YPF", InvestmentType.Stock, 90000m, Currency.ARS, "YPFD", 10m);
        _prices.Prices.Add(new MarketPrice("AL30", Currency.ARS, 839.40m));
        _prices.Prices.Add(new MarketPrice("YPFD", Currency.ARS, 8305m));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("2 inversion(es) cotizada(s) con BYMA, 0 sin precio, 0 empleado(s) con error.");
        bond.CurrentValue.ShouldBe(Money.From(839400m, Currency.ARS));
        bond.ValuedOn.ShouldBe(Today);
        bond.HasMarketValuation.ShouldBeTrue();
        stock.CurrentValue.ShouldBe(Money.From(83050m, Currency.ARS));
        stock.ReturnAmount.ShouldBe(-6950m);
    }

    [Fact]
    public async Task The_currency_of_the_investment_picks_the_price_variant_and_a_missing_one_is_counted()
    {
        var dollars = Tracked("AL30 en dolares", InvestmentType.Bond, 500m, Currency.USD, "AL30D", 1000m);
        var wrongVariant = Tracked("AL30 mal cargado", InvestmentType.Bond, 500m, Currency.USD, "AL30", 1000m);
        _prices.Prices.Add(new MarketPrice("AL30", Currency.ARS, 839.40m));
        _prices.Prices.Add(new MarketPrice("AL30D", Currency.USD, 0.5397m));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("1 inversion(es) cotizada(s) con BYMA, 1 sin precio, 0 empleado(s) con error.");
        dollars.CurrentValue.ShouldBe(Money.From(539.70m, Currency.USD));
        wrongVariant.HasMarketValuation.ShouldBeFalse();
    }

    [Fact]
    public async Task Running_twice_the_same_day_updates_the_valuation_instead_of_duplicating_it()
    {
        var stock = Tracked("YPF", InvestmentType.Stock, 90000m, Currency.ARS, "YPFD", 10m);
        _prices.Prices.Add(new MarketPrice("YPFD", Currency.ARS, 8305m));
        await Job().ExecuteAsync(CancellationToken.None);

        _prices.Prices.Clear();
        _prices.Prices.Add(new MarketPrice("YPFD", Currency.ARS, 8400m));
        await Job().ExecuteAsync(CancellationToken.None);

        stock.Valuations.Count.ShouldBe(2);
        stock.CurrentValue.ShouldBe(Money.From(84000m, Currency.ARS));
    }

    [Fact]
    public async Task When_the_source_does_not_answer_the_run_fails_and_nothing_is_touched()
    {
        var stock = Tracked("YPF", InvestmentType.Stock, 90000m, Currency.ARS, "YPFD", 10m);
        _prices.Failure = new HttpRequestException("open.bymadata.com.ar no responde");

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull().ShouldContain("BYMA");
        result.Summary.ShouldBe("No se cotizo ninguna inversion.");
        stock.HasMarketValuation.ShouldBeFalse();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Without_tracked_investments_the_source_is_not_called()
    {
        _investments.Add(Investment.Create(
            Guid.CreateVersion7(), "FCI", InvestmentType.MutualFund, Money.From(1000m, Currency.ARS), Today.AddDays(-5), Now));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("No hay inversiones con simbolo para cotizar.");
        _prices.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Investments_without_symbol_keep_their_value_while_the_tracked_ones_are_quoted()
    {
        var tracked = Tracked("YPF", InvestmentType.Stock, 90000m, Currency.ARS, "YPFD", 10m);
        var manual = Investment.Create(
            tracked.EmployeeId, "Acciones varias", InvestmentType.Stock, Money.From(5000m, Currency.ARS), Today.AddDays(-5), Now);
        _investments.Add(manual);
        _prices.Prices.Add(new MarketPrice("YPFD", Currency.ARS, 8305m));

        await Job().ExecuteAsync(CancellationToken.None);

        tracked.HasMarketValuation.ShouldBeTrue();
        manual.HasMarketValuation.ShouldBeFalse();
        manual.CurrentValue.ShouldBe(Money.From(5000m, Currency.ARS));
    }

    [Fact]
    public void The_job_runs_after_the_close_on_business_days()
    {
        var job = Job();

        job.Name.ShouldBe("investment-quotes");
        job.Schedule.ShouldBe("30 21 * * 1-5");
        job.Description.ShouldNotBeNullOrWhiteSpace();
    }

    private InvestmentQuotesJob Job() => new(
        _investments,
        _prices,
        _provider.GetRequiredService<IServiceScopeFactory>(),
        _clock,
        NullLogger<InvestmentQuotesJob>.Instance);

    private Investment Tracked(string assetName, InvestmentType type, decimal invested, Currency currency, string symbol, decimal quantity)
    {
        var investment = Investment.Create(
            Guid.CreateVersion7(), assetName, type, Money.From(invested, currency), Today.AddDays(-30), Now.AddDays(-30));
        investment.Track(symbol, quantity, Now.AddDays(-30));
        _investments.Add(investment);

        return investment;
    }
}
