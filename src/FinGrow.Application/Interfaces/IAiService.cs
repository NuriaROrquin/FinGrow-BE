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
    double Confidence);

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

public interface IAiService
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategorizedExpense>> CategorizeExpensesAsync(
        IReadOnlyList<ExpenseToCategorize> expenses,
        CancellationToken cancellationToken = default);

    Task<ParsedMessage> ParseTransactionMessageAsync(
        string text,
        Currency defaultCurrency,
        CancellationToken cancellationToken = default);

    Task<ParsedBillEmail> ParseBillEmailAsync(
        BillEmail email,
        Currency defaultCurrency,
        CancellationToken cancellationToken = default);
}
