namespace FinGrow.Application.UnitTests.Features.Integrations.Iol;

using System.Net;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Iol;
using FinGrow.Application.Features.Integrations.Iol.Sync;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class IolSynchronizerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 13);

    private readonly FakeIolAuthClient _auth = new();
    private readonly FakeIolPortfolioClient _iol = new();
    private readonly FakeInvestmentRepository _investments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly EmployeeIntegration _integration;

    public IolSynchronizerTests()
    {
        _integration = EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.Iol, "juan.perez", Now.AddDays(-1));
        // Vence despues de los dias que simulan los tests: la renovacion del bearer la cubre IolAccessTokenProviderTests.
        _integration.Authorize(OAuthGrant.From("iol-bearer", "iol-refresh", Now.AddDays(7)), Now.AddDays(-1));
    }

    [Fact]
    public async Task Each_supported_position_becomes_an_investment_valued_today_by_iol()
    {
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES", quantity: 10m, averagePrice: 5000m, valued: 56_000m));
        Holds(IolCountry.Argentina, Position("AAPL", "CEDEARS", quantity: 2m, averagePrice: 15_000m, valued: 31_000m));
        Holds(IolCountry.UnitedStates, Position("SPY", "ETF", market: "nYSE", currency: "dolar_Estadounidense", quantity: 1m, averagePrice: 560m, valued: 575.5m));

        var summary = (await Sync()).Value;

        summary.ShouldBe(new IolSyncSummary(Imported: 3, Updated: 0, Unchanged: 0, Ignored: 0, NoLongerHeld: 0, Now));
        _investments.Investments.Count.ShouldBe(3);

        var galicia = Imported("bcba:GGAL");
        galicia.EmployeeId.ShouldBe(_integration.EmployeeId);
        galicia.AssetName.ShouldBe("GGAL - Descripcion de GGAL");
        galicia.Type.ShouldBe(InvestmentType.Stock);
        galicia.Symbol.ShouldBe("GGAL");
        galicia.Quantity.ShouldBe(10m);
        galicia.InvestedAmount.ShouldBe(Money.From(50_000m, Currency.ARS));
        var valuation = galicia.Valuations.ShouldHaveSingleItem();
        valuation.Value.ShouldBe(Money.From(56_000m, Currency.ARS));
        valuation.Source.ShouldBe(ValuationSource.Feed);
        valuation.ValuedOn.ShouldBe(Today);

        Imported("bcba:AAPL").Type.ShouldBe(InvestmentType.Cedear);
        var spy = Imported("nyse:SPY");
        spy.Type.ShouldBe(InvestmentType.Etf);
        spy.InvestedAmount.ShouldBe(Money.From(560m, Currency.USD));
        spy.CurrentValue.ShouldBe(Money.From(575.5m, Currency.USD));
    }

    [Fact]
    public async Task Both_portfolios_are_read_with_the_bearer_of_the_integration()
    {
        await Sync();

        _iol.PortfolioRequests.ShouldBe(new[] { ("iol-bearer", IolCountry.Argentina), ("iol-bearer", IolCountry.UnitedStates) });
        _integration.LastSyncedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_purchase_date_is_the_first_completed_buy_of_the_symbol()
    {
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES"));
        _iol.Operations.Add(new IolOperation("GGAL", "Compra", new DateOnly(2026, 9, 2)));
        _iol.Operations.Add(new IolOperation("GGAL", "Compra", new DateOnly(2026, 8, 14)));

        await Sync();

        Imported("bcba:GGAL").PurchasedOn.ShouldBe(new DateOnly(2026, 8, 14));
        _iol.OperationRequests.ShouldHaveSingleItem().ShouldBe(Today.AddYears(-IolSynchronizer.OperationsLookbackYears));
    }

    [Fact]
    public async Task Without_a_buy_on_record_the_purchase_date_is_the_first_sync()
    {
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES"));

        await Sync();

        Imported("bcba:GGAL").PurchasedOn.ShouldBe(Today);
    }

    [Fact]
    public async Task When_the_operations_do_not_answer_the_import_goes_on_with_the_first_sync_as_purchase_date()
    {
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES"));
        _iol.OperationsFailure = new HttpRequestException("bad gateway", null, HttpStatusCode.BadGateway);

        var result = await Sync();

        result.IsSuccess.ShouldBeTrue();
        Imported("bcba:GGAL").PurchasedOn.ShouldBe(Today);
    }

    [Fact]
    public async Task A_position_already_imported_is_not_duplicated_and_does_not_ask_for_operations_again()
    {
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES", quantity: 10m, averagePrice: 5000m, valued: 56_000m));
        await Sync();
        _iol.OperationRequests.Clear();
        _clock.UtcNow = Now.AddDays(1);

        var summary = (await Sync()).Value;

        summary.Imported.ShouldBe(0);
        summary.Unchanged.ShouldBe(1);
        _investments.Investments.ShouldHaveSingleItem();
        Imported("bcba:GGAL").Valuations.ShouldHaveSingleItem();
        _iol.OperationRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_new_quantity_adjusts_the_capital_and_a_new_value_adds_a_valuation()
    {
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES", quantity: 10m, averagePrice: 5000m, valued: 56_000m));
        await Sync();
        _clock.UtcNow = Now.AddDays(1);
        Replace(IolCountry.Argentina, Position("GGAL", "ACCIONES", quantity: 15m, averagePrice: 5100m, valued: 87_000m));

        var summary = (await Sync()).Value;

        summary.Updated.ShouldBe(1);
        var galicia = Imported("bcba:GGAL");
        galicia.Quantity.ShouldBe(15m);
        galicia.InvestedAmount.ShouldBe(Money.From(76_500m, Currency.ARS));
        galicia.Valuations.Count.ShouldBe(2);
        galicia.CurrentValue.ShouldBe(Money.From(87_000m, Currency.ARS));
        galicia.ValuedOn.ShouldBe(Today.AddDays(1));
    }

    [Fact]
    public async Task Only_a_new_price_records_a_valuation_and_keeps_the_capital()
    {
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES", quantity: 10m, averagePrice: 5000m, valued: 56_000m));
        await Sync();
        _clock.UtcNow = Now.AddDays(1);
        Replace(IolCountry.Argentina, Position("GGAL", "ACCIONES", quantity: 10m, averagePrice: 5000m, valued: 54_500m));

        await Sync();

        var galicia = Imported("bcba:GGAL");
        galicia.InvestedAmount.ShouldBe(Money.From(50_000m, Currency.ARS));
        galicia.Valuations.Count.ShouldBe(2);
        galicia.ReturnAmount.ShouldBe(4_500m);
    }

    [Fact]
    public async Task An_unsupported_type_is_skipped_and_reported_as_ignored_without_stopping_the_run()
    {
        Holds(IolCountry.Argentina, Position("GFGC5000OC", "Opciones"));
        Holds(IolCountry.Argentina, Position("CAUCION", "Cauciones"));
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES"));

        var summary = (await Sync()).Value;

        summary.Imported.ShouldBe(1);
        summary.Ignored.ShouldBe(2);
        _investments.Investments.ShouldHaveSingleItem().ExternalReference.ShouldBe("bcba:GGAL");
    }

    [Fact]
    public async Task A_position_sold_in_full_is_kept_with_its_last_valuation()
    {
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES", valued: 56_000m));
        Holds(IolCountry.Argentina, Position("YPFD", "ACCIONES", valued: 40_000m));
        await Sync();
        _clock.UtcNow = Now.AddDays(1);
        Replace(IolCountry.Argentina, Position("YPFD", "ACCIONES", valued: 40_000m));

        var summary = (await Sync()).Value;

        summary.NoLongerHeld.ShouldBe(1);
        var sold = Imported("bcba:GGAL");
        sold.Valuations.ShouldHaveSingleItem();
        sold.CurrentValue.ShouldBe(Money.From(56_000m, Currency.ARS));
        sold.ValuedOn.ShouldBe(Today);
        _investments.Investments.Count.ShouldBe(2);
    }

    [Fact]
    public async Task When_iol_does_not_answer_no_investment_is_touched_and_the_failure_is_unavailable()
    {
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES", valued: 56_000m));
        await Sync();
        var saves = _unitOfWork.SaveCount;
        _clock.UtcNow = Now.AddDays(1);
        Replace(IolCountry.Argentina, Position("GGAL", "ACCIONES", valued: 60_000m));
        Holds(IolCountry.Argentina, Position("YPFD", "ACCIONES"));
        _iol.PortfolioFailure = new HttpRequestException("bad gateway", null, HttpStatusCode.BadGateway);
        _iol.FailingCountry = IolCountry.UnitedStates;

        var result = await Sync();

        result.Error.ShouldBe(IolErrors.Unavailable);
        _investments.Investments.ShouldHaveSingleItem().Valuations.ShouldHaveSingleItem();
        _integration.LastSyncedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(saves);
    }

    [Fact]
    public async Task A_timeout_of_iol_is_also_unavailable()
    {
        _iol.PortfolioFailure = new TaskCanceledException("timeout");

        var result = await Sync();

        result.Error.ShouldBe(IolErrors.Unavailable);
        _integration.LastSyncedAt.ShouldBeNull();
    }

    [Fact]
    public async Task A_manual_investment_with_the_same_symbol_is_not_overwritten()
    {
        var manual = Investment.Create(
            _integration.EmployeeId, "Galicia", InvestmentType.Stock, Money.From(20_000m, Currency.ARS), Today.AddDays(-60), Now.AddDays(-60));
        manual.Track("GGAL", 4m, Now.AddDays(-60));
        _investments.Add(manual);
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES", quantity: 10m, averagePrice: 5000m, valued: 56_000m));

        var summary = (await Sync()).Value;

        summary.Imported.ShouldBe(1);
        _investments.Investments.Count.ShouldBe(2);
        manual.Source.ShouldBe(InvestmentSource.Manual);
        manual.Quantity.ShouldBe(4m);
        manual.InvestedAmount.ShouldBe(Money.From(20_000m, Currency.ARS));
        manual.Valuations.ShouldHaveSingleItem().Source.ShouldBe(ValuationSource.Manual);
        Imported("bcba:GGAL").Source.ShouldBe(InvestmentSource.Iol);
    }

    [Fact]
    public async Task Positions_of_another_employee_are_not_considered_imported()
    {
        _investments.Add(Investment.Import(
            Guid.CreateVersion7(), InvestmentSource.Iol, "bcba:GGAL", "GGAL", InvestmentType.Stock, "GGAL", 1m,
            Money.From(5000m, Currency.ARS), Today, Money.From(5000m, Currency.ARS), Today, Now));
        Holds(IolCountry.Argentina, Position("GGAL", "ACCIONES"));

        var summary = (await Sync()).Value;

        summary.Imported.ShouldBe(1);
        summary.NoLongerHeld.ShouldBe(0);
    }

    [Fact]
    public async Task An_integration_in_error_does_not_reach_the_portfolio()
    {
        _integration.MarkFailed(IolErrors.GrantExpired.Description, Now);

        var result = await Sync();

        result.Error.ShouldBe(IolErrors.GrantExpired);
        _iol.PortfolioRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Without_an_iol_link_the_handler_answers_not_linked()
    {
        var handler = new SyncIolPortfolioHandler(new FakeEmployeeIntegrationRepository(), Synchronizer());

        var result = await handler.Handle(new SyncIolPortfolioCommand(Guid.CreateVersion7()), CancellationToken.None);

        result.Error.ShouldBe(IolErrors.NotLinked);
    }

    private void Holds(IolCountry country, IolPosition position) => _iol.Portfolios[country].Add(position);

    private void Replace(IolCountry country, params IolPosition[] positions)
    {
        _iol.Portfolios[country].Clear();
        _iol.Portfolios[country].AddRange(positions);
    }

    private Investment Imported(string reference) =>
        _investments.Investments.Single(investment => investment.ExternalReference == reference);

    private static IolPosition Position(
        string symbol,
        string type,
        string market = "bCBA",
        string currency = "peso_Argentino",
        decimal quantity = 10m,
        decimal averagePrice = 5000m,
        decimal valued = 56_000m) =>
        new(symbol, $"Descripcion de {symbol}", type, market, currency, quantity, averagePrice, valued);

    private IolSynchronizer Synchronizer() =>
        new(
            new IolAccessTokenProvider(_auth, _unitOfWork, _clock, NullLogger<IolAccessTokenProvider>.Instance),
            _iol,
            _investments,
            _unitOfWork,
            _clock,
            NullLogger<IolSynchronizer>.Instance);

    private Task<Result<IolSyncSummary>> Sync() => Synchronizer().SyncAsync(_integration, CancellationToken.None);
}
