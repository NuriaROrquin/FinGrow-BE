namespace FinGrow.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.GetHistory;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

internal sealed class TransactionReadRepository : ITransactionReadRepository
{
    private static readonly Expression<Func<Transaction, int>> CategoryByLabel = SortExpressions.RankBy<Transaction>(
        transaction => transaction.ExpenseCategory == ExpenseCategory.AhorroInversion,
        transaction => transaction.ExpenseCategory == ExpenseCategory.Alimentos,
        transaction => transaction.ExpenseCategory == ExpenseCategory.Educacion,
        transaction => transaction.ExpenseCategory == ExpenseCategory.Entretenimiento,
        transaction => transaction.IncomeCategory == IncomeCategory.Freelance,
        transaction => transaction.ExpenseCategory == ExpenseCategory.Indumentaria,
        transaction => transaction.IncomeCategory == IncomeCategory.Inversiones,
        transaction => transaction.ExpenseCategory == ExpenseCategory.Otros
            || transaction.IncomeCategory == IncomeCategory.Otros,
        transaction => transaction.IncomeCategory == IncomeCategory.Regalo,
        transaction => transaction.IncomeCategory == IncomeCategory.Salario,
        transaction => transaction.ExpenseCategory == ExpenseCategory.Salud,
        transaction => transaction.ExpenseCategory == ExpenseCategory.Servicios,
        transaction => transaction.ExpenseCategory == ExpenseCategory.Transporte,
        transaction => transaction.ExpenseCategory == ExpenseCategory.Vivienda);

    private static readonly Expression<Func<Transaction, int>> PaymentMethodByLabel = SortExpressions.RankBy<Transaction>(
        transaction => transaction.PaymentMethod == PaymentMethod.DigitalWallet,
        transaction => transaction.PaymentMethod == PaymentMethod.Cash,
        transaction => transaction.PaymentMethod == PaymentMethod.CreditCard,
        transaction => transaction.PaymentMethod == PaymentMethod.DebitCard,
        transaction => transaction.PaymentMethod == PaymentMethod.BankTransfer);

    private static readonly Expression<Func<Transaction, int>> SourceByLabel = SortExpressions.RankBy<Transaction>(
        transaction => transaction.Source == TransactionSource.Manual,
        transaction => transaction.Source == TransactionSource.ReceiptScan,
        transaction => transaction.Source == TransactionSource.Gmail,
        transaction => transaction.Source == TransactionSource.MercadoPago,
        transaction => transaction.Source == TransactionSource.Telegram,
        transaction => transaction.Source == TransactionSource.WhatsApp);

    private static readonly Expression<Func<Transaction, int>> StatusByLabel = SortExpressions.RankBy<Transaction>(
        transaction => transaction.Status == TransactionStatus.Confirmed,
        transaction => transaction.Status == TransactionStatus.Discarded,
        transaction => transaction.Status == TransactionStatus.Eliminated,
        transaction => transaction.Status == TransactionStatus.Pending);

    private static readonly Expression<Func<Transaction, decimal>> SignedAmount = transaction =>
        transaction.Type == TransactionType.Expense
            ? -Math.Abs(transaction.Amount.Amount)
            : Math.Abs(transaction.Amount.Amount);

    private readonly FinGrowDbContext _dbContext;

    public TransactionReadRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<MonthlyExpenseTotal>> GetMonthlyExpensesAsync(
        Guid employeeId,
        Currency currency,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var totals = await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.EmployeeId == employeeId
                && transaction.Status == TransactionStatus.Confirmed
                && transaction.Type == TransactionType.Expense
                && transaction.Amount.Currency == currency
                && transaction.OccurredOn >= fromDate
                && transaction.OccurredOn <= toDate)
            .GroupBy(transaction => new { transaction.OccurredOn.Year, transaction.OccurredOn.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Total = group.Sum(transaction => transaction.Amount.Amount),
            })
            .OrderBy(total => total.Year)
            .ThenBy(total => total.Month)
            .ToListAsync(cancellationToken);

        return totals
            .Select(total => new MonthlyExpenseTotal(total.Year, total.Month, total.Total))
            .ToList();
    }

    public async Task<IReadOnlyList<MonthlyIncomeExpenseTotal>> GetMonthlyIncomeExpensesAsync(
        Guid employeeId,
        Currency currency,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var totals = await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.EmployeeId == employeeId
                && transaction.Status == TransactionStatus.Confirmed
                && transaction.Amount.Currency == currency
                && transaction.OccurredOn >= fromDate
                && transaction.OccurredOn <= toDate)
            .GroupBy(transaction => new { transaction.OccurredOn.Year, transaction.OccurredOn.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                TotalIncome = group.Sum(transaction =>
                    transaction.Type == TransactionType.Income ? transaction.Amount.Amount : 0m),
                TotalExpense = group.Sum(transaction =>
                    transaction.Type == TransactionType.Expense ? transaction.Amount.Amount : 0m),
            })
            .OrderBy(total => total.Year)
            .ThenBy(total => total.Month)
            .ToListAsync(cancellationToken);

        return totals
            .Select(total => new MonthlyIncomeExpenseTotal(
                total.Year,
                total.Month,
                total.TotalIncome,
                total.TotalExpense))
            .ToList();
    }

    public async Task<IReadOnlyList<CategoryExpenseTotal>> GetExpensesByCategoryAsync(
        Guid employeeId,
        Currency currency,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var totals = await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.EmployeeId == employeeId
                && transaction.Status == TransactionStatus.Confirmed
                && transaction.Type == TransactionType.Expense
                && transaction.Amount.Currency == currency
                && transaction.OccurredOn >= fromDate
                && transaction.OccurredOn <= toDate)
            .GroupBy(transaction => transaction.ExpenseCategory!.Value)
            .Select(group => new
            {
                Category = group.Key,
                Total = group.Sum(transaction => transaction.Amount.Amount),
            })
            .Where(total => total.Total > 0m)
            .OrderBy(total => total.Category)
            .ToListAsync(cancellationToken);

        return totals
            .Select(total => new CategoryExpenseTotal(total.Category, total.Total))
            .ToList();
    }

    public async Task<TransactionSummary> GetSummaryAsync(
        Guid employeeId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var confirmedQuery = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.EmployeeId == employeeId)
            .Where(transaction => transaction.Status == TransactionStatus.Confirmed);

        if (fromDate.HasValue)
        {
            confirmedQuery = confirmedQuery.Where(transaction => transaction.OccurredOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            confirmedQuery = confirmedQuery.Where(transaction => transaction.OccurredOn <= toDate.Value);
        }

        var totals = await confirmedQuery
            .GroupBy(transaction => new { transaction.Type, transaction.Amount.Currency })
            .Select(group => new TypeCurrencyTotal(
                group.Key.Type,
                group.Key.Currency,
                group.Count(),
                group.Sum(transaction => transaction.Amount.Amount)))
            .ToListAsync(cancellationToken);

        var totalTransactions = totals.Sum(total => total.Count);
        var totalIncomeTransactions = totals
            .Where(total => total.Type == TransactionType.Income)
            .Sum(total => total.Count);
        var totalExpenseTransactions = totals
            .Where(total => total.Type == TransactionType.Expense)
            .Sum(total => total.Count);

        var totalIncomeArs = GetTotal(totals, TransactionType.Income, Currency.ARS);
        var totalIncomeUsd = GetTotal(totals, TransactionType.Income, Currency.USD);
        var totalExpenseArs = GetTotal(totals, TransactionType.Expense, Currency.ARS);
        var totalExpenseUsd = GetTotal(totals, TransactionType.Expense, Currency.USD);
        var currencies = Enum.GetValues<Currency>()
            .ToDictionary(
                currency => currency,
                currency => new CurrencyTransactionSummary(
                    GetTotal(totals, TransactionType.Income, currency),
                    GetTotal(totals, TransactionType.Expense, currency),
                    totals
                        .Where(total => total.Currency == currency)
                        .Sum(total => total.Count),
                    totals
                        .Where(total => total.Currency == currency && total.Type == TransactionType.Income)
                        .Sum(total => total.Count),
                    totals
                        .Where(total => total.Currency == currency && total.Type == TransactionType.Expense)
                        .Sum(total => total.Count)));
        var otherCurrencies = currencies
            .Where(pair => pair.Key is not (Currency.ARS or Currency.USD) && pair.Value.Transactions > 0)
            .Select(pair => pair.Key)
            .ToArray();

        return new TransactionSummary(
            totalTransactions,
            totalExpenseTransactions,
            totalIncomeTransactions,
            totalIncomeArs,
            totalIncomeUsd,
            totalExpenseArs,
            totalExpenseUsd,
            currencies,
            otherCurrencies);
    }
    

    public async Task<TransactionPage> GetPageAsync(
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
        TransactionSortField sortBy,
        SortDirection sortDirection,
        CancellationToken cancellationToken = default)
    {
        var filteredQuery = BuildFilteredQuery(
            employeeId,
            search,
            type,
            statuses,
            expenseCategory,
            incomeCategory,
            paymentMethod,
            fromDate,
            toDate);

        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        var items = await Sort(filteredQuery, sortBy, sortDirection)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new TransactionPage(items, pageNumber, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<Transaction>> GetFilteredAsync(
        Guid employeeId,
        string? search,
        TransactionType? type,
        IReadOnlyCollection<TransactionStatus> statuses,
        ExpenseCategory? expenseCategory,
        IncomeCategory? incomeCategory,
        PaymentMethod? paymentMethod,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default) =>
        await BuildFilteredQuery(
                employeeId,
                search,
                type,
                statuses,
                expenseCategory,
                incomeCategory,
                paymentMethod,
                fromDate,
                toDate)
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync(cancellationToken);

    private IQueryable<Transaction> BuildFilteredQuery(
        Guid employeeId,
        string? search,
        TransactionType? type,
        IReadOnlyCollection<TransactionStatus> statuses,
        ExpenseCategory? expenseCategory,
        IncomeCategory? incomeCategory,
        PaymentMethod? paymentMethod,
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        var filteredQuery = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.EmployeeId == employeeId)
            .Where(transaction => statuses.Contains(transaction.Status));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            filteredQuery = filteredQuery.Where(transaction =>
                EF.Functions.ILike(transaction.Description, pattern)
                || (transaction.ExternalReference != null
                    && EF.Functions.ILike(transaction.ExternalReference, pattern)));
        }

        if (type.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.Type == type.Value);
        }

        if (expenseCategory.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.ExpenseCategory == expenseCategory.Value);
        }

        if (incomeCategory.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.IncomeCategory == incomeCategory.Value);
        }

        if (paymentMethod.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.PaymentMethod == paymentMethod.Value);
        }

        if (fromDate.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.OccurredOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.OccurredOn <= toDate.Value);
        }

        return filteredQuery;
    }

    private static IQueryable<Transaction> Sort(
        IQueryable<Transaction> query,
        TransactionSortField sortBy,
        SortDirection sortDirection)
    {
        var ascending = sortDirection == SortDirection.Ascending;

        var ordered = sortBy switch
        {
            TransactionSortField.Description => query.OrderByDirection(
                transaction => EF.Functions.Collate(transaction.Description, SortExpressions.SpanishCollation),
                ascending),
            TransactionSortField.Category => query.OrderByDirection(CategoryByLabel, ascending),
            TransactionSortField.PaymentMethod => query.OrderByDirection(PaymentMethodByLabel, ascending),
            TransactionSortField.Source => query.OrderByDirection(SourceByLabel, ascending),
            TransactionSortField.Status => query.OrderByDirection(StatusByLabel, ascending),
            TransactionSortField.Amount => query.OrderByDirection(SignedAmount, ascending),
            _ => query.OrderByDirection(transaction => transaction.OccurredOn, ascending),
        };

        return ordered
            .ThenByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.Id);
    }

    private static decimal GetTotal(IEnumerable<TypeCurrencyTotal> totals, TransactionType type, Currency currency) =>
        totals.FirstOrDefault(total => total.Type == type && total.Currency == currency)?.Total ?? 0m;

    private sealed record TypeCurrencyTotal(TransactionType Type, Currency Currency, int Count, decimal Total);
}
