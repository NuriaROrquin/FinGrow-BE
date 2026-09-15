namespace FinGrow.Application.Features.Transactions.ListTransactions;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs.Transactions;
using MediatR;

public record ListTransactionsRequest(Guid EmployeeId) : IRequest<Result<IReadOnlyList<TransactionResponse>>>;
