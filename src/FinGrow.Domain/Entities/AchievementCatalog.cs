namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public static class AchievementCatalog
{
    public const int GoalsForThreeGoalsAchieved = 3;
    public const int ConsecutiveActiveMonths = 3;
    public const decimal SaverRate = 0.10m;

    public static IReadOnlyList<Achievement> All { get; } = new[]
    {
        Achievement.Define(
            AchievementCode.FirstTransaction,
            "Primer paso",
            "Confirmaste tu primer movimiento.",
            "Tiene al menos un movimiento confirmado, cargado a mano o por cualquier integración.",
            AchievementEvaluation.Instant),
        Achievement.Define(
            AchievementCode.ReceiptScanned,
            "Sin papeles",
            "Registraste un movimiento sacándole una foto al comprobante.",
            "Tiene al menos un movimiento confirmado que entró por la lectura de un comprobante.",
            AchievementEvaluation.Instant),
        Achievement.Define(
            AchievementCode.ChannelLinked,
            "Conectado",
            "Vinculaste tu primera integración.",
            "Vinculó al menos una integración: Gmail, Mercado Pago, Telegram, WhatsApp o InvertirOnline.",
            AchievementEvaluation.Instant),
        Achievement.Define(
            AchievementCode.FirstBudget,
            "Con plan",
            "Armaste tu primer presupuesto.",
            "Tiene al menos un presupuesto con un tope por categoría.",
            AchievementEvaluation.Instant),
        Achievement.Define(
            AchievementCode.FirstGoal,
            "Con objetivo",
            "Creaste tu primera meta de ahorro.",
            "Creó al menos una meta de ahorro.",
            AchievementEvaluation.Instant),
        Achievement.Define(
            AchievementCode.GoalAchieved,
            "Meta cumplida",
            "Llegaste al objetivo de una meta de ahorro.",
            "Tiene al menos una meta alcanzada.",
            AchievementEvaluation.Instant,
            isMajor: true),
        Achievement.Define(
            AchievementCode.ThreeGoalsAchieved,
            "Imparable",
            "Cumpliste tres metas de ahorro.",
            $"Tiene al menos {GoalsForThreeGoalsAchieved} metas alcanzadas.",
            AchievementEvaluation.Instant,
            isMajor: true),
        Achievement.Define(
            AchievementCode.FirstInvestment,
            "Inversor",
            "Registraste tu primera inversión.",
            "Tiene al menos una inversión, cargada a mano o importada de InvertirOnline.",
            AchievementEvaluation.Instant),
        Achievement.Define(
            AchievementCode.FirstCourse,
            "Aprendiz",
            "Terminaste tu primer curso.",
            "Completó todas las lecciones de al menos un curso publicado.",
            AchievementEvaluation.Instant),
        Achievement.Define(
            AchievementCode.ConsistentThreeMonths,
            "Constancia",
            "Registraste tus finanzas tres meses seguidos.",
            $"Cerró {ConsecutiveActiveMonths} meses consecutivos con actividad suficiente: al menos "
            + $"{FinancialHealthFactors.MinimumConfirmedTransactions} movimientos confirmados y algún ingreso en cada uno.",
            AchievementEvaluation.MonthlyClose,
            isMajor: true),
        Achievement.Define(
            AchievementCode.BudgetKept,
            "Dentro del plan",
            "Cerraste un mes sin pasarte de ningún tope de tu presupuesto.",
            "Cerró un mes con actividad suficiente y con presupuesto mensual, sin ninguna categoría por encima de su tope.",
            AchievementEvaluation.MonthlyClose,
            isMajor: true),
        Achievement.Define(
            AchievementCode.Saver,
            "Ahorrador",
            "Ahorraste al menos el 10% de tus ingresos en un mes.",
            "Cerró un mes con actividad suficiente en el que lo que no gastó fue al menos el 10% de sus ingresos. "
            + "Lo destinado a ahorro_inversion cuenta como ahorro, igual que en el puntaje de salud financiera.",
            AchievementEvaluation.MonthlyClose,
            isMajor: true),
    };

    public static IReadOnlyList<AchievementCode> InstantCodes { get; } = All
        .Where(achievement => achievement.Evaluation == AchievementEvaluation.Instant)
        .Select(achievement => achievement.Code)
        .ToList();

    public static Achievement Find(AchievementCode code) =>
        All.SingleOrDefault(achievement => achievement.Code == code)
        ?? throw new DomainException($"El logro '{code}' no existe en el catalogo.");

    public static bool IsActiveMonth(int confirmedTransactions, bool hasIncome) =>
        confirmedTransactions >= FinancialHealthFactors.MinimumConfirmedTransactions && hasIncome;

    public static bool KeptBudget(IReadOnlyCollection<BudgetHealth> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);

        return categories.Count > 0 && categories.All(health => health != BudgetHealth.Exceeded);
    }

    public static bool Saved(Money income, Money spending)
    {
        ArgumentNullException.ThrowIfNull(income);
        ArgumentNullException.ThrowIfNull(spending);

        return !income.IsZero && income.DifferenceWith(spending) / income.Amount >= SaverRate;
    }
}
