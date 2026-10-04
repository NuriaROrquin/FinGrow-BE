namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

public sealed class PeriodMetrics : ValueObject
{
    private PeriodMetrics()
    {
    }

    private PeriodMetrics(
        int activeEmployees,
        int participatingEmployees,
        int confirmedTransactions,
        int employeesWithBudget,
        int employeesWithActiveGoal,
        int goalsAchieved,
        int employeesWithIntegration)
    {
        ActiveEmployees = activeEmployees;
        ParticipatingEmployees = participatingEmployees;
        ConfirmedTransactions = confirmedTransactions;
        EmployeesWithBudget = employeesWithBudget;
        EmployeesWithActiveGoal = employeesWithActiveGoal;
        GoalsAchieved = goalsAchieved;
        EmployeesWithIntegration = employeesWithIntegration;
    }

    public int ActiveEmployees { get; private set; }

    public int ParticipatingEmployees { get; private set; }

    public int ConfirmedTransactions { get; private set; }

    public int EmployeesWithBudget { get; private set; }

    public int EmployeesWithActiveGoal { get; private set; }

    public int GoalsAchieved { get; private set; }

    public int EmployeesWithIntegration { get; private set; }

    public decimal? ParticipationRate => ActiveEmployees == 0
        ? null
        : decimal.Round(ParticipatingEmployees * 100m / ActiveEmployees, 2, MidpointRounding.ToEven);

    public static PeriodMetrics Empty => From(0, 0, 0, 0, 0, 0, 0);

    public static PeriodMetrics From(
        int activeEmployees,
        int participatingEmployees,
        int confirmedTransactions,
        int employeesWithBudget,
        int employeesWithActiveGoal,
        int goalsAchieved,
        int employeesWithIntegration)
    {
        EnsureNotNegative(activeEmployees, "empleados activos");
        EnsureNotNegative(confirmedTransactions, "movimientos confirmados");
        EnsureNotNegative(goalsAchieved, "metas alcanzadas");
        EnsureEmployeeCount(participatingEmployees, activeEmployees, "empleados que participaron");
        EnsureEmployeeCount(employeesWithBudget, activeEmployees, "empleados con presupuesto");
        EnsureEmployeeCount(employeesWithActiveGoal, activeEmployees, "empleados con meta activa");
        EnsureEmployeeCount(employeesWithIntegration, activeEmployees, "empleados con integracion");

        return new PeriodMetrics(
            activeEmployees,
            participatingEmployees,
            confirmedTransactions,
            employeesWithBudget,
            employeesWithActiveGoal,
            goalsAchieved,
            employeesWithIntegration);
    }

    private static void EnsureNotNegative(int value, string label)
    {
        if (value < 0)
        {
            throw new DomainException($"La cantidad de {label} no puede ser negativa.");
        }
    }

    private static void EnsureEmployeeCount(int value, int activeEmployees, string label)
    {
        EnsureNotNegative(value, label);

        if (value > activeEmployees)
        {
            throw new DomainException($"La cantidad de {label} no puede superar la de empleados activos.");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ActiveEmployees;
        yield return ParticipatingEmployees;
        yield return ConfirmedTransactions;
        yield return EmployeesWithBudget;
        yield return EmployeesWithActiveGoal;
        yield return GoalsAchieved;
        yield return EmployeesWithIntegration;
    }
}
