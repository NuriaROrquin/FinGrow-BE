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
    int TotalPages);

internal sealed class GetTransactionHistoryQueryHandler(
    ITransactionReadRepository transactionReadRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetTransactionHistoryQuery, Result<TransactionHistoryResponse>>
{
    public async Task<Result<TransactionHistoryResponse>> Handle(
        GetTransactionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<TransactionHistoryResponse>(
                Error.Forbidden("Transactions.Unauthenticated", "Hay que iniciar sesion para consultar los movimientos."));
        }

        var type = request.Type?.Trim().ToLowerInvariant() switch
        {
            null or "" => null,
            "ingreso" or "income" => TransactionType.Income,
            "gasto" or "expense" => TransactionType.Expense,
            _ => (TransactionType?)null
        };

        var page = await transactionReadRepository.GetPageAsync(
            employeeId,
            request.PageNumber,
            request.PageSize,
            request.Search,
            type,
            cancellationToken);

        var items = page.Items
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.Id)
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
            page.TotalPages));
    }

    private static string GetCategory(Transaction transaction) =>
        transaction.Type == TransactionType.Expense
            ? transaction.ExpenseCategory!.Value.ToString()
            : transaction.IncomeCategory!.Value.ToString();
}
