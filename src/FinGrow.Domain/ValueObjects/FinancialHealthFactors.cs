namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public sealed class FinancialHealthFactors : ValueObject
{
    public const int MinimumConfirmedTransactions = 10;
    public const int SavingsWeight = 40;
    public const int BudgetWeight = 25;
    public const int GoalsWeight = 20;
    public const int CushionWeight = 15;
    public const decimal TargetSavingsRate = 0.20m;
    public const decimal TargetCushionMonths = 3m;
    public const int SpendingAverageMonths = 3;

    private const decimal Full = 100m;
    private const int DecimalPlaces = 2;

    private FinancialHealthFactors()
    {
    }

    private FinancialHealthFactors(decimal savings, decimal? budget, decimal? goals, decimal cushion)
    {
        Savings = savings;
        Budget = budget;
        Goals = goals;
        Cushion = cushion;
    }

    public decimal Savings { get; private set; }

    public decimal? Budget { get; private set; }

    public decimal? Goals { get; private set; }

    public decimal Cushion { get; private set; }

    public int Score
    {
        get
        {
            var weighted = (Savings * SavingsWeight) + (Cushion * CushionWeight);
            var weights = SavingsWeight + CushionWeight;

            if (Budget is { } budget)
            {
                weighted += budget * BudgetWeight;
                weights += BudgetWeight;
            }

            if (Goals is { } goals)
            {
                weighted += goals * GoalsWeight;
                weights += GoalsWeight;
            }

            return (int)decimal.Round(weighted / weights, 0, MidpointRounding.AwayFromZero);
        }
    }

    public FinancialHealthBand Band => FinancialHealthScale.BandOf(Score);

    public static bool HasEnoughActivity(int confirmedTransactions, Money income)
    {
        ArgumentNullException.ThrowIfNull(income);

        return confirmedTransactions >= MinimumConfirmedTransactions && !income.IsZero;
    }

    public static FinancialHealthFactors From(decimal savings, decimal? budget, decimal? goals, decimal cushion)
    {
        EnsureInRange(savings, "ahorro");
        EnsureInRange(cushion, "colchon");

        if (budget is { } budgetFactor)
        {
            EnsureInRange(budgetFactor, "presupuesto");
        }

        if (goals is { } goalsFactor)
        {
            EnsureInRange(goalsFactor, "metas");
        }

        return new FinancialHealthFactors(savings, budget, goals, cushion);
    }

    public static decimal SavingsOf(Money income, Money spending)
    {
        ArgumentNullException.ThrowIfNull(income);
        ArgumentNullException.ThrowIfNull(spending);

        if (income.IsZero)
        {
            throw new DomainException("Sin ingresos en el mes no se puede medir el ahorro.");
        }

        return Proportion(income.DifferenceWith(spending) / income.Amount, TargetSavingsRate);
    }

    public static decimal? BudgetOf(IEnumerable<BudgetHealth> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);

        var evaluated = categories.ToList();

        return evaluated.Count == 0
            ? null
            : Proportion(evaluated.Count(category => category != BudgetHealth.Exceeded), evaluated.Count);
    }

    public static decimal? GoalsOf(IEnumerable<Goal> goals, DateOnly anyDayOfMonth)
    {
        ArgumentNullException.ThrowIfNull(goals);

        var fulfilled = goals
            .Select(goal => goal.CommitmentFulfilledIn(anyDayOfMonth))
            .OfType<decimal>()
            .ToList();

        return fulfilled.Count == 0 ? null : Proportion(fulfilled.Average(), 1m);
    }

    public static decimal CushionOf(Money incomeToDate, Money spendingToDate, Money averageMonthlySpending)
    {
        ArgumentNullException.ThrowIfNull(incomeToDate);
        ArgumentNullException.ThrowIfNull(spendingToDate);
        ArgumentNullException.ThrowIfNull(averageMonthlySpending);

        if (averageMonthlySpending.Currency != incomeToDate.Currency)
        {
            throw new DomainException("El colchon se mide con el saldo y el gasto en la misma moneda.");
        }

        var balance = incomeToDate.DifferenceWith(spendingToDate);

        if (averageMonthlySpending.IsZero)
        {
            return balance > 0m ? Full : 0m;
        }

        return Proportion(balance / averageMonthlySpending.Amount, TargetCushionMonths);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Savings;
        yield return Budget;
        yield return Goals;
        yield return Cushion;
    }

    private static decimal Proportion(decimal value, decimal target) =>
        decimal.Round(Math.Clamp(value / target, 0m, 1m) * Full, DecimalPlaces, MidpointRounding.ToEven);

    private static void EnsureInRange(decimal factor, string label)
    {
        if (factor is < 0m or > Full)
        {
            throw new DomainException($"El factor de {label} tiene que estar entre 0 y 100.");
        }
    }
}
