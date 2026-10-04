namespace FinGrow.Application.Features.Investments.QuoteInvestments;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class InvestmentQuotesJob : IScheduledJob
{
    public const string JobName = "investment-quotes";

    private readonly IInvestmentRepository _investments;
    private readonly IMarketPriceProvider _marketPrices;
    private readonly IServiceScopeFactory _scopes;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<InvestmentQuotesJob> _logger;

    public InvestmentQuotesJob(
        IInvestmentRepository investments,
        IMarketPriceProvider marketPrices,
        IServiceScopeFactory scopes,
        IDateTimeProvider clock,
        ILogger<InvestmentQuotesJob> logger)
    {
        _investments = investments;
        _marketPrices = marketPrices;
        _scopes = scopes;
        _clock = clock;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Cotiza las inversiones cargadas con simbolo y cantidad usando los precios de cierre de BYMA "
        + "y les registra una valuacion de mercado del dia.";

    public string Schedule => "30 21 * * 1-5";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var employees = await _investments.ListEmployeesWithTrackedInvestmentsAsync(cancellationToken);

        if (employees.Count == 0)
        {
            return JobResult.Success("No hay inversiones con simbolo para cotizar.");
        }

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
            $"{quoted} inversion(es) cotizada(s) con {_marketPrices.Source}, {withoutPrice} sin precio, {failures.Count} empleado(s) con error.");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la cotizacion de las inversiones del empleado {EmployeeId}.")]
    private static partial void LogEmployeeFailed(ILogger logger, Exception exception, Guid employeeId);
}
