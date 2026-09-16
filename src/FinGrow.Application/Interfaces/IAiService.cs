namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.Enums;

public sealed record ExpenseToCategorize(Guid Id, string Description, decimal Amount, Currency Currency, DateOnly OccurredOn);

public sealed record CategorizedExpense(Guid Id, ExpenseCategory Category, double Confidence);

public interface IAiService
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategorizedExpense>> CategorizeExpensesAsync(
        IReadOnlyList<ExpenseToCategorize> expenses,
        CancellationToken cancellationToken = default);
}
