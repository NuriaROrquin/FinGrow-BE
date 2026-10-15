namespace FinGrow.Application.Features.Achievements.Evaluate;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal sealed record MonthlyAchievements(
    IReadOnlyDictionary<Guid, IReadOnlyDictionary<AchievementCode, DateTimeOffset>> Reached,
    IReadOnlyList<Guid> WithoutExchangeRate);

/// <summary>
/// Los logros de mes cerrado de una empresa. Reutiliza las lecturas del puntaje de salud financiera
/// para que "actividad suficiente" y "ahorro" signifiquen lo mismo en los dos lados.
/// </summary>
internal sealed partial class MonthlyAchievementsEvaluator(
    IPeriodActivityReadRepository activity,
    IFinancialHealthReadRepository finances,
    IExchangeRateProvider exchangeRates,
    ILogger<MonthlyAchievementsEvaluator> logger)
{
    private decimal? _usdToArs;
    private bool _usdRateUnavailable;

    public async Task<MonthlyAchievements> EvaluateAsync(
        Guid companyId,
        MetricsPeriod period,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(period);

        var months = new List<MetricsPeriod> { period };

        while (months.Count < AchievementCatalog.ConsecutiveActiveMonths)
        {
            months.Insert(0, months[0].Previous());
        }

        var activeByMonth = new Dictionary<MetricsPeriod, HashSet<Guid>>();
        IReadOnlyList<EmployeeCurrencyTotals> closedMonthTotals = Array.Empty<EmployeeCurrencyTotals>();

        foreach (var month in months)
        {
            var (active, totals) = await ActiveEmployeesAsync(companyId, month, cancellationToken);
            activeByMonth[month] = active;

            if (month.Equals(period))
            {
                closedMonthTotals = totals;
            }
        }

        var activeInPeriod = activeByMonth[period];
        var limits = activeInPeriod.Count == 0
            ? Array.Empty<EmployeeBudgetLimit>()
            : await finances.ListBudgetLimitsAsync(activeInPeriod.ToList(), period, cancellationToken);
        var achievedAt = period.EndInstantExclusive;
        var reached = new Dictionary<Guid, IReadOnlyDictionary<AchievementCode, DateTimeOffset>>();
        var withoutExchangeRate = new List<Guid>();

        foreach (var employeeId in activeInPeriod)
        {
            var codes = new Dictionary<AchievementCode, DateTimeOffset>();

            if (months.All(month => activeByMonth[month].Contains(employeeId)))
            {
                codes[AchievementCode.ConsistentThreeMonths] = achievedAt;
            }

            var budgetCategories = limits
                .Where(limit => limit.EmployeeId == employeeId)
                .Select(limit => Budget.HealthOf(limit.Limit, limit.Spent))
                .ToList();

            if (AchievementCatalog.KeptBudget(budgetCategories))
            {
                codes[AchievementCode.BudgetKept] = achievedAt;
            }

            var ownTotals = closedMonthTotals.Where(total => total.EmployeeId == employeeId).ToList();

            switch (await SavedAsync(ownTotals, cancellationToken))
            {
                case true:
                    codes[AchievementCode.Saver] = achievedAt;
                    break;
                case null:
                    withoutExchangeRate.Add(employeeId);
                    break;
            }

            if (codes.Count > 0)
            {
                reached[employeeId] = codes;
            }
        }

        return new MonthlyAchievements(reached, withoutExchangeRate);
    }

    private async Task<(HashSet<Guid> Active, IReadOnlyList<EmployeeCurrencyTotals> Totals)> ActiveEmployeesAsync(
        Guid companyId,
        MetricsPeriod month,
        CancellationToken cancellationToken)
    {
        var candidates = (await activity.ListByCompanyAsync(companyId, month, cancellationToken))
            .Where(employee => employee.ConfirmedTransactions >= FinancialHealthFactors.MinimumConfirmedTransactions)
            .ToList();

        if (candidates.Count == 0)
        {
            return (new HashSet<Guid>(), Array.Empty<EmployeeCurrencyTotals>());
        }

        var totals = await finances.ListMovementTotalsAsync(
            candidates.Select(employee => employee.EmployeeId).ToList(), month, cancellationToken);

        var active = candidates
            .Where(employee => AchievementCatalog.IsActiveMonth(
                employee.ConfirmedTransactions,
                totals.Any(total => total.EmployeeId == employee.EmployeeId && total.MonthIncome > 0m)))
            .Select(employee => employee.EmployeeId)
            .ToHashSet();

        return (active, totals);
    }

    /// <summary>Null cuando hay movimientos en dolares y no se consiguio el MEP: no se puede saber.</summary>
    private async Task<bool?> SavedAsync(IReadOnlyCollection<EmployeeCurrencyTotals> totals, CancellationToken cancellationToken)
    {
        var usdToArs = 0m;

        if (totals.Any(total => total.Currency == Currency.USD))
        {
            if (await UsdToArsAsync(cancellationToken) is not { } rate)
            {
                return null;
            }

            usdToArs = rate;
        }

        return AchievementCatalog.Saved(
            InArs(totals, total => total.MonthIncome, usdToArs),
            InArs(totals, total => total.MonthSpending, usdToArs));
    }

    private async Task<decimal?> UsdToArsAsync(CancellationToken cancellationToken)
    {
        if (_usdToArs is not null || _usdRateUnavailable)
        {
            return _usdToArs;
        }

        try
        {
            _usdToArs = (await exchangeRates.GetMepQuoteAsync(cancellationToken)).Sell;
        }
        catch (HttpRequestException exception)
        {
            _usdRateUnavailable = true;
            LogMepUnavailable(logger, exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _usdRateUnavailable = true;
            LogMepUnavailable(logger, exception);
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo obtener la cotizacion del dolar MEP para el logro de ahorro.")]
    private static partial void LogMepUnavailable(ILogger logger, Exception exception);
}
