namespace FinGrow.Application.Features.Transactions.ListTransactions;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record ListTransactionsCommand(Guid EmployeeId) : IRequest<Result<IReadOnlyList<TransactionResponse>>>;
