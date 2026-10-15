namespace FinGrow.Application.UnitTests.Features.Investments.AccrueInvestments;

using FinGrow.Application.Features.Investments.AccrueInvestments;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class InvestmentAccrualsJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 28);

    private readonly FakeInvestmentRepository _investments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly ServiceProvider _provider;

    public InvestmentAccrualsJobTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IInvestmentRepository>(_investments);
        services.AddSingleton<IUnitOfWork>(_unitOfWork);

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Each_investment_with_a_rate_is_valued_with_the_interest_accrued_up_to_today()
    {
        var deposit = Deposit(InvestmentType.FixedTermDeposit, 1_000_000m, Today.AddDays(-15), 30m, 30);
        var account = Deposit(InvestmentType.RemuneratedAccount, 100_000m, Today.AddDays(-10), 36.5m, null);
        var etf = Investment.Create(
            Guid.CreateVersion7(), "SPY", InvestmentType.Etf, Money.From(1000m, Currency.USD), Today.AddDays(-10), Now);
        _investments.Add(etf);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("2 inversion(es) devengada(s) al 2026-09-28, 0 error(es).");
        deposit.ValuedOn.ShouldBe(Today);
        deposit.CurrentValue.Amount.ShouldBe(1_012_328.77m);
        account.CurrentValue.Amount.ShouldBe(101_004.51m);
        etf.HasMarketValuation.ShouldBeFalse();
        _unitOfWork.SaveCount.ShouldBe(2);
    }

    [Fact]
    public async Task A_deposit_that_already_matured_stays_valued_at_maturity()
    {
        var deposit = Deposit(InvestmentType.FixedTermDeposit, 1_000_000m, Today.AddDays(-45), 30m, 30);

        await Job().ExecuteAsync(CancellationToken.None);

        deposit.ValuedOn.ShouldBe(Today.AddDays(-15));
        deposit.CurrentValue.Amount.ShouldBe(1_024_657.53m);
        deposit.IsValuedAtMaturity.ShouldBeTrue();
    }

    [Fact]
    public async Task Running_twice_the_same_day_does_not_duplicate_the_valuation()
    {
        var deposit = Deposit(InvestmentType.FixedTermDeposit, 1_000_000m, Today.AddDays(-15), 30m, 30);

        await Job().ExecuteAsync(CancellationToken.None);
        await Job().ExecuteAsync(CancellationToken.None);

        deposit.Valuations.Count(valuation => valuation.Source == ValuationSource.Accrued).ShouldBe(2);
        deposit.Valuations.Count(valuation => valuation.ValuedOn == Today).ShouldBe(1);
    }

    [Fact]
    public async Task Without_investments_with_a_rate_the_run_succeeds_and_saves_nothing()
    {
        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("0 inversion(es) devengada(s) al 2026-09-28, 0 error(es).");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    private InvestmentAccrualsJob Job() => new(
        _investments,
        _provider.GetRequiredService<IServiceScopeFactory>(),
        _clock,
        NullLogger<InvestmentAccrualsJob>.Instance);

    private Investment Deposit(InvestmentType type, decimal invested, DateOnly purchasedOn, decimal annualRate, int? termDays)
    {
        var investment = Investment.Create(
            Guid.CreateVersion7(), "Plazo fijo Banco Nacion", type, Money.From(invested, Currency.ARS), purchasedOn, Now.AddDays(-30));
        investment.AgreeRate(annualRate, termDays, purchasedOn, Now.AddDays(-30));
        _investments.Add(investment);

        return investment;
    }
}
