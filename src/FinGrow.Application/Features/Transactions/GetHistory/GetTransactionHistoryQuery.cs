namespace FinGrow.Application.Features.Transactions.GetHistory;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record GetTransactionHistoryQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    string? Type = null)
    : IRequest<Result<TransactionHistoryResponse>>;

public sealed record TransactionHistoryItem(
    Guid Id,
    DateOnly OccurredOn,
    decimal Amount,
    string Currency,
    string Category,
    string Description,
    string PaymentMethod,
    string Type,
    string Source);

public sealed record TransactionHistoryResponse(
    IReadOnlyList<TransactionHistoryItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyDictionary<string, decimal> TotalSpent,
    IReadOnlyDictionary<string, decimal> TotalIncome);

internal sealed class GetTransactionHistoryQueryHandler
    : IRequestHandler<GetTransactionHistoryQuery, Result<TransactionHistoryResponse>>
{
    private readonly ITransactionReadRepository _transactionReadRepository;
    private readonly ICurrentUser _currentUser;

    public GetTransactionHistoryQueryHandler(
        ITransactionReadRepository transactionReadRepository,
        ICurrentUser currentUser)
    {
        _transactionReadRepository = transactionReadRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<TransactionHistoryResponse>> Handle(
        GetTransactionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Result.Failure<TransactionHistoryResponse>(
                Error.Forbidden("Transactions.UserRequired", "No se pudo identificar al usuario autenticado."));
        }

        var type = request.Type?.Trim().ToLowerInvariant() switch
        {
            null or "" => null,
            "ingreso" or "income" => TransactionType.Income,
            "gasto" or "expense" => TransactionType.Expense,
            _ => (TransactionType?)null
        };

        var page = await _transactionReadRepository.GetPageAsync(
            _currentUser.UserId.Value,
            request.PageNumber,
            request.PageSize,
            request.Search,
            type,
            cancellationToken);

        var items = page.Items
            .Select(transaction => new TransactionHistoryItem(
                transaction.Id,
                transaction.OccurredOn,
                transaction.Amount.Amount,
                transaction.Amount.Currency.ToString(),
                GetCategory(transaction),
                transaction.Description,
                transaction.PaymentMethod.ToString(),
                transaction.Type.ToString(),
                transaction.Source.ToString()))
            .ToList();

        return Result.Success(new TransactionHistoryResponse(
            items,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            page.TotalSpent,
            page.TotalIncome));
    }

    private static string GetCategory(Transaction transaction) =>
        transaction.Type == TransactionType.Expense
            ? transaction.ExpenseCategory!.Value.ToString()
            : transaction.IncomeCategory!.Value.ToString();
}
