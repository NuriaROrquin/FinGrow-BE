namespace FinGrow.Application.Features.Investments.QuoteInvestments;

using System.Globalization;
using FinGrow.Application.Features.SecurityPrices;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class InvestmentQuotesJob : IScheduledJob
{
    public const string JobName = "investment-quotes";

    private readonly IInvestmentRepository _investments;
    private readonly ISecurityPriceRepository _securityPrices;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReadOnlyList<IMarketPriceProvider> _marketPrices;
    private readonly IServiceScopeFactory _scopes;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<InvestmentQuotesJob> _logger;

    public InvestmentQuotesJob(
        IInvestmentRepository investments,
        ISecurityPriceRepository securityPrices,
        IUnitOfWork unitOfWork,
        IEnumerable<IMarketPriceProvider> marketPrices,
        IServiceScopeFactory scopes,
        IDateTimeProvider clock,
        ILogger<InvestmentQuotesJob> logger)
    {
        _investments = investments;
        _securityPrices = securityPrices;
        _unitOfWork = unitOfWork;
        _marketPrices = marketPrices.ToList();
        _scopes = scopes;
        _clock = clock;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Guarda el ultimo precio de cada simbolo de BYMA, de cada fondo comun y de cada criptomoneda y cotiza con "
        + "ellos las inversiones cargadas con simbolo y cantidad, registrandoles una valuacion de mercado del dia.";

    public string Schedule => "30 21 * * 1-5";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var index = new MarketPriceIndex();
        var answered = new List<string>();
        var failures = new List<string>();

        foreach (var provider in _marketPrices)
        {
            try
            {
                index.Add(provider, await provider.GetClosingPricesAsync(cancellationToken));
                answered.Add(provider.Source);
            }
            catch (HttpRequestException exception)
            {
                failures.Add(SourceUnavailable(provider, exception));
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                failures.Add(SourceUnavailable(provider, exception));
            }
        }

        if (answered.Count == 0)
        {
            return JobResult.Failure(string.Join(Environment.NewLine, failures), "No se cotizo ninguna inversion.");
        }

        var today = _clock.Today;
        var quoted = 0;
        var withoutPrice = 0;
        var stored = 0;

        try
        {
            stored = await StoreClosingPricesAsync(index, today, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogStoringPricesFailed(_logger, exception);
            failures.Add($"Precios de cierre: {exception.GetType().Name}: {exception.Message}");
        }

        var employees = await _investments.ListEmployeesWithTrackedInvestmentsAsync(cancellationToken);

        foreach (var employeeId in employees)
        {
            try
            {
                var (quotedForEmployee, missingForEmployee) = await QuoteEmployeeAsync(employeeId, index, today, cancellationToken);
                quoted += quotedForEmployee;
                withoutPrice += missingForEmployee;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogEmployeeFailed(_logger, exception, employeeId);
                failures.Add($"Empleado {employeeId}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"{stored} precio(s) de cierre guardado(s), {quoted} inversion(es) cotizada(s) con {string.Join(", ", answered)}, "
            + $"{withoutPrice} sin precio, {failures.Count} error(es).");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    private async Task<int> StoreClosingPricesAsync(MarketPriceIndex index, DateOnly today, CancellationToken cancellationToken)
    {
        var storable = index.Prices
            .Where(indexed => indexed.Market.NormalizeSymbol(indexed.Price.Symbol).Length <= indexed.Market.MaxSymbolLength())
            .ToList();

        if (storable.Count == 0)
        {
            return 0;
        }

        var existing = new Dictionary<(PriceMarket Market, string Key, Currency Currency), SecurityPrice>();

        foreach (var price in await _securityPrices.ListAsync(cancellationToken))
        {
            existing.TryAdd((price.Market, MarketPriceIndex.KeyOf(price.Market, price.Symbol), price.Currency), price);
        }

        var now = _clock.UtcNow;
        var added = new List<SecurityPrice>();

        foreach (var (market, source, price) in storable)
        {
            var key = (market, MarketPriceIndex.KeyOf(market, price.Symbol), price.Currency);
            var pricedOn = price.PricedOn ?? today;

            if (existing.TryGetValue(key, out var stored))
            {
                stored.Update(price.UnitPrice, pricedOn, source, now);
            }
            else
            {
                var created = SecurityPrice.Create(market, price.Symbol, price.Currency, price.UnitPrice, pricedOn, source, now);
                existing[key] = created;
                added.Add(created);
            }
        }

        _securityPrices.AddRange(added);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return storable.Count;
    }

    private async Task<(int Quoted, int WithoutPrice)> QuoteEmployeeAsync(
        Guid employeeId,
        MarketPriceIndex index,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInvestmentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var quoted = 0;
        var withoutPrice = 0;

        foreach (var investment in await repository.ListTrackedByEmployeeAsync(employeeId, cancellationToken))
        {
            var currency = investment.InvestedAmount.Currency;

            if (investment.Symbol is null
                || investment.Quantity is not { } quantity
                || investment.PurchasedOn > today
                || investment.Type.QuotedOn() is not { } market
                || index.Find(market, investment.Symbol, currency) is not { } found)
            {
                withoutPrice++;
                continue;
            }

            investment.RecordMarketValuation(Money.From(found.Price.UnitPrice * quantity, currency), today, _clock.UtcNow);
            quoted++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return (quoted, withoutPrice);
    }

    private string SourceUnavailable(IMarketPriceProvider provider, Exception exception)
    {
        LogSourceUnavailable(_logger, exception, provider.Source);

        return $"{provider.Source} no devolvio precios: {exception.Message}";
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudieron obtener los precios de cierre de {Source}.")]
    private static partial void LogSourceUnavailable(ILogger logger, Exception exception, string source);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudieron guardar los precios de cierre.")]
    private static partial void LogStoringPricesFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la cotizacion de las inversiones del empleado {EmployeeId}.")]
    private static partial void LogEmployeeFailed(ILogger logger, Exception exception, Guid employeeId);
}
