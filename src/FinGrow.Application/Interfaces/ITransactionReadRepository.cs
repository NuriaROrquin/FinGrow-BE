namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public interface ITransactionReadRepository
{
    Task<IReadOnlyList<MonthlyExpenseTotal>> GetMonthlyExpensesAsync(
        Guid employeeId,
        Currency currency,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyIncomeExpenseTotal>> GetMonthlyIncomeExpensesAsync(
        Guid employeeId,
        Currency currency,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryExpenseTotal>> GetExpensesByCategoryAsync(
        Guid employeeId,
        Currency currency,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<TransactionSummary> GetSummaryAsync(
        Guid employeeId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<TransactionPage> GetPageAsync(
        Guid employeeId,
        int pageNumber,
        int pageSize,
        string? search,
        TransactionType? type,
        IReadOnlyCollection<TransactionStatus> statuses,
        ExpenseCategory? expenseCategory,
        IncomeCategory? incomeCategory,
        PaymentMethod? paymentMethod,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetFilteredAsync(
        Guid employeeId,
        string? search,
        TransactionType? type,
        IReadOnlyCollection<TransactionStatus> statuses,
        ExpenseCategory? expenseCategory,
        IncomeCategory? incomeCategory,
        PaymentMethod? paymentMethod,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);
}

/// <summary>Totales sobre la totalidad de movimientos confirmados del empleado, sin paginar.</summary>
public sealed record TransactionSummary(
    int TotalTransactions,
    int TotalExpenseTransactions,
    int TotalIncomeTransactions,
    decimal TotalIncomeArs,
    decimal TotalIncomeUsd,
    decimal TotalExpenseArs,
    decimal TotalExpenseUsd)
{
    public TransactionSummary(
        int totalTransactions,
        int totalExpenseTransactions,
        int totalIncomeTransactions,
        decimal totalIncomeArs,
        decimal totalIncomeUsd,
        decimal totalExpenseArs,
        decimal totalExpenseUsd,
        IReadOnlyDictionary<Currency, CurrencyTransactionSummary> currencies,
        IReadOnlyCollection<Currency> otherCurrencies)
        : this(
            totalTransactions,
            totalExpenseTransactions,
            totalIncomeTransactions,
            totalIncomeArs,
            totalIncomeUsd,
            totalExpenseArs,
            totalExpenseUsd)
    {
        Currencies = currencies;
        OtherCurrencies = otherCurrencies;
    }

    public IReadOnlyDictionary<Currency, CurrencyTransactionSummary> Currencies { get; } =
        new Dictionary<Currency, CurrencyTransactionSummary>
        {
            [Currency.ARS] = new(TotalIncomeArs, TotalExpenseArs, TotalTransactions, TotalIncomeTransactions, TotalExpenseTransactions),
            [Currency.USD] = new(TotalIncomeUsd, TotalExpenseUsd, 0, 0, 0),
        };

    public IReadOnlyCollection<Currency> OtherCurrencies { get; } = Array.Empty<Currency>();
}

public sealed record CurrencyTransactionSummary(
    decimal Income,
    decimal Expenses,
    int Transactions,
    int IncomeTransactions,
    int ExpenseTransactions)
{
    public decimal Balance => Income - Expenses;
}

public sealed record TransactionPage(
    IReadOnlyList<Transaction> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record MonthlyExpenseTotal(int Year, int Month, decimal Total);

public sealed record MonthlyIncomeExpenseTotal(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense);

public sealed record CategoryExpenseTotal(ExpenseCategory Category, decimal Total);