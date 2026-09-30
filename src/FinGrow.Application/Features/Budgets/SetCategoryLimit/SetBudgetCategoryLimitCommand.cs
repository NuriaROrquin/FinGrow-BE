namespace FinGrow.Application.Features.Budgets.SetCategoryLimit;

using Common;
using DTOs;
using Domain.Enums;
using MediatR;

/// <summary>
/// Define o edita el tope de una categoria en el presupuesto de <see cref="Year"/>/<see cref="Month"/>.
/// No trae moneda: el tope hereda la del presupuesto.
/// </summary>
public sealed record SetBudgetCategoryLimitCommand(
    Guid EmployeeId,
    int Year,
    int Month,
    ExpenseCategory Category,
    decimal Amount) : IRequest<Result<BudgetResponse>>;
