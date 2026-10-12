namespace FinGrow.Application.Features.Metrics.FinancialHealth;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal sealed partial class FinancialHealthScorer
{
    private readonly IFinancialHealthReadRepository _finances;
    private readonly IGoalRepository _goals;
    private readonly IFinancialHealthScoreRepository _scores;
    private readonly IExchangeRateProvider _exchangeRates;
    private readonly ILogger<FinancialHealthScorer> _logger;
    private decimal? _usdToArs;
    private bool _usdRateUnavailable;

    public FinancialHealthScorer(
        IFinancialHealthReadRepository finances,
        IGoalRepository goals,
        IFinancialHealthScoreRepository scores,
        IExchangeRateProvider exchangeRates,
        ILogger<FinancialHealthScorer> logger)
    {
        _finances = finances;
        _goals = goals;
        _scores = scores;
        _exchangeRates = exchangeRates;
        _logger = logger;
    }

    public async Task<FinancialHealthScoring> ScoreAsync(
        Guid companyId,
        MetricsPeriod period,
        IReadOnlyCollection<EmployeePeriodActivity> activity,
        DateTimeOffset computedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(activity);

        var measured = await MeasureAsync(period, activity, cancellationToken);

        if (measured is null)
        {
            return FinancialHealthScoring.WithoutExchangeRate;
        }

        await SaveAsync(companyId, period, measured, computedAt, cancellationToken);

        return FinancialHealthScoring.Completed(measured.ToDictionary(pair => pair.Key, pair => pair.Value.Score));
    }

    private async Task<Dictionary<Guid, FinancialHealthFactors>?> MeasureAsync(
        MetricsPeriod period,
        IReadOnlyCollection<EmployeePeriodActivity> activity,
        CancellationToken cancellationToken)
    {
        var measured = new Dictionary<Guid, FinancialHealthFactors>();
        var candidates = activity
            .Where(employee => employee.ConfirmedTransactions >= FinancialHealthFactors.MinimumConfirmedTransactions)
            .ToList();

        if (candidates.Count == 0)
        {
            return measured;
        }

        var employeeIds = candidates.Select(employee => employee.EmployeeId).ToList();
        var totals = await _finances.ListMovementTotalsAsync(employeeIds, period, cancellationToken);
        var limits = await _finances.ListBudgetLimitsAsync(employeeIds, period, cancellationToken);
        var goals = await _goals.ListByEmployeesAsync(employeeIds, cancellationToken);

        var usdToArs = 0m;

        if (totals.Any(total => total.Currency == Currency.USD))
        {
            if (await UsdToArsAsync(cancellationToken) is not { } rate)
            {
                return null;
            }

            usdToArs = rate;
        }

        foreach (var employee in candidates)
        {
            var ownTotals = totals.Where(total => total.EmployeeId == employee.EmployeeId).ToList();
            var income = InArs(ownTotals, total => total.MonthIncome, usdToArs);

            if (!FinancialHealthFactors.HasEnoughActivity(employee.ConfirmedTransactions, income))
            {
                continue;
            }

            var spending = InArs(ownTotals, total => total.MonthSpending, usdToArs);
            var incomeToDate = InArs(ownTotals, total => total.IncomeToDate, usdToArs);
            var spendingToDate = InArs(ownTotals, total => total.SpendingToDate, usdToArs);
            var monthsOfHistory = MonthsOfHistory(ownTotals.Min(total => total.FirstMovementOn), period);
            var averageMonthlySpending = Money.From(
                InArs(ownTotals, total => total.RecentSpending, usdToArs).Amount / monthsOfHistory,
                Currency.ARS);
            var budgetCategories = limits
                .Where(limit => limit.EmployeeId == employee.EmployeeId)
                .Select(limit => Budget.HealthOf(limit.Limit, limit.Spent));
            var ownGoals = goals.Where(goal => goal.EmployeeId == employee.EmployeeId);

            measured[employee.EmployeeId] = FinancialHealthFactors.From(
                FinancialHealthFactors.SavingsOf(income, spending),
                FinancialHealthFactors.BudgetOf(budgetCategories),
                FinancialHealthFactors.GoalsOf(ownGoals, period.Start),
                FinancialHealthFactors.CushionOf(incomeToDate, spendingToDate, averageMonthlySpending));
        }

        return measured;
    }

    private async Task SaveAsync(
        Guid companyId,
        MetricsPeriod period,
        Dictionary<Guid, FinancialHealthFactors> measured,
        DateTimeOffset computedAt,
        CancellationToken cancellationToken)
    {
        var existing = await _scores.ListByCompanyAsync(companyId, period.Start, cancellationToken);

        foreach (var stale in existing.Where(score => !measured.ContainsKey(score.EmployeeId)))
        {
            _scores.Remove(stale);
        }

        foreach (var (employeeId, factors) in measured)
        {
            var current = existing.SingleOrDefault(score => score.EmployeeId == employeeId);

            if (current is null)
            {
                _scores.Add(FinancialHealthScore.Compute(employeeId, period, factors, computedAt));
            }
            else
            {
                current.Refresh(factors, computedAt);
            }
        }
    }

    private async Task<decimal?> UsdToArsAsync(CancellationToken cancellationToken)
    {
        if (_usdToArs is not null || _usdRateUnavailable)
        {
            return _usdToArs;
        }

        try
        {
            _usdToArs = (await _exchangeRates.GetMepQuoteAsync(cancellationToken)).Sell;
        }
        catch (HttpRequestException exception)
        {
            _usdRateUnavailable = true;
            LogMepUnavailable(_logger, exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _usdRateUnavailable = true;
            LogMepUnavailable(_logger, exception);
        }

        return _usdToArs;
    }

    private static Money InArs(
        IEnumerable<EmployeeCurrencyTotals> totals,
        Func<EmployeeCurrencyTotals, decimal> amount,
        decimal usdToArs) =>
        Money.From(
            totals.Sum(total => total.Currency switch
            {
                Currency.ARS => amount(total),
                Currency.USD => amount(total) * usdToArs,
                _ => 0m,
            }),
            Currency.ARS);

    private static int MonthsOfHistory(DateOnly firstMovementOn, MetricsPeriod period)
    {
        var elapsed = ((period.Start.Year - firstMovementOn.Year) * 12) + period.Start.Month - firstMovementOn.Month + 1;

        return Math.Clamp(elapsed, 1, FinancialHealthFactors.SpendingAverageMonths);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo obtener la cotizacion del dolar MEP para el puntaje de salud financiera.")]
    private static partial void LogMepUnavailable(ILogger logger, Exception exception);
}
