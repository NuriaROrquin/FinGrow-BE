namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.Enums;

public sealed record ExpenseToCategorize(Guid Id, string Description, decimal Amount, Currency Currency, DateOnly OccurredOn);

public sealed record CategorizedExpense(Guid Id, ExpenseCategory Category, double Confidence, string? Model = null);

public enum MessageParsingOutcome
{
    Parsed,
    MissingAmount,
    NotATransaction,
    MultipleTransactions
}

public sealed record ParsedTransaction(
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    ExpenseCategory? ExpenseCategory,
    IncomeCategory? IncomeCategory,
    string Description,
    PaymentMethod? PaymentMethod,
    double Confidence,
    DateOnly? OccurredOn = null,
    bool OccurredOnIsEstimate = false);

public sealed record ParsedMessage(MessageParsingOutcome Outcome, ParsedTransaction? Transaction, string? Model)
{
    public static ParsedMessage Of(MessageParsingOutcome outcome, string? model = null) => new(outcome, null, model);
}

public enum BillParsingOutcome
{
    Bill,
    MissingAmount,
    NotABill
}

/// <summary>
/// Un mail para que FinGrow-AI decida si es una factura. <c>Sender</c> es solo el dominio del remitente:
/// la direccion completa es un dato personal y en un reenvio es la del propio empleado.
/// </summary>
public sealed record BillEmail(string Subject, string Sender, string Body);

public sealed record ParsedBill(
    string Issuer,
    decimal Amount,
    Currency Currency,
    DateOnly? DueDate,
    ExpenseCategory Category,
    double Confidence);

public sealed record ParsedBillEmail(BillParsingOutcome Outcome, ParsedBill? Bill, string? Model)
{
    public static ParsedBillEmail Of(BillParsingOutcome outcome, string? model = null) => new(outcome, null, model);
}

public enum AudioTranscriptionOutcome
{
    Transcribed,
    NotUnderstood,
    UnsupportedFormat,
    TooLarge
}

public sealed record AudioTranscription(AudioTranscriptionOutcome Outcome, string? Text, string? Model)
{
    public static AudioTranscription Of(AudioTranscriptionOutcome outcome) => new(outcome, null, null);
}

public enum ReceiptScanOutcome
{
    Scanned,
    MissingAmount,
    Unreadable,
    UnsupportedDocument,
    TooLarge
}

public sealed record ScannedReceipt(
    string? Merchant,
    decimal Amount,
    Currency Currency,
    DateOnly? OccurredOn,
    ExpenseCategory Category,
    PaymentMethod? PaymentMethod,
    double Confidence);

public sealed record ReceiptScan(ReceiptScanOutcome Outcome, ScannedReceipt? Receipt, string? Model)
{
    public static ReceiptScan Of(ReceiptScanOutcome outcome, string? model = null) => new(outcome, null, model);
}

public enum RecommendationKind
{
    Positive,
    Warning,
    Tip
}

public enum RecommendationAction
{
    None,
    Budgets,
    Goals,
    Transactions,
    Education
}

public sealed record RecommendationsInput(
    Language Language,
    Currency Currency,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<RecommendationMonth> Months,
    IReadOnlyList<RecommendationBudgetLimit> BudgetLimits,
    IReadOnlyList<RecommendationGoal> Goals);

public sealed record RecommendationMonth(
    int Year,
    int Month,
    bool IsReference,
    decimal Income,
    decimal Expenses,
    decimal Savings,
    IReadOnlyDictionary<ExpenseCategory, decimal> ExpensesByCategory);

public sealed record RecommendationBudgetLimit(ExpenseCategory Category, decimal Limit, decimal Spent);

public sealed record RecommendationGoal(
    string Name,
    bool IsAchieved,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal MonthlyCommitment,
    DateOnly Deadline);

public sealed record FinancialRecommendation(
    RecommendationKind Kind,
    string Title,
    string Body,
    RecommendationAction Action);

public sealed record FinancialRecommendations(IReadOnlyList<FinancialRecommendation> Recommendations, string? Model);

/// <summary>
/// Lo que se le manda a FinGrow-AI para recomendar sobre los presupuestos (HU-25): por cada mes, los
/// topes contra lo gastado y lo gastado en categorías sin tope. Nunca movimientos sueltos.
/// </summary>
public sealed record BudgetRecommendationsInput(
    Language Language,
    Currency Currency,
    IReadOnlyList<BudgetRecommendationMonth> Months);

public sealed record BudgetRecommendationMonth(
    int Year,
    int Month,
    bool IsCurrent,
    IReadOnlyList<RecommendationBudgetLimit> Limits,
    IReadOnlyDictionary<ExpenseCategory, decimal> UnbudgetedExpenses);

/// <summary>
/// Lo que se le manda a FinGrow-AI para recomendarle a una empresa (HU-61 y HU-64): conteos y
/// porcentajes agregados de un mes. Nunca un id, un nombre ni un texto libre; lo verifica
/// <c>AnonymizedContractRules</c>.
/// </summary>
public sealed record CompanyRecommendationsInput(
    Language Language,
    int Year,
    int Month,
    int MeasuredEmployees,
    decimal? AverageScore,
    decimal? ParticipationRate,
    IReadOnlyList<CompanyConcernSummary> Concerns);

public sealed record CompanyConcernSummary(
    CompanyConcern Concern,
    int AffectedEmployees,
    int EvaluatedEmployees,
    decimal Percentage);

public interface IAiService
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategorizedExpense>> CategorizeExpensesAsync(
        IReadOnlyList<ExpenseToCategorize> expenses,
        CancellationToken cancellationToken = default);

    Task<ParsedMessage> ParseTransactionMessageAsync(
        string text,
        Currency defaultCurrency,
        DateOnly today,
        CancellationToken cancellationToken = default);

    Task<ParsedBillEmail> ParseBillEmailAsync(
        BillEmail email,
        Currency defaultCurrency,
        CancellationToken cancellationToken = default);

    Task<AudioTranscription> TranscribeAudioAsync(
        ReadOnlyMemory<byte> audio,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<ReceiptScan> ScanReceiptAsync(
        ReadOnlyMemory<byte> document,
        string contentType,
        Currency defaultCurrency,
        CancellationToken cancellationToken = default);

    Task<FinancialRecommendations> RecommendAsync(
        RecommendationsInput input,
        CancellationToken cancellationToken = default);

    Task<FinancialRecommendations> RecommendBudgetAsync(
        BudgetRecommendationsInput input,
        CancellationToken cancellationToken = default);

    Task<FinancialRecommendations> RecommendCompanyAsync(
        CompanyRecommendationsInput input,
        CancellationToken cancellationToken = default);
}
