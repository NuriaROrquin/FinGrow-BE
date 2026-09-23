namespace FinGrow.Application.Features.Transactions.GetTransactionSummary;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using MediatR;

public sealed record GetTransactionSummaryQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<Result<TransactionSummaryResponse>>;

public sealed record TransactionSummaryResponse(
    decimal TotalIncomeArs,
    decimal TotalIncomeUsd,
    int TotalIncomeTransactions,
    decimal TotalExpenseArs,
    decimal TotalExpenseUsd,
    int TotalExpenseTransactions,
    int TotalTransactions);

internal sealed class GetTransactionSummaryQueryHandler(
    ITransactionReadRepository transactionReadRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetTransactionSummaryQuery, Result<TransactionSummaryResponse>>
{
    public async Task<Result<TransactionSummaryResponse>> Handle(
        GetTransactionSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<TransactionSummaryResponse>(
                Error.Forbidden("Transactions.Unauthenticated", "Hay que iniciar sesion para consultar el resumen."));
        }

        var summary = await transactionReadRepository.GetSummaryAsync(
            employeeId, request.FromDate, request.ToDate, cancellationToken);

        return Result.Success(new TransactionSummaryResponse(
            summary.TotalIncomeArs,
            summary.TotalIncomeUsd,
            summary.TotalIncomeTransactions,
            summary.TotalExpenseArs,
            summary.TotalExpenseUsd,
            summary.TotalExpenseTransactions,
            summary.TotalTransactions));
    }
}
