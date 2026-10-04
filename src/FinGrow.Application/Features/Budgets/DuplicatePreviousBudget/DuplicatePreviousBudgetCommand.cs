namespace FinGrow.Application.Features.Budgets.DuplicatePreviousBudget;

using Common;
using DTOs;
using MediatR;

public sealed record DuplicatePreviousBudgetCommand(
    Guid EmployeeId,
    int Year,
    int Month) : IRequest<Result<BudgetResponse>>;
