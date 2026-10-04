namespace FinGrow.Application.Features.Investments.QuoteInvestments;

using System.Globalization;
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
    private readonly IMarketPriceProvider _marketPrices;
    private readonly IServiceScopeFactory _scopes;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<InvestmentQuotesJob> _logger;

    public InvestmentQuotesJob(
        IInvestmentRepository investments,
        ISecurityPriceRepository securityPrices,
        IUnitOfWork unitOfWork,
        IMarketPriceProvider marketPrices,
        IServiceScopeFactory scopes,
        IDateTimeProvider clock,
        ILogger<InvestmentQuotesJob> logger)
    {
        _investments = investments;
        _securityPrices = securityPrices;
        _unitOfWork = unitOfWork;
        _marketPrices = marketPrices;
        _scopes = scopes;
        _clock = clock;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Guarda los precios de cierre de BYMA como ultimo cierre de cada simbolo y cotiza con ellos "
        + "las inversiones cargadas con simbolo y cantidad, registrandoles una valuacion de mercado del dia.";

    public string Schedule => "30 21 * * 1-5";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<MarketPrice> prices;

        try
        {
            prices = await _marketPrices.GetClosingPricesAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return SourceUnavailable(exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SourceUnavailable(exception);
        }

        var unitPrices = prices
            .GroupBy(price => (price.Symbol, price.Currency))
            .ToDictionary(group => group.Key, group => group.First().UnitPrice);

        var today = _clock.Today;
        var quoted = 0;
        var withoutPrice = 0;
        var failures = new List<string>();
        var stored = 0;

        try
        {
            stored = await StoreClosingPricesAsync(unitPrices, today, cancellationToken);
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
                var (quotedForEmployee, missingForEmployee) = await QuoteEmployeeAsync(employeeId, unitPrices, today, cancellationToken);
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
            $"{stored} precio(s) de cierre guardado(s), {quoted} inversion(es) cotizada(s) con {_marketPrices.Source}, "
            + $"{withoutPrice} sin precio, {failures.Count} error(es).");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    private async Task<int> StoreClosingPricesAsync(
        Dictionary<(string Symbol, Currency Currency), decimal> unitPrices,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var storable = unitPrices
            .Where(price => price.Key.Symbol.Length <= Investment.MaxSymbolLength)
            .ToList();

        if (storable.Count == 0)
        {
            return 0;
        }

        var existing = (await _securityPrices.ListAsync(cancellationToken))
            .ToDictionary(price => (price.Symbol, price.Currency));
        var now = _clock.UtcNow;
        var added = new List<SecurityPrice>();

        foreach (var ((symbol, currency), unitPrice) in storable)
        {
            if (existing.TryGetValue((symbol, currency), out var price))
            {
                price.Update(unitPrice, today, _marketPrices.Source, now);
            }
            else
            {
                added.Add(SecurityPrice.Create(symbol, currency, unitPrice, today, _marketPrices.Source, now));
            }
        }

        _securityPrices.AddRange(added);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return storable.Count;
    }

    private async Task<(int Quoted, int WithoutPrice)> QuoteEmployeeAsync(
        Guid employeeId,
        Dictionary<(string Symbol, Currency Currency), decimal> unitPrices,
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
                || !unitPrices.TryGetValue((investment.Symbol, currency), out var unitPrice))
            {
                withoutPrice++;
                continue;
            }

            investment.RecordMarketValuation(Money.From(unitPrice * quantity, currency), today, _clock.UtcNow);
            quoted++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return (quoted, withoutPrice);
    }

    private JobResult SourceUnavailable(Exception exception)
    {
        LogSourceUnavailable(_logger, exception, _marketPrices.Source);

        return JobResult.Failure(
            $"{_marketPrices.Source} no devolvio precios: {exception.Message}",
            "No se cotizo ninguna inversion.");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudieron obtener los precios de cierre de {Source}.")]
    private static partial void LogSourceUnavailable(ILogger logger, Exception exception, string source);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudieron guardar los precios de cierre.")]
    private static partial void LogStoringPricesFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la cotizacion de las inversiones del empleado {EmployeeId}.")]
    private static partial void LogEmployeeFailed(ILogger logger, Exception exception, Guid employeeId);
}
