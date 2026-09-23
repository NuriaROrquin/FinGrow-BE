namespace FinGrow.Application.Features.Transactions.GetHistory;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record GetTransactionHistoryQuery(TransactionFilters Filters)
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
    string Source,
    string Status);

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

        var filters = request.Filters;
        var page = await transactionReadRepository.GetPageAsync(
            employeeId,
            filters.PageNumber,
            filters.PageSize,
            filters.Search,
            filters.Type,
            filters.Status,
            filters.ExpenseCategory,
            filters.IncomeCategory,
            filters.PaymentMethod,
            filters.DateFrom,
            filters.DateTo,
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
                    transaction.Source.ToString(),
                    transaction.Status.ToString()))
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
