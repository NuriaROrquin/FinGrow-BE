namespace FinGrow.Application.Features.Budgets.SetCategoryLimit;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

public sealed record SetBudgetCategoryLimitCommand(
    Guid EmployeeId,
    int Year,
    int Month,
    ExpenseCategory Category,
    decimal Amount) : IRequest<Result<BudgetResponse>>;
