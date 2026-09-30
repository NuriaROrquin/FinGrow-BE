namespace FinGrow.Application.Features.Budgets.CreateBudget;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

/// <summary>Presupuesto mensual: <see cref="Year"/> y <see cref="Month"/> identifican el periodo.</summary>
public sealed record CreateBudgetCommand(
    Guid EmployeeId,
    int Year,
    int Month,
    Currency Currency,
    IReadOnlyList<CategoryLimitInput> Limits) : IRequest<Result<BudgetResponse>>;

public sealed record CategoryLimitInput(ExpenseCategory Category, decimal Amount);
