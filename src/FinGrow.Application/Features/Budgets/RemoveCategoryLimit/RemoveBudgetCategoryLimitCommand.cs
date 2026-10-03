namespace FinGrow.Application.Features.Budgets.RemoveCategoryLimit;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

public sealed record RemoveBudgetCategoryLimitCommand(
    Guid EmployeeId,
    int Year,
    int Month,
    ExpenseCategory Category) : IRequest<Result<BudgetResponse>>;
